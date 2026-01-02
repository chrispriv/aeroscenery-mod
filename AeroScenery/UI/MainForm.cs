using AeroScenery.AFS2;
using AeroScenery.Common;
using AeroScenery.Controls;
using AeroScenery.Data;
using AeroScenery.Data.Mappers;
using AeroScenery.Data.Models;
using AeroScenery.FileManagement;
using AeroScenery.FSCloudPort;
using AeroScenery.OrthophotoSources;
using AeroScenery.OrthoPhotoSources;
using AeroScenery.Resources;
using AeroScenery.UI;
using AeroScenery.USGS;
using AeroScenery.USGS.Models;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
//#MOD_j
using System.Net.Sockets;
using GMap.NET.WindowsForms.Markers;
using System.Net.NetworkInformation;
//using SmartFormat.Core.Output;
//using AForge.Imaging.Filters;
//using System.Drawing.Imaging;

namespace AeroScenery
{
    public partial class MainForm : Form
    {
        public event EventHandler StartStopClicked;
        public event EventHandler<string> ResetGridSquare;
        public Dictionary<string, GridSquareViewModel> SelectedAFS2GridSquares;
        public Dictionary<string, GridSquareViewModel> DownloadedAFS2GridSquares;
        public AFS2GridSquare SelectedAFS2GridSquare;

        //private bool mouseDownOnMap;
        private AFS2Grid afs2Grid;
        private List<DownloadThreadProgressControl> downloadThreadProgressControls;
        private AeroScenery.Common.Point mapMouseDownLocation;
        private IDataRepository dataRepository;
        private GridSquareMapper gridSquareMapper;
        private GMapOverlay activeGridSquareOverlay;
        private bool actionsRunning;
        private readonly ILog log = LogManager.GetLogger("AeroScenery");
        private GMapControlManager gMapControlManager;

        // Airport related
        private FSCloudPortService fsCloudPortService;
        private FSCloudPortMarkerManager fsCloudPortMarkerManager;

        private VersionService versionService;
        private SceneryInstaller sceneryInstaller;
        private FileManager fileManager;

        // Whether we have finished initially updating the UI with settings
        // We can therefore ignore control events until this is true
        private bool uiSetFromSettings;

        private MainFormSideTab currentMainFormSideTab;
        private int afsGridSquareSelectionSize;

        // Whether the user should be shown a dialog about how changing the selection size
        // removes any current selections.
        private bool shownSelectionSizeChangeInfo;

        private List<AFSLevel> afsLevels;
        private List<AFSLevel> elevationAfsLevels;

        private bool processCheckBoxListEvents;

        private List<ImageComboItem> orthophotoSourceItems;
        private ImageList orthophotoSourceImages;

        //#MOD_j
        private CancellationTokenSource _listeningCancellationTokenSource;
        private CancellationTokenSource _refreshPositionCancellationTokenSource;
        private int _port = 49002;

        private double movingMapTimeStamp = 0;
        private double movingMapLongitude = 0;
        private double movingMapLatitude = 0;
        private double movingMapAltitude = 0;
        private double movingMapHeading = 0;
        private double movingMapSpeed = 0;
        private double movingMapPitch = 0;
        private double movingMapRoll = 0;
        private double movingMapVerticalSpeed = 0;

        private double movingMapTimeStampLast = 0;
        private double movingMapLongitudeLast = 0;
        private double movingMapLatitudeLast = 0;
        private double movingMapAltitudeLast = 0;
        private double movingMapVerticalSpeedLast = 0;

        private double movingMapTimeStampAverage = 0;
        private double movingMapLongitudeAverage = 0;
        private double movingMapLatitudeAverage = 0;
        private double movingMapTimeStampAverageLast = 0;
        private double movingMapLongitudeAverageLast = 0;
        private double movingMapLatitudeAverageLast = 0;

        private int traceRouteCount = 0;

        private GMapOverlay airplaneMarkers;
        private GMarkerGoogle airplaneMarker;

        private GMapOverlay traceOverlay;
        private GMapRoute traceRoute;


