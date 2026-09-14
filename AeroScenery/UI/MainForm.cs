using AeroScenery.AFS2;
using AeroScenery.Common;
using AeroScenery.Controls;
using AeroScenery.Data;
using AeroScenery.Data.Mappers;
using AeroScenery.Data.Models;
using AeroScenery.FileManagement;
using AeroScenery.FSCloudPort;
using AeroScenery.OrthoPhotoSources;
using AeroScenery.Resources;
using AeroScenery.UI;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using log4net;
using System;
using System.Collections.Generic;
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
using AeroScenery.Extensions.Services;

//#MOD_k
using AeroScenery.Extensions.Instruments;
using Brushes = System.Drawing.Brushes;

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

        private SceneryInstaller sceneryInstaller;
        private FileManager fileManager;

        // Whether we have finished initially updating the UI with settings
        // We can therefore ignore control events until this is true
        private bool uiSetFromSettings;

        private MainFormSideTab currentMainFormSideTab;

        //#MOD_k
        public int afsGridSquareSelectionSize;

        // Whether the user should be shown a dialog about how changing the selection size
        // removes any current selections.
        private bool shownSelectionSizeChangeInfo;

        private List<AFSLevel> afsLevels;

        private bool processCheckBoxListEvents;

        private List<ImageComboItem> orthophotoSourceItems;
        private ImageList orthophotoSourceImages;

        //#MOD_k
        // A run can take hours with nothing on screen changing, so we time the run as a whole and the
        // step being worked on now. Without this there is no way to tell a slow step from a stuck one
        private readonly Stopwatch runStopwatch = new Stopwatch();
        private readonly Stopwatch stepStopwatch = new Stopwatch();
        private System.Windows.Forms.Timer elapsedTimer;
        private string currentStepName;

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
        //#MOD_k
        private double movingMapElevation = -100; //Value -100 if elevation data is not available
        private double movingMapXpdr = 0;
        private string movingMapAircraftName = "";

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

        //#MOD_k
        private bool useUdp = true; // UDP = true, Shared Memory = false
        private GMarkerGoogle airplaneLabelMarker;

        //#MOD_k
        private CancellationTokenSource hudTaskTokenSource;
        private Task hudUpdateTask;

        private HudOverlayControl hudOverlay;


        public MainForm()
        {
            InitializeComponent();

            //NEW_k
            this.elapsedTimer = new System.Windows.Forms.Timer(this.components);
            this.elapsedTimer.Interval = 1000;
            this.elapsedTimer.Tick += ElapsedTimer_Tick;

            this.afs2Grid = new AFS2Grid();
            this.gridSquareMapper = new GridSquareMapper();
            this.gMapControlManager = new GMapControlManager();
            this.fsCloudPortMarkerManager = new FSCloudPortMarkerManager();
            this.fsCloudPortService = new FSCloudPortService();
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

            // Initially 4 thread processes implemented for simultaneous download 
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress1);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress2);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress3);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress4);
            //#MOD
            //Number of thread processes increased from 4 to 8
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress5);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress6);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress7);
            this.downloadThreadProgressControls.Add(this.downloadThreadProgress8);

            this.downloadThreadProgress1.SetDownloadThreadNumber(1);
            this.downloadThreadProgress2.SetDownloadThreadNumber(2);
            this.downloadThreadProgress3.SetDownloadThreadNumber(3);
            this.downloadThreadProgress4.SetDownloadThreadNumber(4);
            //#MOD
            this.downloadThreadProgress5.SetDownloadThreadNumber(5);
            this.downloadThreadProgress6.SetDownloadThreadNumber(6);
            this.downloadThreadProgress7.SetDownloadThreadNumber(7);
            this.downloadThreadProgress8.SetDownloadThreadNumber(8);

            this.gridSquareLabel.Text = "";
            //#MOD
            this.gridSquareBoundaryBox.Text = "";

            this.currentMainFormSideTab = MainFormSideTab.Images;

            this.gMapControlManager.GMapControl = this.mainMap;
            this.fsCloudPortMarkerManager.GMapControl = this.mainMap;

            this.shownSelectionSizeChangeInfo = true;

        }

        public void Initialize()
        {
            System.Windows.Forms.ToolTip toolTip1 = new System.Windows.Forms.ToolTip();
            toolTip1.IsBalloon = true;
            toolTip1.InitialDelay = 500;
            //#MOD
            toolTip1.SetToolTip(this.generateAFS2LevelsHelpImage, "First set the image resolution with the 'Image Detail (Zoom Level)' slider, then press [Choose for me].\nAeroScenery selects the Aerofly levels that GeoConvert should compile.\nLevel 16 (~2.389 m) is a good default for a full Size 9 square. Use a higher zoom level to make smaller tiles at a higher resolution.");

            //#MOD
            System.Windows.Forms.ToolTip toolTip2 = new System.Windows.Forms.ToolTip();
            toolTip2.IsBalloon = true;
            toolTip2.InitialDelay = 500;
            toolTip2.SetToolTip(this.chooseActionsToRunHelpImage, "Use 'Run Default actions' to run the required steps in order.\nIf 'Install Scenery (waiting for GeoConvert)' is selected, all selected tiles are installed automatically into the Aerofly working user folder set in Settings after GeoConvert finishes.\nUse 'Choose actions to run' to run or repeat single steps (for example 'Run GeoConvert' after manually editing the stitched images).\nThe toolbar 'Install Tile' button installs only the currently selected square.");

            //#MOD_j
            System.Windows.Forms.ToolTip toolTip3 = new System.Windows.Forms.ToolTip();
            toolTip3.IsBalloon = true;
            toolTip3.InitialDelay = 500;
            toolTip3.SetToolTip(this.movingMapHelpImage, "Moving map (Aerofly FS2/FS4): turn on 'Broadcast flight info to IP address' under Settings > Miscellaneous settings.\nClick the (?) help icon for the broadcast IP (for example xxx.xxx.xxx.255). Broadcast port is 49002.\nFor FS4 shared-memory tracking, install aeroflybridge.dll from this package into your Aerofly 'external_dll' folder.\nAllow AeroScenery through the firewall / antivirus if the map does not move.");

            toolTip1.SetToolTip(this.imageSourceHelpImage, "First choose the Grid Square Size in the main toolbar, then select the Image Source.\nAerofly sceneries should be based on Size 9, or at least Size 10, at a base resolution.\nFor a first test you can start with a smaller tile.");

            var actionsTip = new System.Windows.Forms.ToolTip();
            actionsTip.IsBalloon = true;
            actionsTip.InitialDelay = 500;
            actionsTip.SetToolTip(this.downloadOsmDataCheckBox, "Writes a PowerShell script to download OpenStreetMap Overpass data for the selected squares.\nEnable this in Settings (Elevation/OSM) first. Run the generated script in the working folder if the automatic run is skipped.");
            actionsTip.SetToolTip(this.downloadElevationDataCheckBox, "Writes a PowerShell script to download 30 m elevation GeoTIFFs from OpenTopography for the selected area.\nSet an OpenTopography API key in Settings first. After download, use mesh_conv.bat for Aerofly .tth mesh files.");
            actionsTip.SetToolTip(this.waterMaskingCheckBox, "Applies extra coastline/water masking from Carto Basemaps (OpenStreetMap).\nTurn this on in Settings (Carto API key + enhanced water masking) before it appears here.");
            actionsTip.SetToolTip(this.allowShiftCorrectionCheckBox, "Optional north-south pixel shift applied when GeoConvert runs, for stitched images that sit slightly off the grid.\nEnable the feature in Settings (GeoConvert tab), then set the offset here if a tile is misaligned.");
            actionsTip.SetToolTip(this.shiftCorrectionLevel, "North-south shift in GeoConvert levels (negative = south, positive = north).\nOnly used when Shift Correction is enabled.");
            actionsTip.SetToolTip(this.installSceneryIntoAFSCheckBox, "After GeoConvert finishes, copy every selected square into the Aerofly user scenery folder from Settings.\nNeeds sequential GeoConvert if several squares run in one job. The toolbar Install Tile button still installs only the current square.");
            actionsTip.SetToolTip(this.fixMissingTilesCheckBox, "Generates a PowerShell script that re-downloads only missing or empty image tiles instead of the whole square.");
            actionsTip.SetToolTip(this.shutdownCheckbox, "When sequential GeoConvert is enabled, shut down Windows after every selected action (including Install Scenery) has finished.\nDisabled for parallel GeoConvert because jobs may still be running.");

            var movingMapTip = new System.Windows.Forms.ToolTip();
            movingMapTip.IsBalloon = true;
            movingMapTip.InitialDelay = 500;
            movingMapTip.SetToolTip(this.panel3DRadioButtonUDP, "HUD and aircraft position from Aerofly UDP broadcast (port 49002).\nWorks with FS2 and FS4 when 'Broadcast flight info' is on.");
            movingMapTip.SetToolTip(this.panel3DRadioButtonDLL, "HUD and aircraft position from aeroflybridge.dll shared memory (Aerofly FS4).\nCopy the DLL from Resources\\external_dll into your Aerofly FS4 external_dll folder.");
            movingMapTip.SetToolTip(this.movingMapFixCheckBox, "Keep the map centred; the aircraft symbol moves. Uncheck to pan the map with the aircraft.");
            movingMapTip.SetToolTip(this.movingMapTraceFlightCheckBox, "Draw the flown route on the map.");
            movingMapTip.SetToolTip(this.movingMapHideTilesCheckBox, "Hide working-grid overlays while the moving map is running so the view stays clearer.");

            // Initialize the AFS Levels CheckBoxLists
            afsLevels = new List<AFSLevel>();
            afsLevels.Add(new AFSLevel("Level 9", 9));
            afsLevels.Add(new AFSLevel("Level 10", 10));
            afsLevels.Add(new AFSLevel("Level 11", 11));
            afsLevels.Add(new AFSLevel("Level 12", 12));
            afsLevels.Add(new AFSLevel("Level 13", 13));
            afsLevels.Add(new AFSLevel("Level 14", 14));
            afsLevels.Add(new AFSLevel("Level 15", 15));

            //#MOD
            afsLevels.Add(new AFSLevel("Level 7", 7));
            afsLevels.Add(new AFSLevel("Level 8", 8));

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
            //#MOD
            this.orthophotoSourceImages.Images.Add(AeroSceneryImages.world_map); //8

            orthophotoSourceItems = new List<ImageComboItem>() {
                new ImageComboItem() { Text = "Bing", Value = OrthophotoSource.Bing, ImageIndex = 0 },
                new ImageComboItem() { Text = "Google", Value = OrthophotoSource.Google, ImageIndex = 0  },
                new ImageComboItem() { Text = "ArcGIS", Value = OrthophotoSource.ArcGIS, ImageIndex = 0  },
                new ImageComboItem() { Text = "Here WeGo", Value = OrthophotoSource.HereWeGo, ImageIndex = 0  },
                //#MOD
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

                //#MOD
                //Currently no use of the additional maps 
                //new ImageComboItem() { Text = "Google Maps (just for masking)", Value = OrthophotoSource.GoogleMaps, ImageIndex = 8  },
                //new ImageComboItem() { Text = "Google Roads (just for masking)", Value = OrthophotoSource.GoogleRoads, ImageIndex = 8  },
                //new ImageComboItem() { Text = "Google Road Map (just for masking)", Value = OrthophotoSource.GoogleRoads, ImageIndex = 8  },
                //new ImageComboItem() { Text = "OSM Maps (just for masking)", Value = OrthophotoSource.OSMMaps, ImageIndex = 8  },

                //#MOD - No more need in the selection due to direct download vie "Action to Run" checkbox
                //new ImageComboItem() { Text = "Carto DB Light (just for masking)", Value = OrthophotoSource.CartoDBLight, ImageIndex = 8  }

            };

            imageSourceComboBox.ImageList = this.orthophotoSourceImages;
            imageSourceComboBox.DataSource = orthophotoSourceItems;

            //#MOD
            var settings = AeroSceneryManager.Instance.Settings;
            // Hide the box resp. option for running Download Elevation if no API-Key is set in the Settings
            if (settings.OpenTopographyApiKey == "")
            {
                downloadElevationDataCheckBox.Visible = false;
                downloadElevationDataCheckBox.Checked = false;
            }
            else
            {
                downloadElevationDataCheckBox.Visible = true;
            }

            //#MOD
            // Hide the box resp. option for enabling Download OSM Data if Option is Set under Settings
            if (settings.DownloadOSMDataEnable == false)
            {
                downloadOsmDataCheckBox.Visible = false;
                downloadOsmDataCheckBox.Checked = false;
            }
            else
            {
                downloadOsmDataCheckBox.Visible = true;
            }

            //#MOD_k
            if (settings.FixMissingTilesEnable == false)
            {
                fixMissingTilesCheckBox.Visible = false;
                fixMissingTilesCheckBox.Checked = false;
            }
            else
            {
                fixMissingTilesCheckBox.Visible = true;
            }

            if (settings.WaterMaskingEnable == false)
            {
                waterMaskingCheckBox.Visible = false;
                waterMaskingCheckBox.Checked = false;
            }
            else
            {
                waterMaskingCheckBox.Visible = true;
            }

            if (settings.AllowShiftCorrectionEnable == false) 
            {
                allowShiftCorrectionCheckBox.Visible = false;
                allowShiftCorrectionCheckBox.Checked = false;
                shiftCorrectionLevel.Visible = false;
            }
            else
            {
                allowShiftCorrectionCheckBox.Visible = true;
                shiftCorrectionLevel.Visible = true;
                shiftCorrectionLevel.Value = settings.AllowShiftCorrectionLevel.Value;
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

            //#MOD_k
            // Show Airports is hidden for Mod k: fscloudport.com is permanently offline.
            // The FSCloudPort scrape, SQLite cache and map-marker code is kept for a later
            // replacement (e.g. OurAirports / local ICAO scan). Do not contact the dead server
            // at startup.
            AeroSceneryManager.Instance.Settings.ShowAirports = false;
            //await this.fsCloudPortService.UpdateAirportsIfRequiredAsync();
            //var airports = await this.fsCloudPortService.GetAirportsAsync();
            //this.fsCloudPortMarkerManager.Airports = airports;
            //
            //if (AeroSceneryManager.Instance.Settings.ShowAirports.Value)
            //{
            //    this.fsCloudPortMarkerManager.UpdateFSCloudPortMarkers();
            //}

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

                //#MOD
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
                //#MOD_k Show Airports is hidden; do not draw fscloudport markers.
                this.showAirportsToolstripButton.Text = "Hide Airports";
            }
            else
            {
                this.showAirportsToolstripButton.Text = "Show Airports";
            }

            //#MOD
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
            //#MOD_k
            this.installSceneryIntoAFSCheckBox.Checked = true;

            //#MOD
            this.fixMissingTilesCheckBox.Checked = false;
            this.downloadOsmDataCheckBox.Checked = false;
            this.downloadElevationDataCheckBox.Checked = false;

            this.downloadImageTileCheckBox.Enabled = false;
            this.stitchImageTilesCheckBox.Enabled = false;
            this.generateAFSFilesCheckBox.Enabled = false;
            this.runGeoConvertCheckBox.Enabled = false;
            //#MOD_k
            this.installSceneryIntoAFSCheckBox.Enabled = false;

            //#MOD
            this.fixMissingTilesCheckBox.Enabled = false;
            this.downloadOsmDataCheckBox.Enabled = false;
            this.downloadElevationDataCheckBox.Enabled = false;

            //#MOD_k
            this.waterMaskingCheckBox.Checked = false;
            this.allowShiftCorrectionCheckBox.Checked = false;
            this.shiftCorrectionLevel.Value = 0;

            this.waterMaskingCheckBox.Enabled = false;
            this.allowShiftCorrectionCheckBox.Enabled = false;
            this.shiftCorrectionLevel.Enabled = false;

        }

        private void SetCustomActions()
        {
            var settings = AeroSceneryManager.Instance.Settings;
            // Actions
            this.downloadImageTileCheckBox.Checked = settings.DownloadImageTiles.Value;
            this.stitchImageTilesCheckBox.Checked = settings.StitchImageTiles.Value;
            this.generateAFSFilesCheckBox.Checked = settings.GenerateAIDAndTMCFiles.Value;
            this.runGeoConvertCheckBox.Checked = settings.RunGeoConvert.Value;
            //@MOD_k
            this.installSceneryIntoAFSCheckBox.Checked = settings.InstallScenery.Value;

            //#MOD
            this.fixMissingTilesCheckBox.Checked = settings.FixMissingTilesProcessing.Value;
            //#MOD_k
            this.waterMaskingCheckBox.Checked = settings.WaterMaskingProcessing.Value;
            this.allowShiftCorrectionCheckBox.Checked = settings.AllowShiftCorrectionProcessing.Value;
            this.downloadOsmDataCheckBox.Checked = settings.DownloadOsmData.Value;
            this.downloadElevationDataCheckBox.Checked = settings.DownloadElevationData.Value;

            this.downloadImageTileCheckBox.Enabled = true;
            this.stitchImageTilesCheckBox.Enabled = true;
            this.generateAFSFilesCheckBox.Enabled = true;
            this.runGeoConvertCheckBox.Enabled = true;
            //#MOD_k
            this.installSceneryIntoAFSCheckBox.Enabled = true;

            //#MOD
            this.fixMissingTilesCheckBox.Enabled = true;
            this.downloadOsmDataCheckBox.Enabled = true;
            this.downloadElevationDataCheckBox.Enabled = true;

            //#MOD_k
            this.waterMaskingCheckBox.Enabled = true;
            this.allowShiftCorrectionCheckBox.Enabled = true;
            this.shiftCorrectionLevel.Enabled = true;   




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
                            //#MOD_k    
                            this.StartElapsedClock();
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
            //#MOD
            this.fixMissingTilesCheckBox.Enabled = false;
            this.downloadOsmDataCheckBox.Enabled = false;
            this.downloadElevationDataCheckBox.Enabled = false;
            this.installSceneryIntoAFSCheckBox.Enabled = false;
            //#MOD_k
            this.waterMaskingCheckBox.Enabled = false;
            this.allowShiftCorrectionCheckBox.Enabled = false;
            this.shiftCorrectionLevel.Enabled = false;

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
            if (AeroSceneryManager.Instance.Settings.ActionSet == ActionSet.Custom)
            {
                this.downloadImageTileCheckBox.Enabled = true;
                this.stitchImageTilesCheckBox.Enabled = true;
                this.generateAFSFilesCheckBox.Enabled = true;
                this.runGeoConvertCheckBox.Enabled = true;
                //#MOD
                this.fixMissingTilesCheckBox.Enabled = true;
                this.downloadOsmDataCheckBox.Enabled = true;
                this.downloadElevationDataCheckBox.Enabled = true;
                this.installSceneryIntoAFSCheckBox.Enabled = true;
                //#MOD_k
                this.waterMaskingCheckBox.Enabled = true;
                this.allowShiftCorrectionCheckBox.Enabled = true;
                this.shiftCorrectionLevel.Enabled = true;

            }
        }

        private void ResetProgress()
        {
            this.downloadThreadProgress1.Reset();
            this.downloadThreadProgress2.Reset();
            this.downloadThreadProgress3.Reset();
            this.downloadThreadProgress4.Reset();
            //#MOD
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
                //#MOD
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

            //#MOD
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
                //#MOD
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
                //#MOD
                this.copyToClipboardToolStripButton.Enabled = true;
            }
            else
            {
                this.openImageFolderToolstripButton.Enabled = false;
                this.deleteImagesToolStripButton.Enabled = false;
                this.openMapToolStripDropDownButton.Enabled = false;
                this.installSceneryToolStripButton.Enabled = false;
                //#MOD
                this.copyToClipboardToolStripButton.Enabled = false;
            }

        }

        public void LoadDownloadedGridSquares()
        {
            var gridSquares = this.dataRepository.GetAllGridSquares();

            foreach (GridSquare gridSquare in gridSquares)
            {
                var afs2GridSqure = this.gridSquareMapper.ToAFS2GridSquare(gridSquare);
                //#MOD_k
                var polygonOverlay = this.gMapControlManager.DrawGridSquare(afs2GridSqure, GridSquareDisplayType.Downloaded);
                if (gridSquare.Fixed == 0)
                {
                    polygonOverlay = this.gMapControlManager.DrawGridSquare(afs2GridSqure, GridSquareDisplayType.Data);
                }

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
        //#MOD_k
        public void AddDataGridSquare(AFS2GridSquare afs2GridSqure)
        {
            var polygonOverlay = this.gMapControlManager.DrawGridSquare(afs2GridSqure, GridSquareDisplayType.Data);

            var gridSquareViewModel = new GridSquareViewModel();
            gridSquareViewModel.GMapOverlay = polygonOverlay;
            gridSquareViewModel.AFS2GridSquare = afs2GridSqure;

            this.DownloadedAFS2GridSquares[afs2GridSqure.Name] = gridSquareViewModel;

        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();

            //#MOD_k
            settingsForm.FormClosed += (s, args) =>
            {
                this.UpdateFromSettings();
            };

            settingsForm.Show();
            if (settingsForm.StartPosition == FormStartPosition.CenterParent)
            {
                var x = Location.X + (Width - settingsForm.Width) / 2;
                var y = Location.Y + (Height - settingsForm.Height) / 2;
                settingsForm.Location = new System.Drawing.Point(Math.Max(x, 0), Math.Max(y, 0));
            }

        }

        //#MOD_k
        private void UpdateFromSettings()
        {
            var settings = AeroSceneryManager.Instance.Settings;

            this.fixMissingTilesCheckBox.Visible =
                settings.FixMissingTilesEnable.Value;
            if (settings.FixMissingTilesEnable.Value == false)
            {
                this.fixMissingTilesCheckBox.Checked = false;
            }   
            fixMissingTiles_CheckedChanged(this.fixMissingTilesCheckBox, EventArgs.Empty);

            this.waterMaskingCheckBox.Visible =
                settings.WaterMaskingEnable.Value;
            if (settings.WaterMaskingEnable.Value == false)
            {
                this.waterMaskingCheckBox.Checked = false;
            }
            waterMaskingCheckBox_CheckedChanged(this.waterMaskingCheckBox, EventArgs.Empty);

            this.allowShiftCorrectionCheckBox.Visible =
                settings.AllowShiftCorrectionEnable.Value;

            if (settings.AllowShiftCorrectionEnable.Value == false)
            {
                this.allowShiftCorrectionCheckBox.Checked = false;
            }
            allowShiftCorrectionCheckBox_CheckedChanged(this.fixMissingTilesCheckBox, EventArgs.Empty);

            this.shiftCorrectionLevel.Visible =
                settings.AllowShiftCorrectionEnable.Value;
            if (settings.AllowShiftCorrectionEnable.Value == false)
            {
                this.shiftCorrectionLevel.Value = 0;
            }
            shiftCorrectionLevel_ValueChanged(this.shiftCorrectionLevel, EventArgs.Empty);

            //#MOD_k
            this.downloadOsmDataCheckBox.Visible =
                settings.DownloadOSMDataEnable.Value;
            if (settings.DownloadOSMDataEnable.Value == false)
            {
                this.downloadOsmDataCheckBox.Checked = false;
            }
            downloadOsmDataCheckBox_CheckedChanged(this.downloadOsmDataCheckBox, EventArgs.Empty);

            //#MOD_k
            if (settings.OpenTopographyApiKey == "")
            {
                this.downloadElevationDataCheckBox.Visible = false;
                this.downloadElevationDataCheckBox.Checked = false;
            }
            else
            {
                this.downloadElevationDataCheckBox.Visible = true;
            }
            downloadElevationDataCheckBox_CheckedChanged(this.downloadElevationDataCheckBox, EventArgs.Empty);

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
                        //#TRY
                        //this.DeselectAFSGridSquare(evt.X, evt.Y);
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

                //#MOD
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

                //#MOD
                // Additionally copy the center coordinates to the clipoard as "<lon> <lat>" for use in TSC-Files of Aerofly
                var centerCoodinateStr = selectedGridSquare.GetCenter().Lng.ToString("#.########", CultureInfo.InvariantCulture) + " " + selectedGridSquare.GetCenter().Lat.ToString("#.########", CultureInfo.InvariantCulture);
                Clipboard.SetData(DataFormats.Text, (Object)centerCoodinateStr);
            }
        }
        //#MOD
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

                //#MOD
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

                //#MOD_k
                // Additionally copy the name of the selected gridsquare to the clipboard, before opening the folder (even if it doesn't exist)
                Clipboard.SetData(DataFormats.Text, (Object)this.SelectedAFS2GridSquare.Name);

                if (Directory.Exists(gridSquareDirectory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = gridSquareDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });

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

        private void fixMissingTiles_CheckedChanged(object sender, EventArgs e)
        {
            if (fixMissingTilesCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.FixMissingTilesProcessing = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.FixMissingTilesProcessing = false;
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

        //#MOD_k
        private void waterMaskingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (waterMaskingCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.WaterMaskingProcessing = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.WaterMaskingProcessing = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void allowShiftCorrectionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (allowShiftCorrectionCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.AllowShiftCorrectionProcessing = true;
                //shiftCorrectionLevel.Visible = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.AllowShiftCorrectionProcessing = false;
                //shiftCorrectionLevel.Visible = false;
            }

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void shiftCorrectionLevel_ValueChanged(object sender, EventArgs e)
        {
            AeroSceneryManager.Instance.Settings.AllowShiftCorrectionLevel = (int)shiftCorrectionLevel.Value;

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
            
            UpdateShutdownComputerWhenDoneState();

            AeroSceneryManager.Instance.SaveSettings();
        }

        private void installSceneryIntoAFSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (installSceneryIntoAFSCheckBox.Checked)
            {
                AeroSceneryManager.Instance.Settings.InstallScenery = true;
            }
            else
            {
                AeroSceneryManager.Instance.Settings.InstallScenery = false;
            }

            UpdateShutdownComputerWhenDoneState();

            AeroSceneryManager.Instance.SaveSettings();
        }

        //#MOD_k
        private void UpdateShutdownComputerWhenDoneState()
        {
            bool allowed =
                this.runGeoConvertCheckBox.Checked &&
                this.installSceneryIntoAFSCheckBox.Checked;

            this.shutdownCheckbox.Enabled = allowed;

            if (!allowed)
            {
                this.shutdownCheckbox.Checked = false;
            }
        }

        //#MOD
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

        private void downloadElevationDataCheckBox_CheckedChanged(object sender, EventArgs e)
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

        private void helpToolStripButton_Click(object sender, EventArgs e)
        {
            var url = "https://github.com/chrispriv/aeroscenery-mod";
            System.Diagnostics.Process.Start(url);
        }

        private void getSDKToolStripButton_Click(object sender, EventArgs e)
        {
            //#MOD
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
            this.StartTimingStep(childTask);
        }


        public void UpdateTaskLabels(string parentTask, string childTask)
        {
            this.parentTaskLabel.Text = parentTask;
            this.childTaskLabel.Text = childTask;
        }


        //#MOD_k
        /// <summary>
        /// Starts the clocks for a new run. Both the run and its current step are timed, and the
        /// labels are refreshed once a second so that a long step still looks alive.
        /// </summary>
        private void StartElapsedClock()
        {
            this.currentStepName = null;
            this.runStopwatch.Restart();
            this.stepStopwatch.Restart();
            this.elapsedTimer.Start();

            this.UpdateElapsedLabels();
        }

        /// <summary>
        /// Stops the clocks and leaves the totals on screen. The log keeps them too, since that is
        /// the only record left once the window is closed.
        /// </summary>
        private void StopElapsedClock(bool finished)
        {
            if (!this.runStopwatch.IsRunning)
            {
                return;
            }

            this.elapsedTimer.Stop();
            this.runStopwatch.Stop();
            this.stepStopwatch.Stop();

            if (this.currentStepName != null)
            {
                log.Info(String.Format("{0} took {1}", this.currentStepName, FormatElapsed(this.stepStopwatch.Elapsed)));
                this.currentStepName = null;
            }

            log.Info(String.Format("Run {0} after {1}", finished ? "finished" : "stopped", FormatElapsed(this.runStopwatch.Elapsed)));

            this.UpdateElapsedLabels();

            this.statusStripElapsedLabel.Text = String.Format("{0} {1}",
                finished ? "Finished in" : "Stopped after",
                FormatElapsed(this.runStopwatch.Elapsed));
        }

        /// <summary>
        /// Times the step that is starting, and logs how long the one it replaces took. Every step
        /// change goes through the child task label, which is why the timing hangs off it.
        /// </summary>
        private void StartTimingStep(string stepName)
        {
            // Label updates outside a run - "Finished", "Stopped" - are not steps and must not
            // restart a clock that has already been stopped
            if (!this.runStopwatch.IsRunning || stepName == this.currentStepName)
            {
                return;
            }

            if (this.currentStepName != null)
            {
                log.Info(String.Format("{0} took {1}", this.currentStepName, FormatElapsed(this.stepStopwatch.Elapsed)));
            }

            this.currentStepName = stepName;
            this.stepStopwatch.Restart();

            this.UpdateElapsedLabels();
        }

        private void ElapsedTimer_Tick(object sender, EventArgs e)
        {
            this.UpdateElapsedLabels();
        }

        private void UpdateElapsedLabels()
        {
            this.runElapsedLabel.Text = String.Format("Elapsed {0}", FormatElapsed(this.runStopwatch.Elapsed));

            this.stepElapsedLabel.Text = this.currentStepName != null
                ? String.Format("this step {0}", FormatElapsed(this.stepStopwatch.Elapsed))
                : "";

            // Once the run is over the status strip says how it ended, so only touch it while running
            if (this.runStopwatch.IsRunning)
            {
                this.statusStripElapsedLabel.Text = String.Format("Running {0}", FormatElapsed(this.runStopwatch.Elapsed));
            }
        }

        /// <summary>
        /// Hours, minutes and seconds. TimeSpan's own "hh" wraps at a day and these runs can pass it.
        /// </summary>
        private static string FormatElapsed(TimeSpan elapsed)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                (int)elapsed.TotalHours,
                elapsed.Minutes,
                elapsed.Seconds);
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
                        //#MOD
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

            //#MOD_k
            var finished = this.actionsRunning;
            this.StopElapsedClock(finished);
            
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
                    //#MOD
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

                //#MOD
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

        private void sideTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.sideTabControl.SelectedIndex)
            {
                case 0:
                    this.currentMainFormSideTab = MainFormSideTab.Images;
                    //#MOD_y
                    this.startStopButton.Enabled = false;
                    break;
                case 1:
                    this.currentMainFormSideTab = MainFormSideTab.Elevation;
                    this.ClearAllSelectedAFSGridSquares();
                    //#MOD_y
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
                //#MOD
                case 7:
                    afsLevels.Add(7);

                    break;
                //#MOD
                case 8:
                    afsLevels.Add(8);

                    break;

                case 9:

                    afsLevels.Add(9);
                    //#MOD_k
                    //afsLevels.Add(10);
                    //afsLevels.Add(11);
                    //afsLevels.Add(12);
                    if (zoomLevel > 12)
                    {
                        afsLevels.Add(10);
                    }
                    if (zoomLevel > 13)
                    {
                        afsLevels.Add(11);
                    }
                    if (zoomLevel > 14)
                    {
                        afsLevels.Add(12);
                    }

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
                    //#MOD_k
                    //afsLevels.Add(11);
                    //afsLevels.Add(12);
                    if (zoomLevel > 13)
                    {
                        afsLevels.Add(11);
                    }
                    if (zoomLevel > 14)
                    {
                        afsLevels.Add(12);
                    }
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
                    //#MOD_k
                    //afsLevels.Add(12);
                    //afsLevels.Add(13);
                    if (zoomLevel > 14)
                    {
                        afsLevels.Add(12);
                    }

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

                case 12:

                    afsLevels.Add(12);
                    //#MOD_k
                    //afsLevels.Add(13);
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
                case 13:

                    afsLevels.Add(13);
                    //#MOD_k
                    //afsLevels.Add(14);
                    if (zoomLevel > 16)
                    {
                        afsLevels.Add(14);
                    }

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
            switch (e.ClickedItem.Tag)
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
                    //#MOD
                    this.mainMap.MapProvider = GMapProviders.OpenCycleMap;
                    break;
                //#MOD
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

        //#MOD_k
        private async void InstallSceneryToolStripButton_ClickAsync(object sender, EventArgs e)
        {
            if (this.SelectedAFS2GridSquare != null)
            {
                await this.InstallSceneryForGridSquareAsync(this.SelectedAFS2GridSquare, true);
            }

        }
        /// <summary>
        /// Installs the ttc files of a grid square into the Aerofly scenery folder.
        /// Shared by the toolbar button and the Install Scenery action, which differ only in whether
        /// the user is asked to confirm first.
        /// </summary>
        public async Task InstallSceneryForGridSquareAsync(AFS2GridSquare afs2GridSquare, bool confirmWithUser)
        {
            var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + afs2GridSquare.Name;

            if (Directory.Exists(gridSquareDirectory))
            {
                var result = this.sceneryInstaller.ConfirmSceneryInstallation(afs2GridSquare, confirmWithUser);

                if (result == DialogResult.Yes)
                {
                    var ttcFiles = new List<string>();

                    var duplicateResult = this.sceneryInstaller.CheckForDuplicateTTCFiles(afs2GridSquare, out ttcFiles);

                    if (duplicateResult == null || duplicateResult == DialogResult.OK)
                    {
                        var installTask = this.sceneryInstaller.InstallSceneryAsync(afs2GridSquare, ttcFiles);

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
                var messageBox = new CustomMessageBox(String.Format("There is no image folder yet for grid square {0}", afs2GridSquare.Name),
                    "AeroScenery",
                    MessageBoxIcon.Information);

                messageBox.ShowDialog();
            }

        }

        private void openMapToolStripDropDownButton_Click(object sender, EventArgs e)
        {

        }
        private void copyToClipboardToolStripButton_Click(object sender, EventArgs e)
        {
            //#MOD
            Clipboard.SetData(DataFormats.Text, (Object)gridSquareBoundaryBox.Text);

        }

        private void versionToolStripLabel_Click(object sender, EventArgs e)
        {

        }

        //#MOD
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
            //#MOD
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

                log.InfoFormat(String.Format("Listening for UDP data on port {0}", _port));
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

                    UpdateTraceRoute();

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
            
            UpdateFlightInfoDisplay();

        }


        private void ListenForSharedMemoryData(CancellationToken cancellationToken)
        {
            AeroflyConnector connector = null;

            log.Info("Waiting for AeroflyBridge shared memory...");
            UpdateTxtMovingMapData("Waiting for AeroflyBridge.dll shared memory...");

            while (!cancellationToken.IsCancellationRequested)
            {
                connector = AeroflyConnector.TryCreate();
                if (connector != null)
                    break;

                Thread.Sleep(500); // 0.5s warten, wenn noch nicht verfügbar
            }

            log.Info("AeroflyBridge shared memory connected.");
            UpdateTxtMovingMapData("AeroflyBridge.dll shared memory connected.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    uint isValid = connector.ReadUInt32(8);
                    if (isValid == 1)
                    {
                        double lat = connector.ReadDouble("Aircraft.Latitude") * 57.2958;
                        double lon = connector.ReadDouble("Aircraft.Longitude") * 57.2958;
                        double alt = connector.ReadDouble("Aircraft.Altitude");
                        double trueHeading = connector.ReadDouble("Aircraft.TrueHeading");
                        double magneticHeading = connector.ReadDouble("Aircraft.MagneticHeading"); // to be used (same as UDP)
                        double pitch = connector.ReadDouble("Aircraft.Pitch") * 57.2958;
                        double roll = connector.ReadDouble("Aircraft.Bank") * 57.2958;
                        double groundSpeed = connector.ReadDouble("Aircraft.GroundSpeed");  // to be used(same as UDP)
                        double airSpeed = connector.ReadDouble("Aircraft.IndicatedAirspeed");
                        double verticalSpeed = connector.ReadDouble("Aircraft.VerticalSpeed");

                        double xpdr = connector.ReadDouble("Communication.TransponderCode");
                        string aircraftName = connector.ReadString("Aircraft.Name");

                        this.movingMapTimeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        this.movingMapLatitude = lat;
                        this.movingMapLongitude = lon;
                        this.movingMapAltitude = alt;
                        this.movingMapHeading = connector.ConvertAeroflyHeading(magneticHeading);
                        this.movingMapPitch = pitch;
                        this.movingMapRoll = roll;
                        this.movingMapSpeed = groundSpeed;
                        this.movingMapVerticalSpeed = verticalSpeed;

                        this.movingMapXpdr = xpdr;
                        this.movingMapAircraftName = aircraftName;  

                        UpdateTraceRoute();
                        UpdateFlightInfoDisplay();
                    }

                    Thread.Sleep(50); // 20 Hz
                }
            }
            catch (Exception ex)
            {
                log.InfoFormat("Fehler bei Shared-Memory-Zugriff: " + ex.Message);
                UpdateTxtMovingMapData("Fehler: AeroflyBridge.dll nicht mehr erreichbar.");
            }
            finally
            {
                connector?.Dispose();
            }
        }
        private void UpdateTraceRoute()
        {
            //Trace route mode
            if ((this.movingMapTraceFlightCheckBox.Checked) && (this.movingMapLatitude != 0) && (this.movingMapLongitude != 0))
            {
                // Add new point and update route 
                Invoke(new Action(() =>
                {
                    // Add new Point to tracing route using an average again (positions are allready 
                    // Use the average position for a better result                    
                    //newPoint = new PointLatLng((this.movingMapLatitudeAverage + this.movingMapLatitudeAverageLast) / 2, (this.movingMapLongitudeAverage + this.movingMapLongitudeAverageLast) / 2);
                    var newPoint = new PointLatLng(this.movingMapLatitude, this.movingMapLongitude);
                    if (useUdp)
                    {
                        // Use the average position for a better result
                        newPoint = new PointLatLng((this.movingMapLatitudeAverage + this.movingMapLatitudeAverageLast) / 2, (this.movingMapLongitudeAverage + this.movingMapLongitudeAverageLast) / 2);
                    }

                    if (traceRoute != null)
                    {
                        traceRoute.Points.Add(newPoint); // Neuen Punkt hinzufügen
                        traceRouteCount++;

                        int step = useUdp ? 5 : 20; // Bei UDP enger filtern, Shared Memory = seltener

                        // Optional: nur jeden x-ten Punkt behalten
                        if ((traceRouteCount % step != 0) && (traceRouteCount > 2))
                        {
                            traceRoute.Points.RemoveAt(traceRoute.Points.Count - 2);
                        }

                        // Begrenzung der maximalen Punkteanzahl (älteste Punkte löschen)
                        int maxPoints = 100000;
                        while (traceRoute.Points.Count > maxPoints)
                        {
                            traceRoute.Points.RemoveAt(0); // Entferne ältesten Punkt
                        }

                        mainMap.Refresh(); // Karte aktualisieren
                    }

                }));
            }
        }

        private void UpdateFlightInfoDisplay()
        {
            // Berechnung aller Einheiten + Anzeige-Logik wie in ProcessReceivedData()

            Double movingMapAltitudeFt = movingMapAltitude * 3.2808399;
            Double movingMapSpeedKmh = movingMapSpeed * 3.6 / 1.15078;
            Double movingMapSpeedKnots = movingMapSpeedKmh / 1.852;
            String movingMapLongitudeDirection;
            String movingMapLatitudeDirection;
            Double movingMapVerticalSpeedMS = movingMapVerticalSpeed;
            if (useUdp) 
            {
                movingMapVerticalSpeedMS = (movingMapVerticalSpeed + movingMapVerticalSpeedLast) / 2;
            } 

            Double movingMapVerticalSpeed100FM = movingMapVerticalSpeedMS * 3.2808399 / 100 * 60;

            //Update the flight data
            string altitudeAGLText = "";
            if (this.movingMapRadioButtonMetric.Checked)
            {
                //#MOD_k
                if (movingMapElevation > -100)
                {
                    altitudeAGLText = $" / {Math.Round(movingMapAltitude - movingMapElevation):#,0} m (AGL)";
                }

                UpdateTxtMovingMapFlight($"Heading:\t{this.movingMapHeading.ToString("##0.0")} °\r\nAltitude:\t{Math.Round(this.movingMapAltitude, 0).ToString("#,0")} m{altitudeAGLText}\r\nSpeed:\t{Math.Round(movingMapSpeedKmh, 0).ToString("#,0")} kmh\r\nVS:\t{Math.Round(movingMapVerticalSpeedMS, 0).ToString("#,0")} m/s");
            }
            else
            {
                //#MOD_k
                if (movingMapElevation > -100)
                {
                    altitudeAGLText = $" / {Math.Round(movingMapAltitudeFt - movingMapElevation * 3.2808399).ToString("#,0")} ft (AGL)";
                }
                UpdateTxtMovingMapFlight($"Heading:\t{this.movingMapHeading.ToString("##0.0")} °\r\nAltitude:\t{Math.Round(movingMapAltitudeFt, 0).ToString("#,0")} ft{altitudeAGLText}\r\nSpeed:\t{Math.Round(movingMapSpeedKnots, 0).ToString("#,0")} kt\r\nVS:\t{Math.Round(movingMapVerticalSpeed100FM, 0).ToString("#,0")} ft/min");
            }

            //Update the position data 
            if (movingMapLongitude >= 0) { movingMapLongitudeDirection = "E"; } else { movingMapLongitudeDirection = "W"; }
            ;
            if (movingMapLatitude >= 0) { movingMapLatitudeDirection = "N"; } else { movingMapLatitudeDirection = "S"; }
            ;
            UpdateTxtMovingMapData($"Latitude / Longitude:   {this.movingMapLatitude.ToString("##0.0000")} / {this.movingMapLongitude.ToString("##0.0000")} ({movingMapLatitudeDirection}{movingMapLongitudeDirection})\r\nPitsch:\t{this.movingMapPitch.ToString("##0.0")} °\r\nRoll:\t{this.movingMapRoll.ToString("##0.0")} °");
        }


        //#MOD_k
        private async void StartListening()
        {
            _listeningCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _listeningCancellationTokenSource.Token;

            if (useUdp) 
            {
                await Task.Run(() => ListenForUdpData(cancellationToken), cancellationToken);
            } 
            else 
            {
                await Task.Run(() => ListenForSharedMemoryData(cancellationToken), cancellationToken);
            }

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

                //#MOD_k
                // Label auf gleiche Geo-Position setzen
                if (airplaneLabelMarker != null)
                {
                    airplaneLabelMarker.Position = new PointLatLng(posLat, posLon);

                    // Tooltip-Position leicht nach oben verschieben
                    // (funktioniert unabhängig vom Zoom)
                    airplaneLabelMarker.ToolTipText = $"{movingMapAircraftName.ToUpper()}: FL{movingMapAltitude * 3.2808399 / 1000:000}\nXPDR: {movingMapXpdr}";
                    //airplaneLabelMarker.ToolTip.Offset = new System.Drawing.Point(airplaneMarker.Bitmap.Height / 2 - 5, -airplaneMarker.Bitmap.Height / 2 + 5);
                }

                mainMap.Refresh();
            }
        }

        private void RefreshMovingMapPosition(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                double posLat = this.movingMapLatitude;
                double posLon = this.movingMapLongitude;

                if (useUdp) 
                {
                    // Determine new position smoothed using average (needed for UDP only)
                    posLat = this.movingMapLatitudeAverageLast;
                    posLon = this.movingMapLongitudeAverageLast;

                    double elapsedTimeSinceUpdate = Convert.ToDouble((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - this.movingMapTimeStamp);
                    double deltaTimeStamp = this.movingMapTimeStampAverage - this.movingMapTimeStampAverageLast;
                    double deltaPosLat = this.movingMapLatitudeAverage - this.movingMapLatitudeAverageLast;
                    double deltaPosLon = this.movingMapLongitudeAverage - this.movingMapLongitudeAverageLast;

                    if (deltaTimeStamp != 0)
                    {
                        posLat = posLat + elapsedTimeSinceUpdate / deltaTimeStamp * deltaPosLat;
                        posLon = posLon + elapsedTimeSinceUpdate / deltaTimeStamp * deltaPosLon;
                    }
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


        private async void movingMapStartStopButton_Click(object sender, EventArgs e)
        {
            if (this.ActionsRunning == false) //Start
            {
                log.InfoFormat("Moving Map Started");

                this.panel3DRadioButtonUDP.Enabled = false;
                this.panel3DRadioButtonDLL.Enabled = false;

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


                //#MOD_k
                // Add a label marker for the airplane with tooltip information (only for Shared Memory mode)
                if (!useUdp) 
                {
                    airplaneLabelMarker = new GMarkerGoogle(mainMap.Position, new Bitmap(1, 1));
                    airplaneLabelMarker.ToolTipMode = MarkerTooltipMode.Always;
                    airplaneLabelMarker.ToolTipText = $" ";
                    airplaneLabelMarker.ToolTip.Fill = Brushes.White;
                    airplaneLabelMarker.ToolTip.Foreground = Brushes.Black;
                    airplaneLabelMarker.ToolTip.Stroke = Pens.Transparent;
                    airplaneLabelMarker.ToolTip.Font = new Font("Segoe UI", 8, FontStyle.Regular);
                    airplaneLabelMarker.ToolTip.Offset = new System.Drawing.Point(airplaneMarker.Bitmap.Height / 2 - 5, -airplaneMarker.Bitmap.Height / 2 + 5);

                    // Overlay hinzufügen
                    airplaneMarkers.Markers.Add(airplaneLabelMarker);

                }

                // Start HUD task 
                StartHudTask();


            }
            else // Stop
            {
                this.movingMapStartStopButton.Text = "Start";

                this.panel3DRadioButtonUDP.Enabled = true;
                this.panel3DRadioButtonDLL.Enabled = true;

                if (this.MovingMapOutputPositionData.Text.Contains("Listening"))
                {
                    this.MovingMapOutputPositionData.Text = "";
                }
                StopListening();
                StopRefreshPositionMovingMap();

                airplaneMarkers.Markers.Remove(airplaneMarker);
                if (airplaneLabelMarker != null)
                {
                    airplaneMarkers.Markers.Remove(airplaneLabelMarker);
                    airplaneLabelMarker = null;
                }
                mainMap.ShowCenter = true;

                // ✅ HUD-Task stoppen
                StopHudTask();

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
                    Stroke = new System.Drawing.Pen(System.Drawing.Color.White, 3) // Set the color and thickness of the tracing line
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

            var messageBox = new CustomMessageBox(String.Format("Your detected IP address is: {0}", yourIPAdress),
            "AeroScenery",
            MessageBoxIcon.Information);

            messageBox.ShowDialog();
        }


        //#MOD_k ---------------------------------------------------
        //
        private void StartHudTask()
        {
            hudTaskTokenSource = new CancellationTokenSource();
            var token = hudTaskTokenSource.Token;
           
            if (hudOverlay == null || !panel3DPreview.Controls.Contains(hudOverlay))
            {
                hudOverlay = new HudOverlayControl
                {
                    Parent = panel3DPreview,
                    Location = new System.Drawing.Point(0, 0),
                    Size = panel3DPreview.Size,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                panel3DPreview.Controls.Add(hudOverlay);
            }

            hudOverlay.Visible = true;
            hudOverlay.BringToFront();
            

            hudUpdateTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    // UI-Aufruf im Main Thread
                    Invoke((MethodInvoker)(() =>
                    {
                        //#TRY
                        //if (panel3DRadioButtonUDP.Checked && hudOverlay != null)
                        if (hudOverlay != null)
                        {
                            hudOverlay.Pitch = movingMapPitch;
                            hudOverlay.Roll = movingMapRoll;

                            hudOverlay.SpeedKt = movingMapSpeed * 60 * 60 / 1000 / 1.15078 / 1.852;
                            hudOverlay.AltitudeFt = movingMapAltitude * 3.2808399;
                            hudOverlay.HeadingDeg = movingMapHeading;
                            if (useUdp) 
                            {
                                hudOverlay.VerticalSpeedFtM = (movingMapVerticalSpeed + movingMapVerticalSpeedLast) / 2 * 3.2808399 / 100 * 60;
                            }
                            else 
                            {
                                hudOverlay.VerticalSpeedFtM = movingMapVerticalSpeed * 3.2808399 / 100 * 60;
                            }
                            hudOverlay.ElevationFt = movingMapElevation * 3.2808399;

                            hudOverlay.Invalidate();
                        }
                    }));

                    await Task.Delay(50); // ca. 20 FPS
                }
            }, token);
        }

        private void StopHudTask()
        {
            if (hudTaskTokenSource != null)
            {
                try
                {
                    hudTaskTokenSource.Cancel();
                }
                catch (Exception ex)
                {
                    log.Warn("Fehler beim Abbrechen der HUD-Task", ex);
                }
                finally
                {
                    hudTaskTokenSource.Dispose();
                    hudTaskTokenSource = null;
                    hudUpdateTask = null; // Task ist durch Token-Abbruch zum Stop gezwungen
                }
            }
        }

        private void panel3DRadioButtonHUD_CheckedChanged(object sender, EventArgs e)
        {
            if (panel3DRadioButtonUDP.Checked)
            { 
                useUdp = true;
            }
            else 
            { 
                useUdp = false;
            }

        }

        //#MOD_k
        public bool ShutdownComputerWhenDone
        {
            get { return this.shutdownCheckbox.Checked; }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

    }
}