        public MainForm()
        {
            InitializeComponent();

            this.afs2Grid = new AFS2Grid();
            this.gridSquareMapper = new GridSquareMapper();
            this.gMapControlManager = new GMapControlManager();
            this.fsCloudPortMarkerManager = new FSCloudPortMarkerManager();
            this.fsCloudPortService = new FSCloudPortService();
            this.versionService = new VersionService();
            this.sceneryInstaller = new SceneryInstaller();
            this.fileManager = new FileManager();

            this.actionsRunning = false;

            mainMap.MinZoom = 2;
            mainMap.MaxZoom = 23;
            mainMap.DragButton = MouseButtons.Left;
            mainMap.IgnoreMarkerOnMouseWheel = true;

            SelectedAFS2GridSquares = new Dictionary<string, GridSquareViewModel>();
            DownloadedAFS2GridSquares = new Dictionary<string, GridSquareViewModel>();

            this.downloadThreadProgressControls = new List<DownloadThreadProgressControl>();
            this.uiSetFromSettings = false;

            this.afsGridSquareSelectionSize = 9;
            this.gridSquareSelectionSizeToolstripCombo.SelectedIndex = 0;

            // TODO - Make this dynamic
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress1);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress2);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress3);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress4);
            //#MOD_g
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress5);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress6);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress7);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress8);

            this.downloadThreadProgress1.SetDownloadThreadNumber(1);
            this.downloadThreadProgress2.SetDownloadThreadNumber(2);
            this.downloadThreadProgress3.SetDownloadThreadNumber(3);
            this.downloadThreadProgress4.SetDownloadThreadNumber(4);
            //#MOD_g
            this.downloadThreadProgress5.SetDownloadThreadNumber(5);
            this.downloadThreadProgress6.SetDownloadThreadNumber(6);
            this.downloadThreadProgress7.SetDownloadThreadNumber(7);
            this.downloadThreadProgress8.SetDownloadThreadNumber(8);

            this.gridSquareLabel.Text = "";
            //#MOD_f
            this.gridSquareBoundaryBox.Text = "";

            this.currentMainFormSideTab = MainFormSideTab.Images;

            this.gMapControlManager.GMapControl = this.mainMap;
            this.fsCloudPortMarkerManager.GMapControl = this.mainMap;

            this.shownSelectionSizeChangeInfo = true;

        }

        public void Initialize()
        {
            ToolTip toolTip1 = new ToolTip();
            toolTip1.IsBalloon = true;
            toolTip1.InitialDelay = 500;
            //#MOD_i
            toolTip1.SetToolTip(this.generateAFS2LevelsHelpImage, "Select first the desired image resulution using the 'Image Detail (Zoom Level)' slider and then press [Choose for me].\nAeroScenery automatically selects the needed levels to be compiled for your Aerofly scenery using GeoConvert process.\nRecommended to use is level 16 with 2.389m resolution covering the whole 'Size 9' area (use higher resolutions for smaller areas).");

            //#MOD_i
            ToolTip toolTip2 = new ToolTip();
            toolTip2.IsBalloon = true;
            toolTip2.InitialDelay = 500;
            toolTip2.SetToolTip(this.chooseActionsToRunHelpImage, "Select 'Run Default actions' to automatically execute all the required steps sequentially.\nWhen GeoConvert process is completed, each selected tile can be installed using 'Install Scenery' to the path set under 'Settings'.\nBy selecting 'Choose actions to run' the steps can be executed separately resp. be done again, e.g. after editing of the stiched images.");

            //#MOD_j
            ToolTip toolTip3 = new ToolTip();
            toolTip2.IsBalloon = true;
            toolTip2.InitialDelay = 500;
            toolTip2.SetToolTip(this.movingMapHelpImage, "To use AeroScenery as a moving map, switch in AeroFly FS2/4 under 'Settings> Miscellaneaus settings>' the option 'Broadcast flight info to IP address' to 'on'.\nFigure out your 'Broadcast IP address' by clicking on the tool tip (?) symbol and set it (e.g. 'xxx.xxx.00x.255') / 'Broadcast IP Port' is '49002'\nYou may need to allow AeroScenery access in your firewall and add an exception to your antivirus protection.");


            // Initialize the AFS Levels CheckBoxLists
            afsLevels = new List<AFSLevel>();
            afsLevels.Add(new AFSLevel("Level 9", 9));
            afsLevels.Add(new AFSLevel("Level 10", 10));
            afsLevels.Add(new AFSLevel("Level 11", 11));
            afsLevels.Add(new AFSLevel("Level 12", 12));
            afsLevels.Add(new AFSLevel("Level 13", 13));
            afsLevels.Add(new AFSLevel("Level 14", 14));
            afsLevels.Add(new AFSLevel("Level 15", 15));

            //#MOD_i
            afsLevels.Add(new AFSLevel("Level 7", 7));
            afsLevels.Add(new AFSLevel("Level 8", 8));

            elevationAfsLevels = new List<AFSLevel>();
            elevationAfsLevels.Add(new AFSLevel("Level 9", 9));
            elevationAfsLevels.Add(new AFSLevel("Level 10", 10));
            elevationAfsLevels.Add(new AFSLevel("Level 11", 11));
            elevationAfsLevels.Add(new AFSLevel("Level 12", 12));
            elevationAfsLevels.Add(new AFSLevel("Level 13", 13));
            elevationAfsLevels.Add(new AFSLevel("Level 14", 14));
            elevationAfsLevels.Add(new AFSLevel("Level 15", 15));

            //#MOD_i
            elevationAfsLevels.Add(new AFSLevel("Level 7", 7));
            elevationAfsLevels.Add(new AFSLevel("Level 8", 8));


            this.afsLevelsCheckBoxList.DataSource = afsLevels;
            this.afsLevelsCheckBoxList.DisplayMember = "Name";
            this.afsLevelsCheckBoxList.ValueMember = "Level";
            this.afsLevelsCheckBoxList.ClearSelected();

            this.elevationAfsLevelCheckBoxList.DataSource = afsLevels;
            this.elevationAfsLevelCheckBoxList.DisplayMember = "Name";
            this.elevationAfsLevelCheckBoxList.ValueMember = "Level";
            this.afsLevelsCheckBoxList.ClearSelected();

            imageSourceComboBox.DisplayMember = "Text";
            imageSourceComboBox.ValueMember = "Value";

            this.orthophotoSourceImages = new ImageList();
            this.orthophotoSourceImages.TransparentColor = System.Drawing.Color.Transparent;
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.world_icon); //0
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.ch_flag); //1
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.es_flag); //2
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.jp_flag); //3
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.no_flag); //4
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.nz_flag); //5
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.se_flag); //6
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.us_flag); //7
            //#MOD_b
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.world_map); //8

            orthophotoSourceItems = new List<ImageComboItem>() {
                new ImageComboItem() { Text = "Bing", Value = OrthophotoSource.Bing, ImageIndex = 0 },
                new ImageComboItem() { Text = "Google", Value = OrthophotoSource.Google, ImageIndex = 0  },
                new ImageComboItem() { Text = "ArcGIS", Value = OrthophotoSource.ArcGIS, ImageIndex = 0  },
                new ImageComboItem() { Text = "Here WeGo", Value = OrthophotoSource.HereWeGo, ImageIndex = 0  },
                //#MOD_e
                new ImageComboItem() { Text = "Mapbox", Value = OrthophotoSource.Mapbox, ImageIndex = 0  },

                new ImageComboItem() { Text = "Geoportal (Switzerland)", Value = OrthophotoSource.CH_Geoportal, ImageIndex = 1  },
                new ImageComboItem() { Text = "GSI (Japan)", Value = OrthophotoSource.JP_GSI, ImageIndex = 3  },
                new ImageComboItem() { Text = "Gule Sider (Norway)", Value = OrthophotoSource.NO_GuleSider, ImageIndex = 4  },
                new ImageComboItem() { Text = "Hitta (Sweden)", Value = OrthophotoSource.SE_Hitta, ImageIndex = 6  },
                new ImageComboItem() { Text = "IDEIB (Balearics)", Value = OrthophotoSource.ES_IDEIB, ImageIndex = 2  },
                new ImageComboItem() { Text = "IGN (Spain)", Value = OrthophotoSource.ES_IGN, ImageIndex = 2  },
                new ImageComboItem() { Text = "Lantmateriet (Sweden)", Value = OrthophotoSource.SE_Lantmateriet, ImageIndex = 6  },
                new ImageComboItem() { Text = "Linz (New Zealand)", Value = OrthophotoSource.NZ_Linz, ImageIndex = 5  },
                new ImageComboItem() { Text = "Norge i Bilder (Norway)", Value = OrthophotoSource.NO_NorgeBilder, ImageIndex = 4  },
                new ImageComboItem() { Text = "USGS (US)", Value = OrthophotoSource.US_USGS, ImageIndex = 7  },

                //#MOD_b
                //Currently no use of the additional maps 
                //new ImageComboItem() { Text = "Google Maps (just for masking)", Value = OrthophotoSource.GoogleMaps, ImageIndex = 8  },
                //new ImageComboItem() { Text = "Google Roads (just for masking)", Value = OrthophotoSource.GoogleRoads, ImageIndex = 8  },
                //new ImageComboItem() { Text = "Google Road Map (just for masking)", Value = OrthophotoSource.GoogleRoads, ImageIndex = 8  },
                //new ImageComboItem() { Text = "OSM Maps (just for masking)", Value = OrthophotoSource.OSMMaps, ImageIndex = 8  },

                //MOD_h - No more need in the selection due to direct download vie "Action to Run" checkbox
                //new ImageComboItem() { Text = "Carto DB Light (just for masking)", Value = OrthophotoSource.CartoDBLight, ImageIndex = 8  }
            };

            imageSourceComboBox.ImageList = this.orthophotoSourceImages;
            imageSourceComboBox.DataSource = orthophotoSourceItems;

            //#MOD_g
            var settings = AeroSceneryManager.Instance.Settings;
            //Hide the boxes resp. options for running Treesdetection if no path is set in the Settings
            if (settings.TreesDetectionDirectory == "") 
            {
                runTreesDetectionCheckBox.Visible = false;
                runTreesDetectionCheckBox.Checked = false;
                runTreesDetectionMaskCheckBox.Visible= false;
                runTreesDetectionDetectionCheckBox.Visible = false;
                label5.Visible = false;
            }
            else 
            {
                runTreesDetectionCheckBox.Visible = true;
                runTreesDetectionMaskCheckBox.Visible = true;
                runTreesDetectionDetectionCheckBox.Visible = true;
                label5.Visible = true;
            }

            //#MOD_h
            // Hide the boxes resp. options for running Download Elevation if no API-Key is set in the Settings
            if (settings.OpenTopographyApiKey == "")
            {
                downloadElevationDataCheckBox.Visible = false;
                downloadElevationDataCheckBox.Checked = false;
            }
            else 
            {
                downloadElevationDataCheckBox.Visible = true;
            }

            //#MOD_i
            // Hide the boxes resp. options for enabling Download OSM Data if Option is Set under Settings
            if (settings.DownloadOSMDataEnable == false)
            {
                downloadOsmDataCheckBox.Visible = false;
                downloadOsmDataCheckBox.Checked = false;
            }
            else
            {
                downloadOsmDataCheckBox.Visible = true;
            }

            this.UpdateUIFromSettings();

            this.dataRepository = new SqlLiteDataRepository();
            this.dataRepository.Settings = AeroSceneryManager.Instance.Settings;

            this.LoadDownloadedGridSquares();

            versionToolStripLabel.Text = "v" + AeroSceneryManager.Instance.Version;

            this.processCheckBoxListEvents = true;

        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            TextBoxAppender.ConfigureTextBoxAppender(this.logTextBox);

            log.Info(String.Format("AeroScenery v{0} Started", AeroSceneryManager.Instance.Version));

            //MOD_j
            //Version check deactivated due to security issue using Newtonsoft.Json (refer to: https://github.com/advisories/GHSA-5crp-9r3c-p9vrsee)
            //this.versionService.CheckForNewerVersions();

            await this.fsCloudPortService.UpdateAirportsIfRequiredAsync();
            var airports = await this.fsCloudPortService.GetAirportsAsync();
            this.fsCloudPortMarkerManager.Airports = airports;

            if (AeroSceneryManager.Instance.Settings.ShowAirports.Value)
            {
                this.fsCloudPortMarkerManager.UpdateFSCloudPortMarkers();
            }

        }



        public void UpdateUIFromSettings()
        {
            log.Info("Updating UI from settings");
            var settings = AeroSceneryManager.Instance.Settings;

            // Orthophoto Source
            if (settings.OrthophotoSource == OrthophotoSource.USGS)
            {
                settings.OrthophotoSource = OrthophotoSource.US_USGS;
            }

            this.imageSourceComboBox.SelectedValue = settings.OrthophotoSource;

            // Zoom Level
            this.zoomLevelTrackBar.Value = settings.ZoomLevel.Value;
            this.setZoomLevelLabelText();


            // AFS Levels To Generate
            for (int i = 0; i < afsLevelsCheckBoxList.Items.Count; i++)
            {
                AFSLevel level = (AFSLevel)afsLevelsCheckBoxList.Items[i];

                //#MOD_i
                //if (settings.AFSLevelsToGenerate.Contains(level.Level))
                if ((settings.AFSLevelsToGenerate.Contains(level.Level)) && level.Level >= 9)
                {
                    level.IsChecked = true;
                    afsLevelsCheckBoxList.SetItemChecked(i, level.IsChecked);
                }

            }

            // Action set
            switch (settings.ActionSet)
            {
                case Common.ActionSet.Custom:
                    this.actionSetComboBox.SelectedIndex = 1;
                    this.SetCustomActions();
                    break;
                case Common.ActionSet.Default:
                    this.actionSetComboBox.SelectedIndex = 0;
                    this.SetDefaultActions();
                    break;
            }

            // Map stuff
            mainMap.MapProvider = GMapProviderHelper.GetGMapProvider(settings.MapControlLastMapType);
            if (settings.MapControlLastZoomLevel.HasValue && settings.MapControlLastZoomLevel > 1)
            {
                mainMap.Zoom = settings.MapControlLastZoomLevel.Value;
            }
            else
            {
                mainMap.Zoom = 5;
            }

            if (settings.MapControlLastX.HasValue && settings.MapControlLastY.HasValue)
            {
                mainMap.Position = new PointLatLng(settings.MapControlLastX.Value, settings.MapControlLastY.Value);
            }


            if (AeroSceneryManager.Instance.Settings.ShowAirports.Value)
            {
                this.fsCloudPortMarkerManager.UpdateFSCloudPortMarkers();
                this.showAirportsToolstripButton.Text = "Hide Airports";
            }
            else
            {
                this.showAirportsToolstripButton.Text = "Show Airports";
            }

            //#MOD_g
            // Hide not used downloaders/ threads
            if (settings.SimultaneousDownloads < 8)
            {
                this.downloadThreadProgress8.Visible = false;
                this.downloadThreadProgress7.Visible = false;
            }
            if (settings.SimultaneousDownloads < 6)
            {
                this.downloadThreadProgress6.Visible = false;
                this.downloadThreadProgress5.Visible = false;
            }
            if (settings.SimultaneousDownloads < 4)
            {
                this.downloadThreadProgress4.Visible = false;
                this.downloadThreadProgress3.Visible = false;
            }

            if (settings.SimultaneousDownloads < 2)
                this.downloadThreadProgress2.Visible = false;

            this.uiSetFromSettings = true;

        }

        private void SetDefaultActions()
        {
            this.downloadImageTileCheckBox.Checked = true;
            this.stitchImageTilesCheckBox.Checked = true;
            this.generateAFSFilesCheckBox.Checked = true;
            this.runGeoConvertCheckBox.Checked = true;
            //this.installSceneryIntoAFSCheckBox.Checked = true;
            //#MOD_h
            this.fixMissingTilesCheckBox.Checked = false;
            this.downloadOsmDataCheckBox.Checked = false;
            this.downloadElevationDataCheckBox.Checked = false;
            //#MOD_g
            this.runTreesDetectionCheckBox.Checked = false;

            this.downloadImageTileCheckBox.Enabled = false;
            this.stitchImageTilesCheckBox.Enabled = false;
            this.generateAFSFilesCheckBox.Enabled = false;
            this.runGeoConvertCheckBox.Enabled = false;
            //this.installSceneryIntoAFSCheckBox.Enabled = false;
            //#MOD_h
            this.fixMissingTilesCheckBox.Enabled = false;
            this.downloadOsmDataCheckBox.Enabled = false;
            this.downloadElevationDataCheckBox.Enabled = false;
            //#MOD_g
            this.runTreesDetectionCheckBox.Enabled = false;
            

        }

        private void SetCustomActions()
        {
            var settings = AeroSceneryManager.Instance.Settings;
            // Actions
            this.downloadImageTileCheckBox.Checked = settings.DownloadImageTiles.Value;
            this.stitchImageTilesCheckBox.Checked = settings.StitchImageTiles.Value;
            this.generateAFSFilesCheckBox.Checked = settings.GenerateAIDAndTMCFiles.Value;
            this.runGeoConvertCheckBox.Checked = settings.RunGeoConvert.Value;
            this.deleteStitchedImagesCheckBox.Checked = settings.DeleteStitchedImageTiles.Value;
            //this.installSceneryIntoAFSCheckBox.Checked = settings.InstallScenery.Value;

            //#MOD_h
            this.fixMissingTilesCheckBox.Checked = settings.FixMissingTiles.Value;
            this.downloadOsmDataCheckBox.Checked = settings.DownloadOsmData.Value;
            this.downloadElevationDataCheckBox.Checked = settings.DownloadElevationData.Value;
            //#MOD_g
            this.runTreesDetectionCheckBox.Checked = settings.RunTreesDetection.Value;
            this.runTreesDetectionMaskCheckBox.Checked = settings.RunTreesDetectionMask.Value;
            this.runTreesDetectionDetectionCheckBox.Checked = settings.RunTreesDetectionDetection.Value;

            this.downloadImageTileCheckBox.Enabled = true;
            this.stitchImageTilesCheckBox.Enabled = true;
            this.generateAFSFilesCheckBox.Enabled = true;
            this.runGeoConvertCheckBox.Enabled = true;
            //this.installSceneryIntoAFSCheckBox.Enabled = true;

            //#MOD_h
            this.fixMissingTilesCheckBox.Enabled = true;
            this.downloadOsmDataCheckBox.Enabled = true;
            this.downloadElevationDataCheckBox.Enabled = true;
            //#MOD_g
            this.runTreesDetectionCheckBox.Enabled = true;

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainMap.Manager.CancelTileCaching();
            mainMap.Dispose();

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            //#MOD_j
            //
            switch (this.currentMainFormSideTab)
            {
                case MainFormSideTab.Images:

                    // Are we currently running actions
                    if (this.ActionsRunning)
                    {
                        // Stop
                        this.mainTabControl.SelectedIndex = 0;
                        this.ActionsRunning = false;
                        this.ResetProgress();
                        this.UnlockUI();
                    }
                    else
                    {
                        if (SceneryGenerationProcessCanStart())
                        {
                            // Start
                            this.mainTabControl.SelectedIndex = 1;
                            this.ActionsRunning = true;
                            this.LockUI();
                        }

                    }

                    StartStopClicked(this, e);
                    break;
                case MainFormSideTab.Elevation:

                    movingMapStartStopButton_Click(this, e);
                    break;
            }

        }

        private void LockUI()
        {
            this.imageSourceComboBox.Enabled = false;
            this.zoomLevelTrackBar.Enabled = false;
            this.autoSelectAFSLevelsButton.Enabled = false;
            this.afsLevelsCheckBoxList.Enabled = false;
            this.actionSetComboBox.Enabled = false;
            this.shutdownCheckbox.Enabled = false;

            this.downloadImageTileCheckBox.Enabled = false;
            this.stitchImageTilesCheckBox.Enabled = false;
            this.generateAFSFilesCheckBox.Enabled = false;
            this.runGeoConvertCheckBox.Enabled = false;
            //#MOD_h
            this.fixMissingTilesCheckBox.Enabled = false;
            this.downloadOsmDataCheckBox.Enabled = false;
            this.downloadElevationDataCheckBox.Enabled = false;
            //#MOD_g
            this.runTreesDetectionCheckBox.Enabled = false;
            this.deleteStitchedImagesCheckBox.Enabled = false;
            this.installSceneryIntoAFSCheckBox.Enabled = false;
        }

        private void UnlockUI()
        {
            this.imageSourceComboBox.Enabled = true;
            this.zoomLevelTrackBar.Enabled = true;
            this.autoSelectAFSLevelsButton.Enabled = true;
            this.afsLevelsCheckBoxList.Enabled = true;
            this.actionSetComboBox.Enabled = true;
            //this.shutdownCheckbox.Enabled = true;

            // Only re-enable these if run custom actions is selected
            if(AeroSceneryManager.Instance.Settings.ActionSet == ActionSet.Custom)
            {
                this.downloadImageTileCheckBox.Enabled = true;
                this.stitchImageTilesCheckBox.Enabled = true;
                this.generateAFSFilesCheckBox.Enabled = true;
                this.runGeoConvertCheckBox.Enabled = true;
                //#MOD_h
                this.fixMissingTilesCheckBox.Enabled = true;
                this.downloadOsmDataCheckBox.Enabled = true;
                this.downloadElevationDataCheckBox.Enabled = true;
                //#MOD_g
                this.runTreesDetectionCheckBox.Enabled = true;
                this.deleteStitchedImagesCheckBox.Enabled = true;
                this.installSceneryIntoAFSCheckBox.Enabled = true;
            }
        }

        private void ResetProgress()
        {
            this.downloadThreadProgress1.Reset();
            this.downloadThreadProgress2.Reset();
            this.downloadThreadProgress3.Reset();
            this.downloadThreadProgress4.Reset();
            //#MOD_g
            this.downloadThreadProgress5.Reset();
            this.downloadThreadProgress6.Reset();
            this.downloadThreadProgress7.Reset();
            this.downloadThreadProgress8.Reset();

            this.currentActionProgressBar.Value = 0;
        }

        public DownloadThreadProgressControl GetDownloadThreadProgressControl(int downloadThread)
        {
            if (downloadThread < this.downloadThreadProgressControls.Count)
            {
                return this.downloadThreadProgressControls[downloadThread];
            }

            return null;
        }

        private bool SceneryGenerationProcessCanStart()
        {
            switch (AeroSceneryManager.Instance.Settings.OrthophotoSource)
            {
                case OrthophotoSource.US_USGS:

                    if (AeroSceneryManager.Instance.Settings.ZoomLevel.HasValue && AeroSceneryManager.Instance.Settings.ZoomLevel > 16)
                    {
                        var messageBox = new CustomMessageBox("USGS only provides image tile services up to zoom level 16.\nHigher resolution images are available by manual download.\n" +
                            "A way to automate the processing of these manual downloads is being researched for AeroScenery.",
                            "AeroScenery",
                            MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                        return false;
                    }

                    break;
                case OrthophotoSource.NZ_Linz:

                    if (String.IsNullOrEmpty(AeroSceneryManager.Instance.Settings.LinzApiKey))
                    {
                        var messageBox = new CustomMessageBox("A Linz API key must be set before using the Linz image source.\nThis can be set in Settings > Image Source Accounts",
                            "AeroScenery",
                            MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                        return false;
                    }

                    break;
                //MOD_e
                case OrthophotoSource.Mapbox:

                    if (String.IsNullOrEmpty(AeroSceneryManager.Instance.Settings.MapboxApiKey))
                    {
                        var messageBox = new CustomMessageBox("Mapbox Access token must be set before using the Mapbox image source.\nThis can be set in Settings > Image Source Accounts",
                            "AeroScenery",
                            MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                        return false;
                    }

                    break;

            }

            return true;
        }

        private void SelectAFSGridSquare(int x, int y)
        {
            double lat = mainMap.FromLocalToLatLng(x, y).Lat;
            double lon = mainMap.FromLocalToLatLng(x, y).Lng;

            // Get the grid square for this lat and lon
            var gridSquare = afs2Grid.GetGridSquareAtLatLon(lat, lon, this.afsGridSquareSelectionSize);

            gridSquareLabel.Text = gridSquare.Name;

            //#MOD_f
            // Create a boundary box using "NWlng, NWlat, SElng, SElat" for use in AFS2 Editor from Nabeelamjad 
            gridSquareBoundaryBox.Text = gridSquare.WestLongitude.ToString("#.#######", CultureInfo.InvariantCulture) + "," + gridSquare.NorthLatitude.ToString("#.#######", CultureInfo.InvariantCulture) + ",";
            gridSquareBoundaryBox.Text = gridSquareBoundaryBox.Text + gridSquare.EastLongitude.ToString("#.#######", CultureInfo.InvariantCulture) + "," + gridSquare.SouthLatitude.ToString("#.#######", CultureInfo.InvariantCulture);

            // Set the map overlay of any previously selected grid square to visisble
            if (this.SelectedAFS2GridSquare != null)
            {
                if (this.SelectedAFS2GridSquares.ContainsKey(this.SelectedAFS2GridSquare.Name))
                {
                    var previouslySelectedGridSquare = this.SelectedAFS2GridSquares[this.SelectedAFS2GridSquare.Name];
                    previouslySelectedGridSquare.GMapOverlay.IsVisibile = true;
                }

            }

            // Clear the previous active overlay
            if (this.activeGridSquareOverlay != null)
            {
                this.activeGridSquareOverlay.Clear();
                this.activeGridSquareOverlay.Dispose();
                this.activeGridSquareOverlay = null;
            }


            // Is this a grid square that is already selected
            if (!this.SelectedAFS2GridSquares.ContainsKey(gridSquare.Name))
            {
                // Add the selected map overlay but make it invislbe for now
                var selectedGridSquare = this.gMapControlManager.DrawGridSquare(gridSquare, GridSquareDisplayType.Selected);
                selectedGridSquare.IsVisibile = false;

                // Add the AFS2 Grid Squrea and the GMapOverlay to the selected grid squares dictionary
                var gridSquareViewModel = new GridSquareViewModel();
                gridSquareViewModel.GMapOverlay = selectedGridSquare;
                gridSquareViewModel.AFS2GridSquare = gridSquare;

                this.SelectedAFS2GridSquares.Add(gridSquare.Name, gridSquareViewModel);

                // Create the active grid square map overlay, let it be visible
                this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(gridSquare, GridSquareDisplayType.Active);
            }
            else
            {
                // Create the active grid square map overlay, let it be visible
                this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(gridSquare, GridSquareDisplayType.Active);
            }

            this.SelectedAFS2GridSquare = gridSquare;
            this.UpdateStatusStrip();
            this.UpdateToolStrip();

            log.InfoFormat("Grid square {0} selected", gridSquare.Name);
        }

        private void DeselectAFSGridSquare(int x, int y)
        {
            double lat = mainMap.FromLocalToLatLng(x, y).Lat;
            double lon = mainMap.FromLocalToLatLng(x, y).Lng;

            // Get the grid square for this lat and lon
            var gridSquare = afs2Grid.GetGridSquareAtLatLon(lat, lon, this.afsGridSquareSelectionSize);

            if (gridSquare != null)
            {
                // If this grid square is already selected, deselect it
                if (this.SelectedAFS2GridSquares.ContainsKey(gridSquare.Name))
                {
                    var squareAndOverlay = this.SelectedAFS2GridSquares[gridSquare.Name];

                    mainMap.Overlays.Remove(squareAndOverlay.GMapOverlay);
                    this.SelectedAFS2GridSquares.Remove(gridSquare.Name);
                    this.SelectedAFS2GridSquare = null;
                }

                this.SelectedAFS2GridSquare = null;
                gridSquareLabel.Text = "";
                //MOD_f
                gridSquareBoundaryBox.Text = "";

                this.activeGridSquareOverlay.Clear();
                this.activeGridSquareOverlay.Dispose();
                this.activeGridSquareOverlay = null;

                this.UpdateStatusStrip();
                this.UpdateToolStrip();
            }

        }

        /// <summary>
        /// Clears any currently selected AFSGridSquares
        /// </summary>
        private void ClearAllSelectedAFSGridSquares()
        {
            foreach (var gridSquare in this.SelectedAFS2GridSquares.Values)
            {
                mainMap.Overlays.Remove(gridSquare.GMapOverlay);
            }


            if (this.activeGridSquareOverlay != null)
            {
                this.activeGridSquareOverlay.Clear();
                this.activeGridSquareOverlay.Dispose();
                this.activeGridSquareOverlay = null;
            }

            mainMap.Refresh();

            this.SelectedAFS2GridSquares.Clear();
            this.SelectedAFS2GridSquare = null;

            this.UpdateStatusStrip();
        }

        private void ClearAllSelectedUSGSGridSquares()
        {
            // TODO
        }

        private void SelectUSGSGridSquare(int x, int y)
        {
        }
        private void DeselectUSGSGridSquare(int x, int y)
        {
        }


        private void UpdateStatusStrip()
        {
            if (this.SelectedAFS2GridSquares.Count == 1)
            {
                this.statusStripLabel1.Text = String.Format("1 Grid Square Selected");
            }
            else
            {
                this.statusStripLabel1.Text = String.Format("{0} Grid Squares Selected", this.SelectedAFS2GridSquares.Count);
            }

            if (this.SelectedAFS2GridSquares.Count > 0)
            {
                this.startStopButton.Enabled = true;
            }
            else
            {
                this.startStopButton.Enabled = false;
            }

        }

        private void UpdateToolStrip()
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                if (!this.DownloadedAFS2GridSquares.ContainsKey(this.SelectedAFS2GridSquare.Name))
                {
                    this.toolStripDownloadedLabel.Text = "Not Downloaded";
                    toolStripDownloadedLabel.Image = imageList1.Images[0];
                    resetSquareToolStripButton.Enabled = false;
                }
                else
                {
                    this.toolStripDownloadedLabel.Text = "Downloaded";
                    toolStripDownloadedLabel.Image = imageList1.Images[1];
                    resetSquareToolStripButton.Enabled = true;
                }
            }

            if (this.SelectedAFS2GridSquare != null)
            {
                this.openImageFolderToolstripButton.Enabled = true;
                this.deleteImagesToolStripButton.Enabled = true;
                this.openMapToolStripDropDownButton.Enabled = true;
                this.installSceneryToolStripButton.Enabled = true;
                //#MOD_f
                this.copyToClipboardToolStripButton.Enabled = true;
            }
            else
            {
                this.openImageFolderToolstripButton.Enabled = false;
                this.deleteImagesToolStripButton.Enabled = false;
                this.openMapToolStripDropDownButton.Enabled = false;
                this.installSceneryToolStripButton.Enabled = false;
                //#MOD_f
                this.copyToClipboardToolStripButton.Enabled = false;
            }

        }

        public void LoadDownloadedGridSquares()
        {
            var gridSquares = this.dataRepository.GetAllGridSquares();

            foreach (GridSquare gridSquare in gridSquares)
            {
                var afs2GridSqure = this.gridSquareMapper.ToAFS2GridSquare(gridSquare);
                var polygonOverlay = this.gMapControlManager.DrawGridSquare(afs2GridSqure, GridSquareDisplayType.Downloaded);

                var gridSquareViewModel = new GridSquareViewModel();
                gridSquareViewModel.GMapOverlay = polygonOverlay;
                gridSquareViewModel.AFS2GridSquare = afs2GridSqure;

                this.DownloadedAFS2GridSquares[afs2GridSqure.Name] = gridSquareViewModel;

            }
        }

        public void AddDownloadedGridSquare(AFS2GridSquare afs2GridSqure)
        {
            var polygonOverlay = this.gMapControlManager.DrawGridSquare(afs2GridSqure, GridSquareDisplayType.Downloaded);

            var gridSquareViewModel = new GridSquareViewModel();
            gridSquareViewModel.GMapOverlay = polygonOverlay;
            gridSquareViewModel.AFS2GridSquare = afs2GridSqure;

            this.DownloadedAFS2GridSquares[afs2GridSqure.Name] = gridSquareViewModel;

        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();
            settingsForm.Show();
            if (settingsForm.StartPosition == FormStartPosition.CenterParent)
            {
                var x = Location.X + (Width - settingsForm.Width) / 2;
                var y = Location.Y + (Height - settingsForm.Height) / 2;
                settingsForm.Location = new System.Drawing.Point(Math.Max(x, 0), Math.Max(y, 0));
            }

        }

        private void MainMap_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.mapMouseDownLocation = new AeroScenery.Common.Point(e.X, e.Y);
            }
        }

        private void MainMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.mapMouseDownLocation != null)
            {
                // Are we showing an airport popup
                if (this.fsCloudPortMarkerManager.PopupShown)
                {
                    // The first click is a click to open it.
                    // We therefore need to count clicks and close after the second click
                    if (this.fsCloudPortMarkerManager.ClickCount > 0)
                    {
                        this.fsCloudPortMarkerManager.CloseAirportPopup();                    
                    }

                    this.fsCloudPortMarkerManager.ClickCount++;

                }
                else
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        var mouseUpLocation = new System.Drawing.Point(e.X, e.Y);

                        var dx = Math.Abs(mouseUpLocation.X - this.mapMouseDownLocation.X);
                        var dy = Math.Abs(mouseUpLocation.Y - this.mapMouseDownLocation.Y);

                        // If there was little movement it was probably meant as a click
                        // rather than a drag
                        if (dx < 10 && dy < 10)
                        {
                            if (!this.mainMap.IsMouseOverMarker)
                            {
                                switch (this.currentMainFormSideTab)
                                {
                                    case MainFormSideTab.Images:
                                        this.SelectAFSGridSquare(e.X, e.Y);
                                        break;
                                    case MainFormSideTab.Elevation:
                                        this.SelectUSGSGridSquare(e.X, e.Y);
                                        //#TRY
                                        //this.SelectAFSGridSquare(e.X, e.Y);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            if (AeroSceneryManager.Instance.Settings.ShowAirports.Value)
                            {
                                this.fsCloudPortMarkerManager.UpdateFSCloudPortMarkers();
                            }
                        }
                    }
                }

            }

        }

        private void mainMap_DoubleClick(object sender, EventArgs e)
        {
            var evt = (MouseEventArgs)e;
            this.mapMouseDownLocation = null;

            double lat = mainMap.FromLocalToLatLng(evt.X, evt.Y).Lat;
            double lon = mainMap.FromLocalToLatLng(evt.X, evt.Y).Lng;

            // Get the grid square for this lat and lon
            var gridSquare = afs2Grid.GetGridSquareAtLatLon(lat, lon, this.afsGridSquareSelectionSize);

            if (this.SelectedAFS2GridSquares.ContainsKey(gridSquare.Name))
            {
                switch (this.currentMainFormSideTab)
                {
                    case MainFormSideTab.Images:
                        this.DeselectAFSGridSquare(evt.X, evt.Y);
                        break;
                    case MainFormSideTab.Elevation:
                        this.DeselectUSGSGridSquare(evt.X, evt.Y);
                        break;
                }
            }
        }

        private void openInGoogleMapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                var selectedGridSquare = this.SelectedAFS2GridSquare;
                var googleMapsUrl = "https://www.google.com/maps/@{0},{1},60000m/data=!3m1!1e3";

                string latStr = selectedGridSquare.GetCenter().Lat.ToString("#.####################", CultureInfo.InvariantCulture);
                string lngStr = selectedGridSquare.GetCenter().Lng.ToString("#.####################", CultureInfo.InvariantCulture);

                System.Diagnostics.Process.Start(String.Format(googleMapsUrl, latStr, lngStr));

                //#MOD_f
                // Additionally copy the center coordinates to the clipoard as "<lon> <lat>" for use in TSC-Files of Aerofly
                var centerCoodinateStr = selectedGridSquare.GetCenter().Lng.ToString("#.########", CultureInfo.InvariantCulture) + " " + selectedGridSquare.GetCenter().Lat.ToString("#.########", CultureInfo.InvariantCulture);
                Clipboard.SetData(DataFormats.Text, (Object)centerCoodinateStr);
            }
        }

        private void openInBingMApsToolStripMenuItem_Click(object sender, EventArgs e)
        {       
            if (this.SelectedAFS2GridSquare != null)
            {
                var selectedGridSquare = this.SelectedAFS2GridSquare;
                var bingMapsUrl = "https://www.bing.com/maps/default.aspx?cp={0}~{1}&lvl=10&style=h";

                string latStr = selectedGridSquare.GetCenter().Lat.ToString("#.####################", CultureInfo.InvariantCulture);
                string lngStr = selectedGridSquare.GetCenter().Lng.ToString("#.####################", CultureInfo.InvariantCulture);

                System.Diagnostics.Process.Start(String.Format(bingMapsUrl, latStr, lngStr));

                //#MOD_f
                // Additionally copy the center coordinates to the clipoard as "<lon> <lat>" for use in TSC-Files of Aerofly
                var centerCoodinateStr = selectedGridSquare.GetCenter().Lng.ToString("#.########", CultureInfo.InvariantCulture) + " " + selectedGridSquare.GetCenter().Lat.ToString("#.########", CultureInfo.InvariantCulture);
                Clipboard.SetData(DataFormats.Text, (Object)centerCoodinateStr);
            }
        }
        //#MOD_f
        // Additional "Open in Map" type for Google Earth (Web-version only)
        private void openInGoogleEarthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                var selectedGridSquare = this.SelectedAFS2GridSquare;
                var googleEarthUrl = "https://earth.google.com/web/@{0},{1},2000a,40000d,40y,0h,80t,0r";

                string latStr = selectedGridSquare.GetCenter().Lat.ToString("#.####################", CultureInfo.InvariantCulture);
                string lngStr = selectedGridSquare.GetCenter().Lng.ToString("#.####################", CultureInfo.InvariantCulture);

                System.Diagnostics.Process.Start(String.Format(googleEarthUrl, latStr, lngStr));

                //#MOD_f
                // Additionally copy the center coordinates to the clipoard as "<lon> <lat>" for use in TSC-Files of Aerofly
                var centerCoodinateStr = selectedGridSquare.GetCenter().Lng.ToString("#.########", CultureInfo.InvariantCulture) + " " + selectedGridSquare.GetCenter().Lat.ToString("#.########", CultureInfo.InvariantCulture);
                Clipboard.SetData(DataFormats.Text, (Object)centerCoodinateStr);
            }
        }
        

        private void openImageFolderToolstripButton_Click(object sender, EventArgs e)
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + this.SelectedAFS2GridSquare.Name;

                if (Directory.Exists(gridSquareDirectory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = gridSquareDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });

                    //#MOD_f
                    // Additionally copy the name of the selected gridsquare to the clipboard
                    Clipboard.SetData(DataFormats.Text, (Object)this.SelectedAFS2GridSquare.Name);

                }
                else
                {
                    var messageBox = new CustomMessageBox(String.Format("There is no image folder yet for grid square {0}", this.SelectedAFS2GridSquare.Name), 
                        "AeroScenery", 
                        MessageBoxIcon.Information);

                    messageBox.ShowDialog();
                }
            }
        }

        private async void deleteImagesToolStripButton_ClickAsync(object sender, EventArgs e)
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + this.SelectedAFS2GridSquare.Name;

                if (Directory.Exists(gridSquareDirectory))
                {
                    using (var deleteSquareOptionsForm = new DeleteSquareOptionsForm())
                    {
                        var result = deleteSquareOptionsForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            var deleteTask = this.fileManager.DeleteGridSquareFilesAsync(gridSquareDirectory, deleteSquareOptionsForm.DeleteMapImageTiles, deleteSquareOptionsForm.DeleteStitchedImages,
                                deleteSquareOptionsForm.DeleteGeoconvertRawImages, deleteSquareOptionsForm.DeleteTTCFiles);

                            var fileOperationProgressForm = new FileOperationProgressForm();
                            fileOperationProgressForm.MessageText = "Deleting Files";
                            fileOperationProgressForm.Title = "Deleting Files";

                            fileOperationProgressForm.FileOperationTask = deleteTask;
                            await fileOperationProgressForm.DoTaskAsync();
                            fileOperationProgressForm = null;

                            //#DEVL
                            // Additionally delete the OSM folder in the root folder of the tile (seperate treatment needed) & also the new trees folder should be added as option!
                            if (deleteSquareOptionsForm.DeleteOSMFolder == true) 
                            {
                                // ...
                            }
                        }
                    }

                }
                else
                {
                    var messageBox = new CustomMessageBox(String.Format("There is no image folder yet for grid square {0}", this.SelectedAFS2GridSquare.Name),
                        "AeroScenery",
                        MessageBoxIcon.Information);

                    messageBox.ShowDialog();
                }
            }
        }

        private void imageSourceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.uiSetFromSettings)
            {
                var settings = AeroSceneryManager.Instance.Settings;
                settings.OrthophotoSource = (OrthophotoSource)this.imageSourceComboBox.SelectedValue;

                AeroSceneryManager.Instance.SaveSettings();
            }

        }

        private void actionSetComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.uiSetFromSettings)
            {
                switch (this.actionSetComboBox.SelectedIndex)
                {
                    // Default
                    case 0:
                        AeroSceneryManager.Instance.Settings.ActionSet = Common.ActionSet.Default;
                        this.SetDefaultActions();
                        break;
                    // Custom
                    case 1:
                        AeroSceneryManager.Instance.Settings.ActionSet = Common.ActionSet.Custom;
                        this.SetCustomActions();
                        break;
                }

                AeroSceneryManager.Instance.SaveSettings();
            }

        }

        private void setZoomLevelLabelText()
        {
            double metersPerPixel = 0;

            switch (this.zoomLevelTrackBar.Value)
            {
                case 12:
                    metersPerPixel = 38.2185;
                    break;
                case 13:
                    metersPerPixel = 19.1093;
                    break;
                case 14:
                    metersPerPixel = 9.5546;
                    break;
                case 15:
                    metersPerPixel = 4.7773;
                    break;
                case 16:
                    metersPerPixel = 2.3887;
                    break;
                case 17:
                    metersPerPixel = 1.1943;
                    break;
                case 18:
                    metersPerPixel = 0.5972;
                    break;
                case 19:
                    metersPerPixel = 0.2986;
                    break;
                case 20:
                    metersPerPixel = 0.1493;
                    break;
            }

            this.zoomLevelLabel.Text = String.Format("{0} - {1} meters/pixel", this.zoomLevelTrackBar.Value, metersPerPixel.ToString("0.000"));
        }

        private void zoomLevelTrackBar_Scroll(object sender, EventArgs e)
        {
            this.setZoomLevelLabelText();
            AeroSceneryManager.Instance.Settings.ZoomLevel = this.zoomLevelTrackBar.Value;
            AeroSceneryManager.Instance.SaveSettings();
        }

        private void gridSquareLevelsCheckBoxList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (uiSetFromSettings && processCheckBoxListEvents)
            {
                var settings = AeroSceneryManager.Instance.Settings;

                //var checkedLevel = e.Index + 9;
                var afsLevel = (AFSLevel)this.afsLevelsCheckBoxList.Items[e.Index];                
                var checkedLevel = afsLevel.Level;

                if (e.NewValue == CheckState.Checked)
                {
                    afsLevel.IsChecked = true;
                }
                else
                {
                    afsLevel.IsChecked = false;
                }

                // Don't let anyone select levels that are smaller than the grid square selection size
                if (checkedLevel < this.afsGridSquareSelectionSize)
                {
                    e.NewValue = e.CurrentValue;

                    CustomMessageBox message = new CustomMessageBox("You cannnot selected an AFS Level bigger than the grid square selection size.", 
                        "AeroScenery", MessageBoxIcon.Information);

                    message.ShowDialog();
                }
                else
                {
                    if (settings.AFSLevelsToGenerate.Contains(checkedLevel))
                    {
                        settings.AFSLevelsToGenerate.Remove(checkedLevel);
                    }
                    else
                    {
                        settings.AFSLevelsToGenerate.Add(checkedLevel);
                    }
                }

                AeroSceneryManager.Instance.SaveSettings();
            }

        }

        private void downloadImageTileCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (downloadImageTileCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.DownloadImageTiles = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.DownloadImageTiles = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (fixMissingTilesCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.FixMissingTiles = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.FixMissingTiles = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void stitchImageTilesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (stitchImageTilesCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.StitchImageTiles = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.StitchImageTiles = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void generateAFSFilesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (generateAFSFilesCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.GenerateAIDAndTMCFiles = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.GenerateAIDAndTMCFiles = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void runGeoConvertCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (runGeoConvertCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.RunGeoConvert = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.RunGeoConvert = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        //#MOD_h
        private void downloadOsmDataCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (downloadOsmDataCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.DownloadOsmData = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.DownloadOsmData = false;
            }
            AeroSceneryManager.Instance.SaveSettings();
        }
        //#MOD_g
        private void runTreesDetectionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (runTreesDetectionCheckBox.Checked)
            {
                this.runTreesDetectionDetectionCheckBox.Enabled = true;
                this.runTreesDetectionDetectionCheckBox.Checked = true;
                this.runTreesDetectionMaskCheckBox.Enabled = true;
                AeroSceneryManager.Instance.Settings.RunTreesDetection = true; 
            }
            else
            {
                this.runTreesDetectionDetectionCheckBox.Enabled = false;
                this.runTreesDetectionDetectionCheckBox.Checked = false;
                this.runTreesDetectionMaskCheckBox.Enabled = false;
                AeroSceneryManager.Instance.Settings.RunTreesDetection = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void downloadElevationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (downloadElevationDataCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.DownloadElevationData = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.DownloadElevationData = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void deleteStitchedImagesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (deleteStitchedImagesCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.DeleteStitchedImageTiles = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.DeleteStitchedImageTiles = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void installSceneryIntoAFSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (downloadImageTileCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.InstallScenery = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.InstallScenery = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void helpToolStripButton_Click(object sender, EventArgs e)
        {
            var url = "https://github.com/nickhod/aeroscenery";
            System.Diagnostics.Process.Start(url);
        }

        private void getSDKToolStripButton_Click(object sender, EventArgs e)
        {
            //#MOD_i
            //var url = "https://www.aerofly.com/community/filebase/index.php?file/2-sdk-tools/";
            var url = "https://www.aerofly-sim.de/aerofly_fs_2_sdk/";
            System.Diagnostics.Process.Start(url);
        }

        public void UpdateParentTaskLabel(string parentTask)
        {
            this.parentTaskLabel.Text = parentTask;
        }

        public void UpdateChildTaskLabel(string childTask)
        {
            this.childTaskLabel.Text = childTask;
        }

        public void UpdateTaskLabels(string parentTask, string childTask)
        {
            this.parentTaskLabel.Text = parentTask;
            this.childTaskLabel.Text = childTask;
        }

        public bool ActionsRunning
        {
            get
            {
                return this.actionsRunning;
            }
            set
            {
                this.actionsRunning = value;

                if (this.actionsRunning)
                {
                    this.startStopButton.Text = "Stop";
                }
                else
                {
                    this.startStopButton.Text = "Start";
                }
            }
        }

        private void resetSquareToolStripButton_Click(object sender, EventArgs e)
        {
            var messageBox = new CustomMessageBox("Are you sure you want to reset the downloaded status of this grid square? (No files will be deleted).",
                "AeroScenery",
                MessageBoxIcon.Question);

            messageBox.SetButtons(
                new string[] { "Yes", "No" },
                new DialogResult[] { DialogResult.Yes, DialogResult.No });

            DialogResult result = messageBox.ShowDialog();

            if (result == DialogResult.Yes)
            {
                if (this.SelectedAFS2GridSquare != null)
                {
                    if (this.DownloadedAFS2GridSquares.ContainsKey(this.SelectedAFS2GridSquare.Name))
                    {
                        ResetGridSquare(this, this.SelectedAFS2GridSquare.Name);

                        var downloadedGridSquare = this.DownloadedAFS2GridSquares[this.SelectedAFS2GridSquare.Name];
                        downloadedGridSquare.GMapOverlay.Clear();
                        downloadedGridSquare.GMapOverlay.Dispose();

                        this.DownloadedAFS2GridSquares.Remove(this.SelectedAFS2GridSquare.Name);

                        var selectedGridSquare = this.SelectedAFS2GridSquares[this.SelectedAFS2GridSquare.Name];

                        if (selectedGridSquare != null)
                        {
                            selectedGridSquare.GMapOverlay.Clear();
                            selectedGridSquare.GMapOverlay.Dispose();
                            selectedGridSquare.GMapOverlay = null;

                            this.SelectedAFS2GridSquares.Remove(this.SelectedAFS2GridSquare.Name);
                        }

                        this.SelectedAFS2GridSquare = null;
                        gridSquareLabel.Text = "";
                        //#MOD_f
                        gridSquareBoundaryBox.Text = "";

                        this.activeGridSquareOverlay.Clear();
                        this.activeGridSquareOverlay.Dispose();
                        this.activeGridSquareOverlay = null;

                        this.UpdateStatusStrip();

                    }
                }
            }
        }

        public int CurrentActionProgressPercentage
        {
            get
            {
                return this.currentActionProgressBar.Value;
            }
            set
            {
                this.currentActionProgressBar.Value = value;
            }
        }

        public void ActionsComplete()
        {
            this.mainTabControl.SelectedIndex = 0;
            this.ActionsRunning = false;
            this.ResetProgress();
            this.UnlockUI();
        }

        private void gridSquareSelectionSizeToolstripCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;

            // If any grid squares are selected and the message hasn't been show before,
            // show a message to say that the selection will be lost when changing size
            if (this.SelectedAFS2GridSquares.Count() > 0 && this.shownSelectionSizeChangeInfo)
            {
                var messageBox = new CustomMessageBox("Changing the grid square selection size removes any current selections.\nAeroScenery can only process one size of grid square per run.",
                    "AeroScenery", MessageBoxIcon.Information);

                messageBox.ShowDialog();

                this.shownSelectionSizeChangeInfo = false;
            }

            int? minAFSLevel = null;

            switch (this.gridSquareSelectionSizeToolstripCombo.SelectedIndex)
            {
                // 9
                case 0:
                    this.afsGridSquareSelectionSize = 9;
                    this.ClearAllSelectedAFSGridSquares();
                    //#MOD_i
                    minAFSLevel = 9;
                    break;

                // 10
                case 1:
                    this.afsGridSquareSelectionSize = 10;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 10;
                    break;

                // 11
                case 2:
                    this.afsGridSquareSelectionSize = 11;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 11;
                    break;

                // 12
                case 3:
                    this.afsGridSquareSelectionSize = 12;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 12;
                    break;

                // 13
                case 4:
                    this.afsGridSquareSelectionSize = 13;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 13;
                    break;

                // 14
                case 5:
                    this.afsGridSquareSelectionSize = 14;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 14;
                    break;

                //#MOD_i
                // 7
                case 6:
                    this.afsGridSquareSelectionSize = 7;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 7;
                    break;

                // 8
                case 7:
                    this.afsGridSquareSelectionSize = 8;
                    this.ClearAllSelectedAFSGridSquares();
                    minAFSLevel = 8;
                    break;
            }

            if (minAFSLevel.HasValue)
            {
                this.processCheckBoxListEvents = false;

                for (int index = 0; index < this.afsLevelsCheckBoxList.Items.Count; ++index)
                {
                    var afsLevel = (AFSLevel)this.afsLevelsCheckBoxList.Items[index];

                    if (afsLevel.Level < minAFSLevel.Value)
                    {
                        this.afsLevelsCheckBoxList.SetItemChecked(index, false);
                        afsLevel.IsChecked = false;
                        settings.AFSLevelsToGenerate.Remove(afsLevel.Level);
                    }
                }

                AeroSceneryManager.Instance.SaveSettings();

                this.processCheckBoxListEvents = true;
            }
        }

        //private async void usgsTestButton_Click(object sender, EventArgs e)
        //{
        //    USGSInventoryService service = new USGSInventoryService();

        //    var loginRequest = new LoginRequest();
        //    loginRequest.Username = AeroSceneryManager.Instance.Settings.USGSUsername;
        //    loginRequest.Password = AeroSceneryManager.Instance.Settings.USGSPassword;
        //    loginRequest.CatalogId = CatalogType.EarthExplorer;
        //    loginRequest.AuthType = "EROS";
        //    var login = await service.LoginAsync(loginRequest);

        //    //var datasetSearchRequest = new DatasetSearchRequest();
        //    //datasetSearchRequest.DatasetName = "ASTER";
        //    //var datasets = await service.DatasetSearchAsync(datasetSearchRequest);

        //    var searchRequest = new SceneSearchRequest();
        //    //searchRequest.DatasetName = "ASTER_GLOBAL_DEM";
        //    searchRequest.DatasetName = "ASTER_GLOBAL_DEM_DE";
        //    //searchRequest.DatasetName = "LANDSAT_8";

        //    var spatialFilter = new SpatialFilter();
        //    spatialFilter.FilterType = "mbr";
        //    spatialFilter.LowerLeft = new Coordinate(51.469400, -3.163811);
        //    spatialFilter.UpperRight = new Coordinate(51.469400, -3.163811);
        //    //spatialFilter.LowerLeft = new Coordinate(75, -135);
        //    //spatialFilter.UpperRight = new Coordinate(90, -120);
        //    searchRequest.SpatialFilter = spatialFilter;

        //    var searchResult = await service.SceneSearch(searchRequest);


        //    // This doesn't work without special permission
        //    //var downloadOptionsRequest = new DownloadOptionsRequest();
        //    //downloadOptionsRequest.DatasetName = "ASTER_GLOBAL_DEM_DE";
        //    //downloadOptionsRequest.EntityIds = new string[] { "ASTGDEMV2_0N51W004" };

        //    //var asdfdsf = await service.DownloadOptions(downloadOptionsRequest);

        //    //int i = 0;

        //    USGSScraper scraper = new USGSScraper();
        //    await scraper.LoginAsync(AeroSceneryManager.Instance.Settings.USGSUsername, AeroSceneryManager.Instance.Settings.USGSPassword);

        //    var downloadPageUrl = "https://earthexplorer.usgs.gov/download/external/options/ASTER_GLOBAL_DEM_DE/ASTGDEMV2_0N51W004/INVSVC/";

        //    await scraper.DownloadAsync(downloadPageUrl, @"E:\Temp");
        //}

        
        /*
        private async void button2_Click(object sender, EventArgs e)
        {
        }
        */

        private void sideTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.sideTabControl.SelectedIndex)
            {
                case 0:
                    this.currentMainFormSideTab = MainFormSideTab.Images;
                    this.ClearAllSelectedUSGSGridSquares();
                    //#TRY_y
                    this.startStopButton.Enabled = false;
                    break;
                case 1:
                    this.currentMainFormSideTab = MainFormSideTab.Elevation;
                    this.ClearAllSelectedAFSGridSquares();
                    //#TRY_y
                    this.startStopButton.Enabled = true;
                    break;
            }

        }

        private void CultivationEditorForm_CultivationEditorFormClosed(object sender, EventArgs e)
        {
            this.mainMap.DisableFocusOnMouseEnter = false;
        }

        private void AutoSelectAFSLevelsButton_Click(object sender, EventArgs e)
        {
            var zoomLevel = AeroSceneryManager.Instance.Settings.ZoomLevel;

            List<int> afsLevels = new List<int>();

            switch (this.afsGridSquareSelectionSize)
            {
                //#MOD_i
                case 7:
                    afsLevels.Add(7);

                    break;

                //#MOD_i
                case 8:
                    afsLevels.Add(8);

                    break;

                case 9:

                    afsLevels.Add(9);
                    //#MOD_e
                    afsLevels.Add(10);
                    afsLevels.Add(11);
                    afsLevels.Add(12);

                    if (zoomLevel > 15)
                    {
                        afsLevels.Add(13);
                    }

                    if (zoomLevel > 16)
                    {
                        afsLevels.Add(14);                    
                    }

                    if (zoomLevel > 17)
                    {
                        afsLevels.Add(15);
                    }

                    break;


                case 10:

                    afsLevels.Add(10);
                    afsLevels.Add(11);
                    afsLevels.Add(12);

                    if (zoomLevel > 15)
                    {
                        afsLevels.Add(13);
                    }

                    if (zoomLevel > 16)
                    {
                        afsLevels.Add(14);
                    }

                    if (zoomLevel > 17)
                    {
                        afsLevels.Add(15);
                    }

                    break;

                case 11:

                    afsLevels.Add(11);
                    afsLevels.Add(12);
                    afsLevels.Add(13);

                    if (zoomLevel > 16)
                    {
                        afsLevels.Add(14);
                    }

                    if (zoomLevel > 17)
                    {
                        afsLevels.Add(15);
                    }

                    break;

                case 12:

                    afsLevels.Add(12);
                    afsLevels.Add(13);

                    if (zoomLevel > 16)
                    {
                        afsLevels.Add(14);
                    }

                    if (zoomLevel > 17)
                    {
                        afsLevels.Add(15);
                    }

                    break;
                case 13:

                    afsLevels.Add(13);
                    afsLevels.Add(14);

                    if (zoomLevel > 17)
                    {
                        afsLevels.Add(15);
                    }

                    break;
                case 14:
                    afsLevels.Add(14);

                    if (zoomLevel > 17)
                    {
                        afsLevels.Add(15);
                    }

                    break;
            }

            this.SetAFSLevels(afsLevels);
        }

        private void SetAFSLevels(List<int> afsLevels)
        {
            // Uncheck everything first
            for (int i = 0; i < afsLevelsCheckBoxList.Items.Count; i++)
            {
                AFSLevel level = (AFSLevel)afsLevelsCheckBoxList.Items[i];
                level.IsChecked = false;
                afsLevelsCheckBoxList.SetItemChecked(i, false);
            }

            // Check what needs to be checked
            for (int i = 0; i < afsLevelsCheckBoxList.Items.Count; i++)
            {
                AFSLevel level = (AFSLevel)afsLevelsCheckBoxList.Items[i];

                if (afsLevels.Contains(level.Level))
                {
                    level.IsChecked = true;
                    afsLevelsCheckBoxList.SetItemChecked(i, level.IsChecked);
                }

            }

            AeroSceneryManager.Instance.Settings.AFSLevelsToGenerate = afsLevels;
        }


        private void MainMap_OnMapZoomChanged()
        {
            AeroSceneryManager.Instance.Settings.MapControlLastZoomLevel = Convert.ToInt32(this.mainMap.Zoom);

            if (AeroSceneryManager.Instance.Settings.ShowAirports.Value)
            {
                this.fsCloudPortMarkerManager.UpdateFSCloudPortMarkers();
            }
        }

        private void MainMap_OnMapDrag()
        {
            AeroSceneryManager.Instance.Settings.MapControlLastX = this.mainMap.Position.Lat;
            AeroSceneryManager.Instance.Settings.MapControlLastY = this.mainMap.Position.Lng;
        }

        private void MainMap_OnMarkerClick(GMap.NET.WindowsForms.GMapMarker item, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (item.Tag != null) 
                {
                    var icao = item.Tag.ToString();
                    this.fsCloudPortMarkerManager.ShowAirportPopup(icao, this, e.Location);
                }
            }
        }

        private void showAirportsToolstripButton_Click(object sender, EventArgs e)
        {
            // We need to hide airports
            if (AeroSceneryManager.Instance.Settings.ShowAirports.Value)
            {
                AeroSceneryManager.Instance.Settings.ShowAirports = false;
                this.showAirportsToolstripButton.Text = "Show Airports";
                this.fsCloudPortMarkerManager.RemoveAllFSCloudPortMarkers();

            }
            // We need to show airports
            else
            {
                AeroSceneryManager.Instance.Settings.ShowAirports = true;
                this.showAirportsToolstripButton.Text = "Hide Airports";
                this.fsCloudPortMarkerManager.UpdateFSCloudPortMarkers();
            }
        }

        private void mapTypeToolStripDropDown_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch(e.ClickedItem.Tag)
            {
                case "GoogleHybrid":
                    this.mainMap.MapProvider = GMapProviders.GoogleHybridMap;
                    break;
                case "GoogleSatellite":
                    this.mainMap.MapProvider = GMapProviders.GoogleSatelliteMap;
                    break;
                case "GoogleStandard":
                    this.mainMap.MapProvider = GMapProviders.GoogleMap;
                    break;
                case "BingHybrid":
                    this.mainMap.MapProvider = GMapProviders.BingHybridMap;
                    break;
                case "BingSatellite":
                    this.mainMap.MapProvider = GMapProviders.BingSatelliteMap;
                    break;
                case "BingStandard":
                    this.mainMap.MapProvider = GMapProviders.BingMap;
                    break;
                case "OpenStreetMap":
                    //MOD_f
                    // Use "Open Cycle Map" instead of "Open Street Map", cause it doesn't work anymore
                    //this.mainMap.MapProvider = GMapProviders.OpenStreetMap;
                    this.mainMap.MapProvider = GMapProviders.OpenCycleMap;
                    break;
                //MOD_f
                case "GoogleTerrain":
                    this.mainMap.MapProvider = GMapProviders.GoogleTerrainMap;
                    break;
            }

            var mapProviderName = this.mainMap.MapProvider.GetType();
            AeroSceneryManager.Instance.Settings.MapControlLastMapType = mapProviderName.Name.Replace("Provider", "");
        }

        private void MainTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // Prevent users returning to the map page if actions are running
            if (actionsRunning && e.TabPageIndex == 0)
            {
                e.Cancel = true;
            }
        }

        private void afsLevelsCheckBoxList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void afsLevelsCheckBoxList_Leave(object sender, EventArgs e)
        {
            this.afsLevelsCheckBoxList.ClearSelected();
        }

        private void sideTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // Prevent users returning to the map page if actions are running
            if (actionsRunning)
            {
                e.Cancel = true;
            }
        }

        private async void InstallSceneryToolStripButton_ClickAsync(object sender, EventArgs e)
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + this.SelectedAFS2GridSquare.Name;

                if (Directory.Exists(gridSquareDirectory))
                {
                    var result = this.sceneryInstaller.ConfirmSceneryInstallation(this.SelectedAFS2GridSquare);

                    if (result == DialogResult.Yes)
                    {
                        var ttcFiles = new List<string>();

                        var duplicateResult = this.sceneryInstaller.CheckForDuplicateTTCFiles(this.SelectedAFS2GridSquare, out ttcFiles);

                        if (duplicateResult == null || duplicateResult == DialogResult.OK)
                        {
                            var installTask = this.sceneryInstaller.InstallSceneryAsync(this.SelectedAFS2GridSquare, ttcFiles);

                            var fileOperationProgressForm = new FileOperationProgressForm();
                            fileOperationProgressForm.MessageText = "Installing Scenery";
                            fileOperationProgressForm.Title = "Installing Scenery";

                            fileOperationProgressForm.FileOperationTask = installTask;
                            await fileOperationProgressForm.DoTaskAsync();
                            fileOperationProgressForm = null;
                        }
                    }

                }
                else
                {
                    var messageBox = new CustomMessageBox(String.Format("There is no image folder yet for grid square {0}", this.SelectedAFS2GridSquare.Name),
                        "AeroScenery",
                        MessageBoxIcon.Information);

                    messageBox.ShowDialog();
                }

            }

        }

        private void openMapToolStripDropDownButton_Click(object sender, EventArgs e)
        {

        }
        private void copyToClipboardToolStripButton_Click(object sender, EventArgs e)
        {
            //#MOD_f
            Clipboard.SetData(DataFormats.Text, (Object)gridSquareBoundaryBox.Text);

        }

        private void versionToolStripLabel_Click(object sender, EventArgs e)
        {

        }

        //#MOD_g
        private void RunTreesDetectionMask_CheckedChanged(object sender, EventArgs e)
        {
            if (runTreesDetectionMaskCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.RunTreesDetectionMask = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.RunTreesDetectionMask = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        //#MOD_g
        private void RunTreesDetectionDetectionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (runTreesDetectionDetectionCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.RunTreesDetectionDetection = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.RunTreesDetectionDetection = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }
        //#MOD_g

        private void openUserFolderToolstripButton_Click(object sender, EventArgs e)
        {
            if (AeroSceneryManager.Instance.Settings.AFS2UserDirectory != null)
            {
                if (Directory.Exists(AeroSceneryManager.Instance.Settings.AFS2UserDirectory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = AeroSceneryManager.Instance.Settings.AFS2UserDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    var messageBox = new CustomMessageBox("No AFS2 user folder found",
                    "AeroScenery",
                        MessageBoxIcon.Information);

                    messageBox.ShowDialog();
                }
            }
        }

        private void openSceneryEditorToolStripButton_Click(object sender, EventArgs e)
        {
            var afs2EditorUrl = "https://afs2-editor.nabeelamjad.co.uk/";

            System.Diagnostics.Process.Start(afs2EditorUrl);

        }

        private void toolStripSearchTileButton_Click(object sender, EventArgs e)
        {
            //#MOD_g
            string inputBoxText = "";
            if (CustomeInputBox.InputBox("Tile/ Location Search", "Tile or Location (e.g. '8500_a500' or 'Paris, France'):", ref inputBoxText) == DialogResult.OK)
            {
                AFS2GridSquare aFS2GridSquareSearch = new AFS2GridSquare();
                AFS2Grid aFS2Grid = new AFS2Grid();
                string squareName = inputBoxText;
                //
                if (squareName.Length > 9) 
                {
                    squareName = inputBoxText.Substring(inputBoxText.Length - 9, 9);
                }
                aFS2GridSquareSearch = aFS2Grid.GetGridSquareName(squareName, this.afsGridSquareSelectionSize);

                if (aFS2GridSquareSearch != null)
                {
                    this.ClearAllSelectedAFSGridSquares();

                    this.mainMap.Position = new PointLatLng((aFS2GridSquareSearch.NorthLatitude + aFS2GridSquareSearch.SouthLatitude) / 2, (aFS2GridSquareSearch.WestLongitude + aFS2GridSquareSearch.EastLongitude) / 2);
                    this.mainMap.Zoom = 10;
                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2GridSquareSearch, GridSquareDisplayType.Show);
                }
                else 
                {
                    //#MOD_j
                    // Perform geocoding for location search using OpenSreeet Map Data 
                    var geoCoder = GMapProviders.OpenStreetMap;

                    // Receive list of points found and status code
                    List<PointLatLng> geocodingPointList;
                    var locations = geoCoder.GetPoints(inputBoxText, out geocodingPointList);

                    // Check whether the search was successful
                    if (geocodingPointList != null && geocodingPointList.Count > 0)
                    {
                        // Use the first item from the list (if several were found)
                        var location = geocodingPointList.First();

                        // Show the coordinates on the map
                        this.mainMap.Position = new PointLatLng(location.Lat, location.Lng);
                        this.mainMap.Zoom = 12;
                    }
                    else
                    {
                        var messageBox = new CustomMessageBox(String.Format("Map Tile/ Location '{0}' not found", inputBoxText),
                        "AeroScenery",
                        MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                    }
                }
            }
        }

        private void mainMap_Load(object sender, EventArgs e)
        {

        }
        
        //#MOD_j
        //------------------------------------------------------------------------------------------------------------
        // Adding a moving map to AeroScenery reading UDP data port with data streaming 'on' in Aerofly FS2/4 Settings
        //------------------------------------------------------------------------------------------------------------
        private void ListenForUdpData(CancellationToken cancellationToken)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                // Establishing a connection
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, _port);

                log.InfoFormat(String.Format("Listening for UDP data on port {0}",_port));
                UpdateTxtMovingMapData($"Listening for UDP data on port {_port} ...");

                bool receivingData = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for data and read them out
                        if (udpClient.Available > 0)
                        {
                            byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                            string receivedText = Encoding.UTF8.GetString(receivedBytes);

                            // Splitting and processing the data
                            ProcessReceivedData(receivedText);
                        }
                        if (!receivingData) 
                        {
                            log.InfoFormat(String.Format("Receiving UDP data on port {0}", _port));
                            receivingData = true;
                        }


                        // Wait 10ms before the next check (reduces the CPU load significant)
                        Thread.Sleep(10);
                    }
                    catch (Exception ex)
                    {
                        log.InfoFormat(String.Format("Error: {0}", ex.Message));
                        UpdateTxtMovingMapData("Error");
                    }
                }
            }
        }

        private void ProcessReceivedData(string data)
        {
            // Processing of the data string with differentiation between “XGPS” position data and “”XATT” position data
            if (data.StartsWith("XGPS"))
            {
                string[] gpsData = data.Split(',');
                if (gpsData.Length >= 6)
                {
                    // Save last position with timestamp in ms before overwriting values (used for extrapolation of flight path)
                    this.movingMapTimeStampLast = this.movingMapTimeStamp;
                    this.movingMapLongitudeLast = this.movingMapLongitude;
                    this.movingMapLatitudeLast = this.movingMapLatitude;
                    this.movingMapAltitudeLast = this.movingMapAltitude;
                    this.movingMapVerticalSpeedLast = this.movingMapVerticalSpeed;

                    // Save new data with timestamp in ms
                    this.movingMapTimeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    this.movingMapLongitude = double.Parse(gpsData[1]);
                    this.movingMapLatitude = double.Parse(gpsData[2]);
                    this.movingMapAltitude = double.Parse(gpsData[3]);
                    this.movingMapHeading = double.Parse(gpsData[4]);
                    this.movingMapSpeed = double.Parse(gpsData[5]);

                    // Save the last position (based on average) before overwriting
                    this.movingMapTimeStampAverageLast = this.movingMapTimeStampAverage;
                    this.movingMapLongitudeAverageLast = this.movingMapLongitudeAverage;
                    this.movingMapLatitudeAverageLast = this.movingMapLatitudeAverage;

                    // Determine new position (based on average)
                    this.movingMapTimeStampAverage = (this.movingMapTimeStamp + this.movingMapTimeStampLast) / 2;
                    this.movingMapLongitudeAverage = (this.movingMapLongitude + this.movingMapLongitudeLast) / 2;
                    this.movingMapLatitudeAverage = (this.movingMapLatitude + this.movingMapLatitudeLast) / 2;

                    // Determine vertical speed of airplane (value not broadcasted)
                    if (this.movingMapAltitude != this.movingMapAltitudeLast) 
                    {
                        this.movingMapVerticalSpeed = (this.movingMapAltitude - this.movingMapAltitudeLast) / (this.movingMapTimeStamp - this.movingMapTimeStampLast) * 1000;
                    }

                    // Only for testing purposes: Due to the circumstance that the data is only updated approximately every second, the map position is updated using a separate process with higher refresh rate 
                    //UpdateMovingMapPosition(movingMapLatitude, movingMapLongitude, movingMapHeading);

                    //Trace route mode
                    if ((this.movingMapTraceFlightCheckBox.Checked) && (this.movingMapLatitude != 0) && (this.movingMapLongitude != 0))
                    {
                        // Add new point and update route 
                        Invoke(new Action(() =>
                        {
                            // Add new Point to tracing route using an average again (positions are allready 
                            var newPoint = new PointLatLng((this.movingMapLatitudeAverage + this.movingMapLatitudeAverageLast) / 2, (this.movingMapLongitudeAverage + this.movingMapLongitudeAverageLast) / 2);
                            if (traceRoute != null)
                            {
                                traceRoute.Points.Add(newPoint); // Add new point 
                                traceRouteCount++;

                                // For a better result, only every 5th point is retained
                                if (((traceRouteCount % 5 != 0)) && (traceRouteCount > 2))
                                {
                                    traceRoute.Points.RemoveAt(traceRoute.Points.Count - 2);
                                }

                                mainMap.Refresh(); // Refresh map to show the change
                            }

                        }));
                    }
                }
            }
            else if (data.StartsWith("XATT"))
            {
                string[] attData = data.Split(',');
                if (attData.Length >= 4)
                {
                    //Save new position with timestamp in ms
                    this.movingMapHeading = double.Parse(attData[1]);
                    this.movingMapPitch = double.Parse(attData[2]);
                    this.movingMapRoll = double.Parse(attData[3]);
                }
            }
            else if (data.StartsWith("XTRAFFIC"))
            {
                // Actually no broadcast of traffic data from Aerofly FS2/4  (UDP Protocol Specifications: https://support.foreflight.com/hc/en-us/articles/204115005-Flight-Simulator-GPS-Integration-UDP-Protocol)
                string[] attData = data.Split(',');
                if (attData.Length >= 10)
                {
                }
            }
            Double movingMapAltitudeFt = movingMapAltitude * 3.2808399;
            Double movingMapSpeedKmh = movingMapSpeed * 60 * 60 / 1000 / 1.15078 ;
            Double movingMapSpeedKnots = movingMapSpeedKmh / 1.852;
            String movingMapLongitudeDirection;
            String movingMapLatitudeDirection;
            Double movingMapVerticalSpeedMS = (movingMapVerticalSpeed + movingMapVerticalSpeedLast) / 2;
            Double movingMapVerticalSpeed100FM = movingMapVerticalSpeedMS * 3.2808399 / 100 * 60;

            //Update the flight data
            if (this.movingMapRadioButtonMetric.Checked) 
            {
                UpdateTxtMovingMapFlight($"Heading:\t{this.movingMapHeading.ToString("##0.0")}°\r\nAltitude:\t{Math.Round(this.movingMapAltitude,0).ToString("#,0")}m\r\nSpeed:\t{Math.Round(movingMapSpeedKmh,0).ToString("#,0")}kmh\r\nVS:\t{Math.Round(movingMapVerticalSpeedMS,0).ToString("#,0")}m/s");
            }
            else
            {
                UpdateTxtMovingMapFlight($"Heading:\t{this.movingMapHeading.ToString("##0.0")}°\r\nAltitude:\t{Math.Round(movingMapAltitudeFt,0).ToString("#,0")}ft\r\nSpeed:\t{Math.Round(movingMapSpeedKnots,0).ToString("#,0")}kn\r\nVS:\t{Math.Round(movingMapVerticalSpeed100FM,0).ToString("#,0")}ft/m");
            }

            //Update the position data 
            if (movingMapLongitude >= 0) { movingMapLongitudeDirection = "E"; } else { movingMapLongitudeDirection = "W"; };
            if (movingMapLatitude >= 0) { movingMapLatitudeDirection = "N"; } else { movingMapLatitudeDirection = "S"; };
            UpdateTxtMovingMapData($"Latitude / Longitude:   {this.movingMapLatitude.ToString("##0.0000")} / {this.movingMapLongitude.ToString("##0.0000")} ({movingMapLatitudeDirection}{movingMapLongitudeDirection})\r\nPitsch:\t{this.movingMapPitch.ToString("##0.0")}°\r\nRoll:\t{this.movingMapRoll.ToString("##0.0")}°");

        }

        private async void StartListening()
        {
            _listeningCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _listeningCancellationTokenSource.Token;

            await Task.Run(() => ListenForUdpData(cancellationToken), cancellationToken);
        }

        private void StopListening()
        {
            _listeningCancellationTokenSource?.Cancel();
        }

        private void UpdateTxtMovingMapFlight(string output)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateTxtMovingMapFlight(output)));
            }
            else
            {
                movingMapOutputFlightData.Text = output;
            }
        }

        private void UpdateTxtMovingMapData(string output)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateTxtMovingMapData(output)));
            }
            else
            {
                MovingMapOutputPositionData.Text = output;
            }
        }

        private void UpdateMovingMapPosition(double posLat, double posLon)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateMovingMapPosition(posLat, posLon)));
            }
            else
            {
                mainMap.Position = new PointLatLng(posLat, posLon);
                mainMap.Refresh();
            }
        }

        private void UpdateAirplaneMarkerPosition(double posLat, double posLon, double heading)
        {
            Bitmap RotateBitmap(Bitmap bitmap, float angle)
            {
                Bitmap rotatedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                using (Graphics g = Graphics.FromImage(rotatedBitmap))
                {
                    g.TranslateTransform(bitmap.Width / 2, bitmap.Height / 2); // Set the center of the image as the rotation point
                    g.RotateTransform(angle);
                    g.TranslateTransform(-bitmap.Width / 2, -bitmap.Height / 2); // Reverse transformation
                    g.DrawImage(bitmap, 0, 0);
                }
                return rotatedBitmap;
            }

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateAirplaneMarkerPosition(posLat, posLon, heading)));
            }
            else
            {
                // Set new position and rotated symbol marker
                airplaneMarker.IsVisible = false;
                airplaneMarker.Position = new PointLatLng(posLat, posLon);

                // Create a rotated bitmap based on the heading value
                Bitmap rotatedIcon = RotateBitmap(new Bitmap(Properties.Resources.airplane_icon), (float)heading);
                airplaneMarker.Bitmap = rotatedIcon;
                airplaneMarker.IsVisible = true;

                mainMap.Refresh();
            }
        }

        private void RefreshMovingMapPosition(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {

                // Determine new position
                double posLat = this.movingMapLatitudeAverageLast;
                double posLon = this.movingMapLongitudeAverageLast;

                double elapsedTimeSinceUpdate = Convert.ToDouble((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - this.movingMapTimeStamp);
                double deltaTimeStamp = this.movingMapTimeStampAverage - this.movingMapTimeStampAverageLast;
                double deltaPosLat = this.movingMapLatitudeAverage - this.movingMapLatitudeAverageLast;
                double deltaPosLon = this.movingMapLongitudeAverage - this.movingMapLongitudeAverageLast;


                if (deltaTimeStamp != 0)
                {
                    posLat = posLat + elapsedTimeSinceUpdate / deltaTimeStamp * deltaPosLat;
                    posLon = posLon + elapsedTimeSinceUpdate / deltaTimeStamp * deltaPosLon;
                }
                
                // UpdateMovingMapPosition(posLat, posLon, movingMapHeading);
                if (this.movingMapFixCheckBox.Checked)
                {
                    // Map fixed mode: airplane moves over the map (with AutoScroll off) 
                    mainMap.AutoScroll = false;

                    // Calculate the map in the field of view (viewport) in longitude and latitude
                    var mapRect = mainMap.ViewArea;
                    double latitudeRange = mapRect.Top - mapRect.Bottom;
                    double longitudeRange = mapRect.Right - mapRect.Left;

                    // Border zones (10% away from the edge)
                    double latMargin = latitudeRange * 0.10;
                    double lonMargin = longitudeRange * 0.10;

                    bool nearEdge = posLat > (mapRect.Top - latMargin) ||
                                    posLat < (mapRect.Bottom + latMargin) ||
                                    posLon > (mapRect.Right - lonMargin) ||
                                    posLon < (mapRect.Left + lonMargin);

                    if (nearEdge)
                    {
                        // Move the map and airplane when the airplane is reaching the boarder area of the map
                        double newMapLat = mainMap.Position.Lat;
                        double newMapLon = mainMap.Position.Lng;

                        if (posLat > (mapRect.Top - latMargin)) newMapLat -= latitudeRange / 2;
                        if (posLat < (mapRect.Bottom + latMargin)) newMapLat += latitudeRange / 2;
                        if (posLon > (mapRect.Right - lonMargin)) newMapLon -= longitudeRange / 2;
                        if (posLon < (mapRect.Left + lonMargin)) newMapLon += longitudeRange / 2;

                        UpdateMovingMapPosition(posLat, posLon);
                    }

                    // Update the position of the airplane marker
                    UpdateAirplaneMarkerPosition(posLat, posLon, movingMapHeading);
                    // Additionally updating the position of the map in trace mode (otherwise the refresh of the line is no longer displayed after a certain time) 
                    if (this.movingMapTraceFlightCheckBox.Checked) 
                    {
                        var mapPos = mainMap.Position;
                        UpdateMovingMapPosition(mapPos.Lat, mapPos.Lng);
                    }
                }
                else
                {
                    // Map move mode: map moves with the airplane (with AutoScroll on)
                    mainMap.AutoScroll = true;

                    UpdateMovingMapPosition(posLat, posLon);
                    UpdateAirplaneMarkerPosition(posLat, posLon, movingMapHeading);
                }

                //Refresh-rate every 10ms resp. 0.01s
                Thread.Sleep(10);
            }
        }

        private async void StartRefreshPositionMovingMap()
        {
            _refreshPositionCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _refreshPositionCancellationTokenSource.Token;

            await Task.Run(() => RefreshMovingMapPosition(cancellationToken), cancellationToken);
        }

        private void StopRefreshPositionMovingMap()
        {
            _refreshPositionCancellationTokenSource?.Cancel();
        }


        private void movingMapStartStopButton_Click(object sender, EventArgs e)
        {
            if (this.ActionsRunning == false)
            {
                log.InfoFormat("Moving Map Started");

                //
                if (this.mainTabControl.SelectedIndex > 0)
                {
                    this.mainTabControl.SelectedIndex = 0;
                }

                this.ActionsRunning = true;
                this.movingMapStartStopButton.Text = "Stop";

                this.movingMapLatitude = mainMap.Position.Lat;
                this.movingMapLongitude = mainMap.Position.Lng;
                this.movingMapLatitudeLast = this.movingMapLatitude;
                this.movingMapLongitudeLast = this.movingMapLongitude;
                StartListening();
                StartRefreshPositionMovingMap();

                airplaneMarkers = new GMapOverlay("airplaneMarkers");
                mainMap.Overlays.Add(airplaneMarkers);
                //#TRY_j
                mainMap.AutoScroll = false;
                mainMap.ShowCenter = false;

                airplaneMarker = new GMarkerGoogle(mainMap.Position, new Bitmap(Properties.Resources.airplane_icon));

                int markerWidth = airplaneMarker.Bitmap.Width;
                int markerHeight = airplaneMarker.Bitmap.Height;
                airplaneMarker.Offset = new System.Drawing.Point(-markerWidth / 2, -markerHeight / 2);

                //airplaneMarker.Size = new Size(48, 48); // would adjust icon size (not used)
                airplaneMarkers.Markers.Add(airplaneMarker);

            }
            else 
            {
                this.movingMapStartStopButton.Text = "Start";
                if (this.MovingMapOutputPositionData.Text.Contains("Listening")) 
                {
                    this.MovingMapOutputPositionData.Text = "";
                }
                StopListening();
                StopRefreshPositionMovingMap();

                airplaneMarkers.Markers.Remove(airplaneMarker);
                mainMap.ShowCenter = true;

                this.ActionsRunning = false;

                log.InfoFormat("Moving Map Stopped");

            }
        }

        private void movingMapTraceFlightCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (movingMapTraceFlightCheckBox.Checked)
            {

                // Activate trace mode: Create new overlay and route
                traceOverlay = new GMapOverlay("traceOverlay");
                traceRoute = new GMapRoute(new List<PointLatLng>(), "TraceRoute")
                {
                    Stroke = new Pen(Color.White, 3) // Set the color and thickness of the tracing line
                };

                // Route zum Overlay hinzufügen und Overlay zur Karte hinzufügen
                traceOverlay.Routes.Add(traceRoute);
                mainMap.Overlays.Add(traceOverlay);
            }
            else
            {
                // Deactivate trace mode: Remove overlay and route
                if (traceOverlay != null)
                {
                    mainMap.Overlays.Remove(traceOverlay);
                    traceOverlay.Clear(); // Remove all markers and routes from the overlay
                    traceOverlay = null;
                    traceRoute = null;
                }

                // Refresh map to display the changes
                mainMap.Refresh();
            }
        }

        private void showDownloadedAFS2GridSquares()
        {
            // Shows the “downloaded grid squares” using a loop
            var keys = this.DownloadedAFS2GridSquares.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                var gridSquare = this.DownloadedAFS2GridSquares[keys[i]];
                if (gridSquare.GMapOverlay != null)
                {
                    Invoke(new Action(() =>
                    {
                        gridSquare.GMapOverlay.IsVisibile = true;
                    }));
                }
            }
        }

        private async void showDownloadedAFS2GridSquaresAgain() 
        {
            // Shows the “downloaded grid squares” again running a task           
            await Task.Run(() => showDownloadedAFS2GridSquares());

        }

        private void movingMapHideTilesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (movingMapHideTilesCheckBox.Checked)
            {
                //Hides the “downloaded grid squares” using a loop
                var keys = this.DownloadedAFS2GridSquares.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    var gridSquare = this.DownloadedAFS2GridSquares[keys[i]];

                    if (gridSquare.GMapOverlay != null)
                    {
                        gridSquare.GMapOverlay.IsVisibile = false;
                    }
                }

                movingMapHideTilesCheckBox.Text = "Show working Tiles";
            }
            else
            {
                // Shows the “downloaded grid squares”
                showDownloadedAFS2GridSquaresAgain();

                movingMapHideTilesCheckBox.Text = "Hide working Tiles";
            }
        }


        private void movingMapHelpImage_Click(object sender, EventArgs e)
        {
            string GetLocalIPAddress()
            {
                string localIP = string.Empty;

                foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Check if the connection is active
                    if (netInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ipInfo in netInterface.GetIPProperties().UnicastAddresses)
                        {
                            // Search for an IPv4 address
                            if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                localIP = ipInfo.Address.ToString();
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(localIP))
                        break;
                }

                return localIP;
            }

        string yourIPAdress = GetLocalIPAddress();

            var messageBox = new CustomMessageBox(String.Format("Your detected IP adress is: {0}", yourIPAdress),
            "AeroScenery",
            MessageBoxIcon.Information);

            messageBox.ShowDialog();
        }

        //#TRY_k
        /*
            airportMarkers.Markers.Clear();
            this.GMapControl.Overlays.Remove(airportMarkers);
            this.GMapControl.Overlays.Add(airportMarkers);

            var mapBounds = this.GMapControl.ViewArea;

            if (this.GMapControl.Zoom >= 7 && this.airportLookup != null && mapBounds != null)
            {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var airport in this.airportLookup.Values)
            {
                if (mapBounds.Left < airport.Longitude &&
                    mapBounds.Right > airport.Longitude &&
                    mapBounds.Top > airport.Latitude &&
                    mapBounds.Bottom < airport.Latitude)
                {
                    var point = new PointLatLng(airport.Latitude, airport.Longitude);
                    var marker = new GMarkerGoogle(point, new Bitmap(Properties.Resources.windsock));
                    marker.Tag = airport.ICAO;
                    airportMarkers.Markers.Add(marker);
                }

            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("Looped through airports in " + elapsedMs + "ms");
        }
        */

        //#TRY_k ---------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            string inputBoxText = "";
            if (CustomeInputBox.InputBox("Tile Search", "Tile (e.g. 8500_a500}:", ref inputBoxText) == DialogResult.OK)
            {

                // Erstelle eine neue Instanz des GMap.NET-Steuerelements
                //GMapControl gMap = new GMapControl();
                //gMap.MapProvider = GMapProviders.GoogleMap;

                // Dein Google API-Schlüssel (ersetze mit deinem tatsächlichen API-Schlüssel)
                //string googleApiKey = "YOUR_GOOGLE_API_KEY";

                // Google Maps-Provider mit API-Schlüssel konfigurieren
                //GMapProviders.GoogleMap.ApiKey = googleApiKey;

                // Beispiel: Suche nach "Paris"
                //string address = "Paris, France";

                // Geocoding durchführen
                var geoCoder = GMapProviders.OpenStreetMap;

                // Liste von gefundenen Punkten und Statuscode erhalten
                List<PointLatLng> geocodingPointList;
                var locations = geoCoder.GetPoints(inputBoxText, out geocodingPointList);

                // Prüfen, ob die Suche erfolgreich war
                // Überprüfen, ob die Liste nicht null ist und Ergebnisse enthält
                if (geocodingPointList != null && geocodingPointList.Count > 0)
                {
                    // Den ersten Punkt aus der Liste verwenden (falls mehrere gefunden wurden)
                    var location = geocodingPointList.First();

                    //Console.WriteLine($"Gefundene Koordinaten für {address}: {location.Lat}°, {location.Lng}°");
                    log.InfoFormat($"Gefundene Koordinaten für {inputBoxText}: {location.Lat}°, {location.Lng}°");

                    // Optional: Die Koordinaten auf der Karte anzeigen
                    // Wenn du eine GMap-Control verwendest, kannst du den Punkt wie folgt setzen:
                    this.mainMap.Position = new PointLatLng(location.Lat, location.Lng);
                }
                else
                {
                    //Console.WriteLine("Ort nicht gefunden.");
                    log.InfoFormat("Ort nicht gefunden.");
                }

                /*
                AFS2GridSquare aFS2GridSquareSearch = new AFS2GridSquare();
                AFS2Grid aFS2Grid = new AFS2Grid();
                if (inputBoxText.Length > 9)
                {
                    inputBoxText = inputBoxText.Substring(inputBoxText.Length - 9, 9);
                }
                aFS2GridSquareSearch = aFS2Grid.GetGridSquareName(inputBoxText, this.afsGridSquareSelectionSize);

                if (aFS2GridSquareSearch != null)
                {
                    this.ClearAllSelectedAFSGridSquares();

                    mainMap.Position = new PointLatLng((aFS2GridSquareSearch.NorthLatitude + aFS2GridSquareSearch.SouthLatitude) / 2, (aFS2GridSquareSearch.WestLongitude + aFS2GridSquareSearch.EastLongitude) / 2);
                    mainMap.Zoom = 10;
                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2GridSquareSearch, GridSquareDisplayType.Show);
                }
                else
                {
                    var messageBox = new CustomMessageBox(String.Format("Map Tile '{0}' not found", inputBoxText),
                    "AeroScenery",
                    MessageBoxIcon.Information);

                    messageBox.ShowDialog();

                    //#TRY_j
                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2Grid.GetGridSquareName("8500_a480", this.afsGridSquareSelectionSize), GridSquareDisplayType.Show);
                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2Grid.GetGridSquareName("8580_a480", this.afsGridSquareSelectionSize), GridSquareDisplayType.Show);
                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2Grid.GetGridSquareName("8500_a400", this.afsGridSquareSelectionSize), GridSquareDisplayType.Show);
                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2Grid.GetGridSquareName("8580_a400", this.afsGridSquareSelectionSize), GridSquareDisplayType.Show);

                    //mainMap.Overlays.Remove(this.activeGridSquareOverlay.GMapOverlay);

                    //var squareAndOverlay = this.SelectedAFS2GridSquares[this.activeGridSquareOverlay.Name];
                    //mainMap.Overlays.Remove(squareAndOverlay.GMapOverlay);

                    this.activeGridSquareOverlay = this.gMapControlManager.DrawGridSquare(aFS2Grid.GetGridSquareName("8500_a480", this.afsGridSquareSelectionSize), GridSquareDisplayType.Downloaded);

                }
                */
            }

        }

    }
}
