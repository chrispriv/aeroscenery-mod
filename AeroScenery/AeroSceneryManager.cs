using AeroScenery.AFS2;
using AeroScenery.Common;
using AeroScenery.Controls;
using AeroScenery.Data;
using AeroScenery.Data.Mappers;
using AeroScenery.Data.Models;
using AeroScenery.Download;
using AeroScenery.FlightPathVisualizer.Models;
using AeroScenery.FlightPathVisualizer.Services;
using AeroScenery.FSCloudPort;
using AeroScenery.ImageProcessing;
using AeroScenery.OrthophotoSources;
using AeroScenery.OrthophotoSources.Japan;
using AeroScenery.OrthophotoSources.NewZealand;
using AeroScenery.OrthophotoSources.Norway;
using AeroScenery.OrthophotoSources.Spain;
using AeroScenery.OrthophotoSources.Sweden;
using AeroScenery.OrthophotoSources.Switzerland;
using AeroScenery.OrthophotoSources.UnitedStates;
using AeroScenery.OrthoPhotoSources;
using AeroScenery.UI;
using HelixToolkit.Wpf;
using log4net;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
//#MOD_k
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;

namespace AeroScenery
{
    public class AeroSceneryManager
    {
        private MainForm mainForm;

        private BingOrthophotoSource bingOrthophotoSource;
        private GoogleOrthophotoSource googleOrthophotoSource;
        private USGSOrthophotoSource usgsOrthophotoSource;
        private GSIOrthophotoSource gsiOrthophotoSource;
        private LinzOrthophotoSource linzOrthophotoSource;
        private NorgeBilderOrthophotoSource norgeBilderOrthophotoSource;
        private IDEIBOrthophotoSource ideibOrthophotoSource;
        private IGNOrthophotoSource ignOrthophotoSource;
        private LantmaterietOrthophotoSource lantmaterietOrthophotoSource;
        private GeoportalOrthophotoSource geoportalOrthophotoSource;
        private ArcGISOrthophotoSource arcGISOrthophotoSource;
        private HittaOrthophotoSource hittaOrthophotoSource;
        private HereWeGoOrthophotoSource hereWeGoOrthophotoSource;
        private GuleSiderOrthophotoSource guleSiderOrthophotoSource;
        //#MOD
        private MapboxOrthophotoSource mapboxOrthophotoSource;
        private GoogleOrthomapSource googleOrthomapSource;
        private GoogleOrthoroadmapSource googleOrthoroadmapSource;
        private OSMMapsOrthomapSource osmMapsOrthomapSource;
        private CartoDBLightOrthomapSource cartoDBLightOrthomapSource;

        private DownloadManager downloadManager;

        private GeoConvertManager geoConvertManager;
        //#MOD_k
        private TerrainData _terrainData;

        //private DownloadFailedForm downloadFailedForm;

        private TileStitcher tileStitcher;

        private static AeroSceneryManager aeroSceneryManager;

        private ImageTileService imageTileService;

        private Common.Settings settings;

        private SettingsService settingsService;

        private IDataRepository dataRepository;

        private GridSquareMapper gridSquareMapper;

        private AFSFileGenerator afsFileGenerator;

        private List<ImageTile> imageTiles;
        private readonly ILog log = LogManager.GetLogger("AeroScenery");
        private string version;
        private int incrementalVersion;

        public AeroSceneryManager()
        {
            downloadManager = new DownloadManager();
            geoConvertManager = new GeoConvertManager();
            imageTileService = new ImageTileService();
            tileStitcher = new TileStitcher();
            settingsService = new SettingsService();
            gridSquareMapper = new GridSquareMapper();
            afsFileGenerator = new AFSFileGenerator();
            dataRepository = new SqlLiteDataRepository();

            imageTiles = null;
            version = "1.1.3 MOD k DEVL by @chrispriv"; //#DEVL_k
            incrementalVersion = 13;
        }

        public Settings Settings
        {
            get
            {
                return this.settings;
            }
        }

        public string Version
        {
            get
            {
                return this.version;
            }
        }

        public int IncrementalVersion
        {
            get
            {
                return this.incrementalVersion;
            }
        }

        public static AeroSceneryManager Instance
        {
            get
            {
                if (AeroSceneryManager.aeroSceneryManager == null)
                {
                    aeroSceneryManager = new AeroSceneryManager();
                }

                return aeroSceneryManager;
            }
        }

        public void Initialize()
        {
            // Create settings if required and read them
            this.settings = settingsService.GetSettings();
            settingsService.LogSettings(this.settings);
            settingsService.CheckConfiguredDirectories(this.settings);

            this.dataRepository.Settings = settings;
            this.dataRepository.UpgradeDatabase();

            var gridSquareNameFixer = new GridSquareNameFixer(settings, this.dataRepository, this.settingsService);
            gridSquareNameFixer.FixGridSquareNames();

            bingOrthophotoSource = new BingOrthophotoSource(settings.OrthophotoSourceSettings.BN_OrthophotoSourceUrlTemplate);
            googleOrthophotoSource = new GoogleOrthophotoSource(settings.OrthophotoSourceSettings.GM_OrthophotoSourceUrlTemplate);
            usgsOrthophotoSource = new USGSOrthophotoSource();
            gsiOrthophotoSource = new GSIOrthophotoSource();
            linzOrthophotoSource = new LinzOrthophotoSource();
            norgeBilderOrthophotoSource = new NorgeBilderOrthophotoSource();
            ideibOrthophotoSource = new IDEIBOrthophotoSource();
            ignOrthophotoSource = new IGNOrthophotoSource();
            lantmaterietOrthophotoSource = new LantmaterietOrthophotoSource();
            geoportalOrthophotoSource = new GeoportalOrthophotoSource();
            arcGISOrthophotoSource = new ArcGISOrthophotoSource();
            hittaOrthophotoSource = new HittaOrthophotoSource();
            hereWeGoOrthophotoSource = new HereWeGoOrthophotoSource();
            guleSiderOrthophotoSource = new GuleSiderOrthophotoSource();
            //#MOD
            mapboxOrthophotoSource = new MapboxOrthophotoSource();
            googleOrthomapSource = new GoogleOrthomapSource(); 
            googleOrthoroadmapSource = new GoogleOrthoroadmapSource(); 
            osmMapsOrthomapSource = new OSMMapsOrthomapSource();
            cartoDBLightOrthomapSource = new CartoDBLightOrthomapSource();

            this.mainForm = new MainForm();
            this.mainForm.StartStopClicked += async (sender, eventArgs) =>
            {
                //#MOD
                // Bug fix: Adding a delay for Start & Stops reduces the occurrence of an unhandled error when stopping the download (bug appears since the number of download threads has been increased from 4 to 8)
                await Task.Delay(600);

                if (this.mainForm.ActionsRunning)
                {
                    //#MOD
                    // Bug fix: Sometimes it's still occured, than even mainForm.ActionRunning value is false download will starts instead of stops!?! Handle this critical exception to to avoid an abort of the app (is there a nother approach?)  
                    try
                    {
                        await StartSceneryGenerationProcessAsync(sender, eventArgs);
                    }
                    catch (Exception)
                    {
                        StopSceneryGenerationProcess(sender, eventArgs);
                    }

                }
                else
                {
                    StopSceneryGenerationProcess(sender, eventArgs);
                }
            };

            this.mainForm.ResetGridSquare += (sender, name) =>
            {
                this.ResetGridSquare(name);
            };

            this.mainForm.Initialize();
            Application.Run(this.mainForm);

        }


        private string GetTileDownloadDirectory(string afsGridSquareDirectory)
        {
            var tileDownloadDirectory = afsGridSquareDirectory;

            switch (this.settings.OrthophotoSource)
            {
                case OrthophotoSource.Bing:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.Bing);
                    break;
                case OrthophotoSource.Google:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.Google);
                    break;
                case OrthophotoSource.ArcGIS:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.ArcGIS);
                    break;
                case OrthophotoSource.US_USGS:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.US_USGS);
                    break;
                case OrthophotoSource.NZ_Linz:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.NZ_Linz);
                    break;
                case OrthophotoSource.ES_IDEIB:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.ES_IDEIB);
                    break;
                case OrthophotoSource.CH_Geoportal:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.CH_Geoportal);
                    break;
                case OrthophotoSource.NO_NorgeBilder:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.NO_NorgeBilder);
                    break;
                case OrthophotoSource.SE_Lantmateriet:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.SE_Lantmateriet);
                    break;
                case OrthophotoSource.ES_IGN:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.ES_IGN);
                    break;
                case OrthophotoSource.JP_GSI:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.JP_GSI);
                    break;
                case OrthophotoSource.SE_Hitta:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.SE_Hitta);
                    break;
                case OrthophotoSource.HereWeGo:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.HereWeGo);
                    break;
                case OrthophotoSource.NO_GuleSider:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.NO_GuleSider);
                    break;
                //#MOD
                case OrthophotoSource.Mapbox:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.Mapbox);
                    break;
                case OrthophotoSource.GoogleMaps:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.GoogleMaps);
                    break;
                case OrthophotoSource.GoogleRoads:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.GoogleRoads);
                    break;
                case OrthophotoSource.OSMMaps:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.OSMMaps);
                    break;
                case OrthophotoSource.CartoDBLight:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.CartoDBLight);
                    break;
            }

            return tileDownloadDirectory;
        }

        public void StopSceneryGenerationProcess(object sender, EventArgs e)
        {
            downloadManager.StopDownloads();

            if (this.imageTiles != null)
            {
                this.imageTiles.Clear();
                this.imageTiles = null;
                System.GC.Collect();
            }

        }

        private void ActionsComplete()
        {
            this.mainForm.ActionsComplete();

            if (this.imageTiles != null)
            {
                this.imageTiles.Clear();
                this.imageTiles = null;
                System.GC.Collect();
            }

        }




        public async Task StartSceneryGenerationProcessAsync(object sender, EventArgs e)
        {
            try
            {
                // Set settings on orthophoto sources
                this.linzOrthophotoSource.ApiKey = settings.LinzApiKey;
                //#MOD
                this.mapboxOrthophotoSource.ApiKey = settings.MapboxApiKey;
                this.hereWeGoOrthophotoSource.ApiKey = settings.HereWeGoApiKey;

                double selectedTilesEastLongitude = -180;
                double selectedTilesWestLongitude = 180;
                double selectedTilesNorthLatitude = -90;
                double selectedTilesSouthLatitude = 90;

                int i = 0;
                foreach (AFS2GridSquare afs2GridSquare in this.mainForm.SelectedAFS2GridSquares.Values.Select(x => x.AFS2GridSquare))
                {
                    var currentGrideSquareMessage = String.Format("Working on AFS Grid Square {0} of {1}", i + 1, this.mainForm.SelectedAFS2GridSquares.Count());
                    this.mainForm.UpdateParentTaskLabel(currentGrideSquareMessage);
                    //#TRY_k    
                    await Task.Delay(50);
                    log.Info(currentGrideSquareMessage);

                    //#MOD
                    // Determine maximum coverage of all selected tiles/area (actually not needed anymore)
                    if (selectedTilesEastLongitude < afs2GridSquare.EastLongitude) { selectedTilesEastLongitude = afs2GridSquare.EastLongitude; }
                    if (selectedTilesWestLongitude > afs2GridSquare.WestLongitude) { selectedTilesWestLongitude = afs2GridSquare.WestLongitude; }
                    if (selectedTilesNorthLatitude < afs2GridSquare.NorthLatitude) { selectedTilesNorthLatitude = afs2GridSquare.NorthLatitude; }
                    if (selectedTilesSouthLatitude > afs2GridSquare.SouthLatitude) { selectedTilesSouthLatitude = afs2GridSquare.SouthLatitude; }

                    //#MOD
                    // If Action Running Check at the level of tiles check and create the working folders and subfolders (not done if only "Downlaod Elevation Data (30m) for selected area" selected) 
                    var afsGridSquareDirectory = this.settings.WorkingDirectory + afs2GridSquare.Name;

                    var tileDownloadDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"\";
                    var stitchedTilesDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-stitched\";

                    //#MOD_k (var declaration for optional masking shifted)
                    var afsGridSquareDirectoryMask = this.settings.WorkingDirectory + afs2GridSquare.Name;

                    var tileDownloadDirectoryMask = afsGridSquareDirectoryMask + @"\c-mask\" + this.settings.ZoomLevel + @"\";
                    var stitchedTilesDirectoryMask = afsGridSquareDirectoryMask + @"\c-mask\" + +this.settings.ZoomLevel + @"-stitched\";


                    //#DEVL_k
                    // 1.
                    // Download of the masking images for Trees Detection if Mask Image or Water Masking on stiched images is selected (both optional) by overriding the image tiles of the orthophoto source
                    // (do this new as first step, so that the images are available for optional water masking after stiching)
                    if (((this.Settings.RunTreesDetectionMask.Value && this.Settings.RunTreesDetection.Value) || this.Settings.WaterMaskingProcessing.Value) && this.mainForm.ActionsRunning)
                    {
                        // Do we have a directory for the afs grid square and this orthophoto source
                        if (!Directory.Exists(tileDownloadDirectoryMask))
                        {
                            Directory.CreateDirectory(tileDownloadDirectoryMask);
                        }
                        // Orthoimages are only downloaded once (directory must be deleted to force a new download)
                        if (!Directory.Exists(stitchedTilesDirectoryMask))
                        {
                            Directory.CreateDirectory(stitchedTilesDirectoryMask);

                            this.mainForm.UpdateChildTaskLabel($"Calculating Masking Image Tiles To Download");
                            log.Info("Calculating Masking Image Tiles To Download");

                            GenericOrthophotoSource orthophotoSourceInstance = null;

                            imageTiles = cartoDBLightOrthomapSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                            orthophotoSourceInstance = cartoDBLightOrthomapSource;

                            this.mainForm.UpdateChildTaskLabel($"Downloading Masking Image Tiles");
                            log.Info("Downloading Masking Image Tiles");

                            // Capture the progress of each thread
                            var downloadThreadProgress = new Progress<DownloadThreadProgress>();
                            downloadThreadProgress.ProgressChanged += DownloadThreadProgress_ProgressChanged;

                            // Send the masking image tiles to the download manager
                            await downloadManager.DownloadImageTiles(OrthophotoSource.CartoDBLight, imageTiles, downloadThreadProgress, tileDownloadDirectoryMask, orthophotoSourceInstance, Convert.ToInt16(settings.SimultaneousDownloads));

                            // Check & Fix missing Masking Image Tiles using a PS1 PowerShell-Script
                            log.Info("Check & Fix missing Masking Image Tiles using a PS1 PowerShell-Script");
                            var proc = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = @"powershell.exe",
                                    Arguments = $@"-NoProfile -ExecutionPolicy ByPass -File ""{tileDownloadDirectoryMask}\_imagetiles_download_catalog.ps1""",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = false,
                                    RedirectStandardError = false,
                                    CreateNoWindow = false,
                                    WorkingDirectory = $@"{tileDownloadDirectoryMask}\"
                                }
                            };

                            proc.Start();
                            // Wait for termination of tile fix, before go on with stiching image, if Stich Image is selected as next step (else going on without waiting)
                            proc.WaitForExit();
                            await Task.Delay(100);

                            // Stitch Masking Image Tiles
                            this.mainForm.UpdateChildTaskLabel($"Stitching Masking Image Tiles");
                            log.Info("Stitching Masking Image Tiles");

                            // Capture the progress of the tile stitcher
                            var tileStitcherProgress = new Progress<TileStitcherProgress>();
                            tileStitcherProgress.ProgressChanged += TileStitcherProgress_ProgressChanged;

                            //#DEVL_k
                            //await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectoryMask, stitchedTilesDirectoryMask, true, OrthophotoSource.CartoDBLight, tileStitcherProgress);
                            await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectoryMask, stitchedTilesDirectoryMask, "", true, OrthophotoSource.CartoDBLight, tileStitcherProgress);

                        }

                    }

                    // 2.Download of the image tiles
                    // Download of the image tiles for the orthophoto source selected in the settings
                    if ((this.Settings.DownloadImageTiles.Value || (this.Settings.DownloadImageTiles.Value || (this.Settings.FixMissingTiles.Value) || (this.Settings.StitchImageTiles.Value) || (this.Settings.GenerateAIDAndTMCFiles.Value) || (this.Settings.RunGeoConvert.Value) || (this.Settings.RunTreesDetection.Value)) && this.mainForm.ActionsRunning)) 
                    {
                        // Do we have a directory for this afs grid square in our working directory?
                        if (!Directory.Exists(this.settings.WorkingDirectory + afs2GridSquare.Name))
                        {
                            Directory.CreateDirectory(this.settings.WorkingDirectory + afs2GridSquare.Name);
                        }

                        if (!Directory.Exists(tileDownloadDirectory))
                        {
                            Directory.CreateDirectory(tileDownloadDirectory);
                        }

                        if (!Directory.Exists(stitchedTilesDirectory))
                        {
                            Directory.CreateDirectory(stitchedTilesDirectory);
                        }
                    }

                    // Download Image Tiles
                    if (this.Settings.DownloadImageTiles.Value && this.mainForm.ActionsRunning)
                    {
                        this.mainForm.UpdateChildTaskLabel($"Calculating Image Tiles To Download");
                        log.Info("Calculating Image Tiles To Download");

                        GenericOrthophotoSource orthophotoSourceInstance = null;

                        var imageTilesTask = Task.Run(() => {

                            // Get a list of all the image tiles we need to download
                            switch (settings.OrthophotoSource)
                            {
                                case OrthophotoSource.Bing:
                                    imageTiles = bingOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = bingOrthophotoSource;
                                    break;
                                case OrthophotoSource.Google:
                                    imageTiles = googleOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = googleOrthophotoSource;
                                    break;
                                case OrthophotoSource.ArcGIS:
                                    imageTiles = arcGISOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = arcGISOrthophotoSource;
                                    break;
                                case OrthophotoSource.US_USGS:
                                    imageTiles = usgsOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = usgsOrthophotoSource;
                                    break;
                                case OrthophotoSource.NZ_Linz:
                                    imageTiles = linzOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = linzOrthophotoSource;
                                    break;
                                case OrthophotoSource.ES_IDEIB:
                                    imageTiles = ideibOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = ideibOrthophotoSource;
                                    break;
                                case OrthophotoSource.CH_Geoportal:
                                    imageTiles = geoportalOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = geoportalOrthophotoSource;
                                    break;
                                case OrthophotoSource.NO_NorgeBilder:
                                    imageTiles = norgeBilderOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = norgeBilderOrthophotoSource;
                                    break;
                                case OrthophotoSource.SE_Lantmateriet:
                                    imageTiles = lantmaterietOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = lantmaterietOrthophotoSource;
                                    break;
                                case OrthophotoSource.ES_IGN:
                                    imageTiles = ignOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = ignOrthophotoSource;
                                    break;
                                case OrthophotoSource.JP_GSI:
                                    imageTiles = gsiOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = gsiOrthophotoSource;
                                    break;
                                case OrthophotoSource.SE_Hitta:
                                    imageTiles = hittaOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = hittaOrthophotoSource;
                                    break;
                                case OrthophotoSource.HereWeGo:
                                    imageTiles = hereWeGoOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = hereWeGoOrthophotoSource;
                                    break;
                                case OrthophotoSource.NO_GuleSider:
                                    imageTiles = guleSiderOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = guleSiderOrthophotoSource;
                                    break;
                                //#MOD
                                case OrthophotoSource.Mapbox:
                                    imageTiles = mapboxOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = mapboxOrthophotoSource;
                                    break;
                                case OrthophotoSource.GoogleMaps:
                                    imageTiles = googleOrthomapSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = googleOrthomapSource;
                                    break;
                                case OrthophotoSource.GoogleRoads:
                                    imageTiles = googleOrthoroadmapSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = googleOrthoroadmapSource;
                                    break;
                                case OrthophotoSource.OSMMaps:
                                    imageTiles = osmMapsOrthomapSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = osmMapsOrthomapSource;
                                    break;
                                case OrthophotoSource.CartoDBLight:
                                    imageTiles = cartoDBLightOrthomapSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = cartoDBLightOrthomapSource;
                                    break;
                            }
                        });

                        await imageTilesTask;

                        this.mainForm.UpdateChildTaskLabel($"Downloading Image Tiles");
                        log.Info("Downloading Image Tiles");

                        // Capture the progress of each thread
                        var downloadThreadProgress = new Progress<DownloadThreadProgress>();
                        downloadThreadProgress.ProgressChanged += DownloadThreadProgress_ProgressChanged;

                        // Send the image tiles to the download manager
                        //#MOD (max. number of simultaneous downloads can be set in settings)
                        await downloadManager.DownloadImageTiles(settings.OrthophotoSource.Value, imageTiles, downloadThreadProgress, tileDownloadDirectory, orthophotoSourceInstance, Convert.ToInt16(settings.SimultaneousDownloads));

                        // Only finalise if we weren't cancelled
                        if (this.mainForm.ActionsRunning)
                        {
                            var existingGridSquare = this.dataRepository.FindGridSquare(afs2GridSquare.Name);

                            if (existingGridSquare == null)
                            {
                                this.dataRepository.CreateGridSquare(this.gridSquareMapper.ToModel(afs2GridSquare));
                                this.mainForm.AddDownloadedGridSquare(afs2GridSquare);
                            }
                            //DEVL_k
                            else if (existingGridSquare.Fixed == 0)
                            {
                                existingGridSquare.Fixed = 1;
                                dataRepository.UpdateGridSquare(existingGridSquare);
                            }
                        }

                    }

                    //#MOD
                    // Check & Fix missing Image Tiles using a PS1 PowerShell-Script (PowerSell-Script has been written before, as a part of DownloadMagaer-Process)
                    if (this.Settings.FixMissingTiles.Value && this.mainForm.ActionsRunning) 
                    {
                        log.Info("Check & Fix missing Image Tiles using a PS1 PowerShell-Script");
                        var proc = new Process
                        {
                        StartInfo = new ProcessStartInfo
                            {
                                FileName = @"powershell.exe",
                                Arguments = $@"-NoProfile -ExecutionPolicy ByPass -File ""{tileDownloadDirectory}\_imagetiles_download_catalog.ps1""",
                                UseShellExecute = false,
                                RedirectStandardOutput = false,
                                RedirectStandardError = false,
                                CreateNoWindow = false,
                                WorkingDirectory = $@"{tileDownloadDirectory}\"
                            }
                        };

                        proc.Start();
                        //Wait for termination of tile fix, before go on with stiching image, if Stich Image is selected as next step (else going on without waiting)
                        if (this.Settings.StitchImageTiles.Value == true) 
                        {
                            proc.WaitForExit();
                        }
                        await Task.Delay(100);
                    }

                    // 3. Stitching Tiles to Images incl. optinal colorisation and water masking 
                    if (this.Settings.StitchImageTiles.Value && this.mainForm.ActionsRunning)
                    {
                        this.mainForm.UpdateChildTaskLabel($"Stitching Image Tiles");
                        log.Info("Stitching Image Tiles");

                        // Capture the progress of the tile stitcher
                        var tileStitcherProgress = new Progress<TileStitcherProgress>();
                        tileStitcherProgress.ProgressChanged += TileStitcherProgress_ProgressChanged;

                        //#DEVL_k (settings.OrthophotoSource.Value and tileDownloadDirectoryMask added)
                        await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectory, stitchedTilesDirectory, stitchedTilesDirectoryMask, true, settings.OrthophotoSource.Value, tileStitcherProgress);
                    }

                    // 4. Generate AFS Metadata Files (incl. optional working structure for convertion for mobile devices)
                    // Generate AID and TMC Files
                    if (this.Settings.GenerateAIDAndTMCFiles.Value && this.mainForm.ActionsRunning)
                    {
                        this.mainForm.UpdateChildTaskLabel($"Generating AFS Metadata Files");
                        log.Info("Generating AFS Metadata Files");

                        // Capture the progress of the tile stitcher
                        var afsFileGeneratorProgress = new Progress<AFSFileGeneratorProgress>();
                        afsFileGeneratorProgress.ProgressChanged += AFSFileGeneratorProgress_ProgressChanged;


                        // Generate AID files for the image tiles
                        
                        await afsFileGenerator.GenerateAFSFilesAsync(afs2GridSquare, stitchedTilesDirectory, GetTileDownloadDirectory(afsGridSquareDirectory), afsFileGeneratorProgress);

                    }

                    //#MOD
                    // Create additional Working Folder incl. tmc & bat file for conversion of images to ttc for mobile (to be run manually after GeoConvert process)
                    if (this.Settings.GenerateAIDAndTMCFiles.Value && this.settings.CreateAddForMobile.Value && this.mainForm.ActionsRunning)
                    {
                        var afsAddForMobileWorkingDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + @"\" + this.settings.ZoomLevel + "-geoconvert-ttc-mobile";
     
                        if (!Directory.Exists(afsAddForMobileWorkingDirectory))
                        {
                            Directory.CreateDirectory(afsAddForMobileWorkingDirectory);

                            //#DEVL_k
                            string inputFolderImages = $"./{this.settings.ZoomLevel}-geoconvert-raw/";
                            string outputFolderTTC = $"./{this.settings.ZoomLevel}-geoconvert-ttc-mobile/";

                            var textTMCImages = new TMCImagesFile(inputFolderImages, outputFolderTTC);
                            string outputFilePath = $@"{GetTileDownloadDirectory(afsGridSquareDirectory)}\content_converter_config_mobile.tmc";
                            File.WriteAllText(outputFilePath, textTMCImages.GeneratedContent);

                        }
                    }

                    //#MOD_k
                    // 5. Download OSM Data from opemstreetmap.org
                    if (this.settings.DownloadOsmData.Value && this.mainForm.ActionsRunning)
                    {
                        // Create subdirectory for osm data, if it not allready existing
                        if (!Directory.Exists(afsGridSquareDirectory + "/osm"))
                        {
                            Directory.CreateDirectory(afsGridSquareDirectory + "/osm");
                        }

                        this.mainForm.UpdateChildTaskLabel($"Downloading OSM Data");
                        log.Info($"Downloading OSM Data for the tile {afs2GridSquare.Name}");

                        var boarderCorr = 0.0005; // Correction of the boarder box to avoid flickering houses (actually fix value)
                        var eastLngCorr = afs2GridSquare.EastLongitude - boarderCorr;
                        var westLngCorr = afs2GridSquare.WestLongitude + boarderCorr;
                        var northLatCorr = afs2GridSquare.NorthLatitude - boarderCorr;
                        var southLatCorr = afs2GridSquare.SouthLatitude + boarderCorr;

                        string eastLngText = eastLngCorr.ToString("#.####", CultureInfo.InvariantCulture);
                        string westLngText = westLngCorr.ToString("#.####", CultureInfo.InvariantCulture);
                        string northLatText = northLatCorr.ToString("#.####", CultureInfo.InvariantCulture);
                        string southLatText = southLatCorr.ToString("#.####", CultureInfo.InvariantCulture);

                        string boundingBox = $@"{westLngText},{southLatText},{eastLngText},{northLatText}";

                        string overpassApiUrl = "https://overpass-api.de/api/map"; 
                        string outputDirectory = $@"{afsGridSquareDirectory}\osm\";  
                        string tileName = $@"{afs2GridSquare.Name}"; 

                        var downloader = new OSMDataDownloader(overpassApiUrl, outputDirectory, tileName, boundingBox);

                        try
                        {
                            // Start task to perform asynchronous download
                            await Task.Run(() => downloader.DownloadOSMData());
                            this.mainForm.UpdateChildTaskLabel($"Downloading OSM Data completed");
                            log.Info("Downloading OSM Data completed");

                            if ((this.settings.DownloadElevationData == false) && (this.mainForm.ActionsRunning)) 
                            {
                                var downloadActionProgressPercentage = this.mainForm.CurrentActionProgressPercentage;
                                int downloadOSMDataProgressPercentage = (i + 1) * 100 / this.mainForm.SelectedAFS2GridSquares.Count();

                                if (downloadOSMDataProgressPercentage > downloadActionProgressPercentage)
                                {
                                    this.mainForm.CurrentActionProgressPercentage = downloadOSMDataProgressPercentage;
                                }

                                var existingGridSquare = this.dataRepository.FindGridSquare(afs2GridSquare.Name);

                                if (existingGridSquare == null)
                                {
                                    this.dataRepository.CreateGridSquare(this.gridSquareMapper.ToModel(afs2GridSquare));
                                    this.mainForm.AddDownloadedGridSquare(afs2GridSquare);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Error handling
                            this.mainForm.UpdateChildTaskLabel($"Error while downloading OSM Data (refer to Log)");
                            log.Info($"Error while downloading OSM data for the tile {afs2GridSquare.Name} : {ex.Message}");
                        }

                    }

                    //#MOD_k
                    // 5. Download Elevation Data
                    // ...
                    if ((settings.OpenTopographyApiKey == "") && (this.settings.DownloadElevationData == true) && (this.mainForm.ActionsRunning))
                    {
                        var messageBox = new CustomMessageBox("API Key need to be add in Settings to access OpenTopography for the download of elevation data of the selected area).\r\r Abort download ...",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                        messageBox.ShowDialog();
                    }
                    //else if ((this.settings.DownloadElevationData == true) && (this.mainForm.ActionsRunning) && ((this.mainForm.SelectedAFS2GridSquares.Count() != 2) || (i != 1)))
                    else if ((this.settings.DownloadElevationData == true) && (this.mainForm.ActionsRunning))
                        {
                        GdalBase.ConfigureAll();
                        Gdal.AllRegister();

                        this.mainForm.UpdateChildTaskLabel($"Downloading Elevation Data");
                        log.Info($"Downloading Elevation Data for the tile {afs2GridSquare.Name}");

                        //
                        //string meshResolution = settings.OpenTopographyDataSet.Split(' ')[0];
                        string meshResolution = Regex.Match(settings.OpenTopographyDataSet, @"\b\d{2,3}m\b").Value.TrimEnd('m');
                        string gridSquareLevel = afs2GridSquare.Name.Split('_')[1];

                        string openTopographyAPIUrl = "https://portal.opentopography.org/API/globaldem?demtype=";
                        if (settings.OpenTopographyDataSet.Substring(0, 4) == "USGS")
                        {
                            openTopographyAPIUrl = "https://portal.opentopography.org/API/usgsdem?datasetName=";
                        }
                        //string openTopographyDemType = settings.OpenTopographyDataSet.Substring(0, settings.OpenTopographyDataSet.IndexOf(" "));
                        string openTopographyDemType = settings.OpenTopographyDataSet.Split(' ')[0];

                        //
                        string elevationDirectory = afsGridSquareDirectory + "/elevation-" + openTopographyDemType;
                        // Create subdirectory for elevation data, if it not allready existing
                        if (!Directory.Exists(elevationDirectory))
                        {
                            Directory.CreateDirectory(elevationDirectory);
                        }
                        if (!Directory.Exists(elevationDirectory + "/geoconvert-tth"))
                        {
                            Directory.CreateDirectory(elevationDirectory + "/geoconvert-tth");
                        }
                        var outputDirectory = elevationDirectory + "/input_aerial_images";
                        if (!Directory.Exists(outputDirectory))
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }

                        //...
                        if (String.IsNullOrEmpty(this.settings.AFS2SDKDirectory))
                        {
                            var messageBox = new CustomMessageBox("Please set the location of the Aerofly SDK in Settings to be able to use Geoconvert for converting Meshes",
                                "AeroScenery",
                                MessageBoxIcon.Warning);

                            messageBox.ShowDialog();
                        }
                        else
                        {
                            // Creates and Copy subfolders 'shader_dx11\' & 'texures\' from the GeoConvert, else GeoCDonvert wil not work outside of the installation-path 
                            if (!Directory.Exists(elevationDirectory + "/shader_dx11"))
                            {
                                Directory.CreateDirectory(elevationDirectory + "/shader_dx11");
                                foreach (string newPath in Directory.GetFiles(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/shader_dx11", "*.*", SearchOption.AllDirectories))
                                {
                                    File.Copy(newPath, newPath.Replace(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/shader_dx11", elevationDirectory + "/shader_dx11"), true);
                                }
                            }
                            if (!Directory.Exists(outputDirectory + "/texture"))
                            {
                                Directory.CreateDirectory(elevationDirectory + "/texture");
                                foreach (string newPath in Directory.GetFiles(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/texture", "*.*", SearchOption.AllDirectories))
                                {
                                    File.Copy(newPath, newPath.Replace(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/texture", elevationDirectory + "/texture"), true);
                                }
                            }
                        }

                        //
                        double boarderCorr = 0.0010 * (15 - Convert.ToInt16(gridSquareLevel)); // Enlarging of the boarder box for a small overlapping of the GeoTiff-images to avoid bugs at the boarder depending of the ZoomLevel
                        // double eastLng, westLng, northLat, southLat;
                        //if ((this.mainForm.SelectedAFS2GridSquares.Count() != 2) || (i == 0)) //..
                        //{
                            double eastLng = afs2GridSquare.EastLongitude;
                            double westLng = afs2GridSquare.WestLongitude;
                            double northLat = afs2GridSquare.NorthLatitude;
                            double southLat = afs2GridSquare.SouthLatitude;
                        //}
                        //else //..
                        //{
                        //    eastLng = selectedTilesEastLongitude;
                        //    westLng = selectedTilesWestLongitude;
                        //    northLat = selectedTilesNorthLatitude;
                        //    southLat = selectedTilesSouthLatitude;
                        //}

                        string eastLngText = eastLng.ToString("#.#######", CultureInfo.InvariantCulture);
                        string westLngText = westLng.ToString("#.#######", CultureInfo.InvariantCulture);
                        string northLatText = northLat.ToString("#.#######", CultureInfo.InvariantCulture);
                        string southLatText = southLat.ToString("#.#######", CultureInfo.InvariantCulture);

                        string afsBoundingBox = $@"south={southLatText}&north={northLatText}&west={westLngText}&east={eastLngText}";

                        eastLng = eastLng + boarderCorr;
                        westLng = westLng - boarderCorr;
                        northLat = northLat + boarderCorr;
                        southLat = southLat - boarderCorr;

                        eastLngText = eastLng.ToString("#.######", CultureInfo.InvariantCulture);
                        westLngText = westLng.ToString("#.######", CultureInfo.InvariantCulture);
                        northLatText = northLat.ToString("#.######", CultureInfo.InvariantCulture);
                        southLatText = southLat.ToString("#.######", CultureInfo.InvariantCulture);

                        string demBoundingBox = $@"south={southLatText}&north={northLatText}&west={westLngText}&east={eastLngText}";

                        string tileName = $@"dem_area_{meshResolution}m";
                        string demPath = Path.Combine(outputDirectory, tileName + ".tif");
                        string demPathTemp = Path.Combine(outputDirectory,tileName + "_temp.tif");

                        // Creates *.bat File for starting GeoConvert-Process (elevation data processing)
                        using (StreamWriter textBatConvert = new StreamWriter($@"{elevationDirectory}/mesh_conv.bat"))
                        {
                            //text.WriteLine($@"start /D {Settings.AFS2SDKDirectory}aerofly_fs_2_geoconvert\ aerofly_fs_2_geoconvert.exe {Settings.WorkingDirectory}map_00_area_data/mesh_conv.tmc");
                            textBatConvert.WriteLine($@"start {Settings.AFS2SDKDirectory}aerofly_fs_2_geoconvert\aerofly_fs_2_geoconvert.exe mesh_conv.tmc");
                        }

                        // Creates *.tmc File needed for the GeoConvert-Process (depending of meshresolution and gridSquareLevel)
                        string inputFolderImages = $"./input_aerial_images/";
                        string outputFolderTTC = $"./geoconvert-tth/";
                        var textTMCImages = new TMCElevationFile(inputFolderImages, outputFolderTTC, afsBoundingBox, meshResolution, gridSquareLevel); // this.settings.ZoomLevel
                        string outputFilePath = $@"{elevationDirectory}\mesh_conv.tmc";
                        File.WriteAllText(outputFilePath, textTMCImages.GeneratedContent);

                        //
                        var downloader = new ElevationDataDownloader(openTopographyAPIUrl, outputDirectory, tileName + "_temp", openTopographyDemType, demBoundingBox, settings.OpenTopographyApiKey);
                        try
                        {
                            // Task starten, um den Download asynchron durchzuführen
                            await Task.Run(() =>
                            {
                                downloader.DownloadElevationData();
                                var loader = new GeoTiffLoader();
                                _terrainData = loader.Load(demPathTemp);
                                GeoTiffExporter.SaveCutoutAsGeoTiff(_terrainData.HeightMap, _terrainData, _terrainData.OriginLongitude, _terrainData.OriginLatitude, demPath);
                                File.Delete(demPathTemp );

                            });

                            // Creates *.aid-File for running GeoConvert-Process (elevation data processing)
                            outputFilePath = $@"{outputDirectory}\dem_area_{meshResolution}m.aid";
                            var textAIDElevation = new AIDElevationFile(_terrainData.OriginLongitude, _terrainData.OriginLatitude, _terrainData.PixelSizeX, _terrainData.PixelSizeY, meshResolution);
                            File.WriteAllText(outputFilePath, textAIDElevation.GeneratedContent);

                            this.mainForm.UpdateChildTaskLabel($"Download of Elevation Data completed");
                            log.Info("Downloading and fixing of Elevation Data completed");

                            var downloadActionProgressPercentage = this.mainForm.CurrentActionProgressPercentage;
                            int downloadElevationDataProgressPercentage = (i + 1) * 100 / this.mainForm.SelectedAFS2GridSquares.Count();

                            if (downloadElevationDataProgressPercentage > downloadActionProgressPercentage)
                            {
                                this.mainForm.CurrentActionProgressPercentage = downloadElevationDataProgressPercentage;
                            }

                            var existingGridSquare = this.dataRepository.FindGridSquare(afs2GridSquare.Name);

                            if (existingGridSquare == null)
                            {
                                this.dataRepository.CreateDataSquare(this.gridSquareMapper.ToModel(afs2GridSquare));
                                this.mainForm.AddDataGridSquare(afs2GridSquare);
                            }

                        }
                        catch (Exception ex)
                        {
                            // Fehlerbehandlung
                            this.mainForm.UpdateChildTaskLabel($"Error while downloading Elevation Data (refer to Log)");
                            log.Info($"Error while downloading and transforming Elevation Data for the tile {afs2GridSquare.Name} : {ex.Message}");
                        }
                    }

                    i++;

                }

                // 6. Run Geoconvert Process
                // If required Move on to running Geoconvert for each tile (Image-Processing)
                if (this.settings.RunGeoConvert.Value && this.mainForm.ActionsRunning)
                {
                    this.StartGeoConvertProcess();
                }


                //#MOD
                // 7. Run Trees Detection 
                // Running external App for TreesDetection by chrispriv (C) with optional OSM based masking of the images 
                if (this.Settings.RunTreesDetection.Value && this.mainForm.ActionsRunning)
                {
                    this.mainForm.UpdateChildTaskLabel($"Running Trees Detection");
                    log.Info($"Running Trees Detection");
                    //#TRY_k   
                    await Task.Delay(50);
                    this.StartTreesDetectionProcess();
                }


                //#Nickohod (not implemented)
                // Delete Stitched Immage Tiles
                //if (this.Settings.DeleteStitchedImageTiles)
                //{
                //    this.mainForm.UpdateChildTaskLabel("Deleting Stitched Image Tiles");

                //    // If we haven't just downloaded image tiles we need to load aero files to get image tile objects
                //    if (imageTiles == null)
                //    {
                //        imageTiles = await this.imageTileService.LoadImageTilesAsync(tileDownloadDirectory);
                //    }

                //}


                // Install Scenery
                //if (this.Settings.InstallScenery)
                //{
                //    this.mainForm.UpdateChildTaskLabel("Prompting To Install Scenery");

                //}

                //#TRY_k
                //this.mainForm.UpdateChildTaskLabel($"Actions Completed");



                this.ActionsComplete();

            }
            finally
            {
                if (this.imageTiles != null)
                {
                    this.imageTiles.Clear();
                    this.imageTiles = null;
                    System.GC.Collect();
                }
            }

        }

        public void StartGeoConvertProcess()
        {
            if (this.mainForm.ActionsRunning)
            {
                if (String.IsNullOrEmpty(this.settings.AFS2SDKDirectory))
                {
                    var messageBox = new CustomMessageBox("Please set the location of the Aerofly SDK in Settings before running Geoconvert",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                else
                {
                    if (settings.AFSLevelsToGenerate.Count == 0)
                    {
                        var messageBox = new CustomMessageBox("Please choose one or more AFS levels to generate before running Geoconvert",
                            "AeroScenery",
                            MessageBoxIcon.Warning);

                        messageBox.ShowDialog();
                    }
                    else
                    {

                        if (this.mainForm.SelectedAFS2GridSquares.Count > 1 && 
                            this.settings.GeoConvertUseWrapper.Value == false)
                        {
                            if (this.settings.ShowMultipleConcurrentSquaresWarning.HasValue && this.settings.ShowMultipleConcurrentSquaresWarning.Value)
                            {
                                string message = "When running GeoConvert on multiple squares it's advisable to use GeoCovnert Wrapper.\n";
                                message += "This will make GeoConvert instances run sequentially rather than in parallel.\n";
                                message += "You can enable GeoConvert Wrapper in the GeoConvert tab of the settings form.\n";
                                message += "See the AeroScenery wiki for more information on how this works.\n";


                                var messageBox = new CustomMessageBox(message,
                                    "AeroScenery",
                                    MessageBoxIcon.Information);

                                messageBox.ShowDialog();
                            }
                        }

                        RunGeoConvertProcess();

                    }

                }

            }

        }

        public void RunGeoConvertProcess()
        {
            log.Info("Starting GeoConvert Process");

            int i = 0;

            // Run the Geoconvert process for each selected grid square
            foreach (AFS2GridSquare afs2GridSquare in this.mainForm.SelectedAFS2GridSquares.Values.Select(x => x.AFS2GridSquare))
            {
                if (this.mainForm.ActionsRunning)
                {
                    var currentGrideSquareMessage = String.Format("Working on AFS Grid Square {0} of {1}", i + 1, this.mainForm.SelectedAFS2GridSquares.Count());
                    this.mainForm.UpdateParentTaskLabel(currentGrideSquareMessage);
                    log.Info(currentGrideSquareMessage);

                    // Do we have a directory for this afs grid square in our working directory?
                    var afsGridSquareDirectory = this.settings.WorkingDirectory + afs2GridSquare.Name;

                    if (Directory.Exists(afsGridSquareDirectory))
                    {
                        var stitchedTilesDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-stitched\";

                        if (Directory.Exists(stitchedTilesDirectory))
                        {
                            // Create raw and ttc directories if required. They could have been deleted manually.
                            var rawDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-geoconvert-raw\";
                            var ttcDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-geoconvert-ttc\";

                            if (!Directory.Exists(rawDirectory))
                            {
                                Directory.CreateDirectory(rawDirectory);
                            }

                            if (!Directory.Exists(ttcDirectory))
                            {
                                Directory.CreateDirectory(ttcDirectory);
                            }

                            this.geoConvertManager.RunGeoConvert(stitchedTilesDirectory, this.mainForm, this.settings.GeoConvertUseWrapper.Value);
                        }
                        else
                        {
                            var messageBox = new CustomMessageBox(String.Format("Could not find any stitched images for the grid square {0}", afs2GridSquare.Name),
                                "AeroScenery",
                                MessageBoxIcon.Error);

                            messageBox.ShowDialog();
                        }

                    }
                    else
                    {
                        // Working directory does not exist
                    }

                    i++;
                }
            }
        }

        //#MOD
        public void StartTreesDetectionProcess()
        {
            if (this.mainForm.ActionsRunning)
            {
                if (String.IsNullOrEmpty(this.settings.TreesDetectionDirectory))
                {
                    var messageBox = new CustomMessageBox("Please set the location of the TreesDetection Directory in Settings before running TreesDetection",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                else if ((this.settings.ZoomLevel != 16) && (this.settings.ZoomLevel != 15))
                {
                    var messageBox = new CustomMessageBox("The Image Source does not have the Zoom Level 16 or 15: \r Please download the images with Zoom Level 16 2.389 meters/pixel or 15 4.777m",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                else if ((this.settings.RunTreesDetectionMask != true) && (this.settings.RunTreesDetectionDetection != true))
                {
                    var messageBox = new CustomMessageBox("Select either the option 'Mask Images (optional)' or 'Generate TSC /TOC Files' for trees detecting or run both together",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                else if (settings.MaximumStitchedImageSize > 80) 
                {
                    var messageBox = new CustomMessageBox("TreesDetection supports only 'max. tiles per stiched images' up to 80 (20'480 x 20'480 px) in Settings",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                else
                {
                    if (this.mainForm.SelectedAFS2GridSquares.Count > 4)
                    {
                        var messageBox = new CustomMessageBox("You have selected more than four tiles, which may cause problems with the performance depending on your equipment.\r\r Continue anyway ...",
                            "AeroScenery",
                            MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                    }

                    if ((settings.TreesDetectionAltitudeCheck.Value) && (settings.OpenTopographyApiKey == "")) 
                    {
                        var messageBox = new CustomMessageBox("For altitude check of TreesDetection an API Key need to be add in Settings to access OpenTopography).\r\r Continue anyway ...",
                            "AeroScenery",
                            MessageBoxIcon.Warning);

                        messageBox.ShowDialog();
                    }

                    RunTreesDetectionProcess();
                }
            }
        }

        //#MOD
        public void RunTreesDetectionProcess()
        {

            log.Info("Starting TreesDetection Process");

            int i = 0;

            // Run the TreesDetection process for each selected grid square
            foreach (AFS2GridSquare afs2GridSquare in this.mainForm.SelectedAFS2GridSquares.Values.Select(x => x.AFS2GridSquare))
            {
                if (this.mainForm.ActionsRunning)
                {
                    var currentGrideSquareMessage = String.Format("Working on AFS Grid Square {0} of {1}", i + 1, this.mainForm.SelectedAFS2GridSquares.Count());
                    this.mainForm.UpdateParentTaskLabel(currentGrideSquareMessage);
                    //TRY_k
                    Application.DoEvents(); // UI wird sofort aktualisiert
                    log.Info(currentGrideSquareMessage);

                    // Do we have a directory for this afs grid square in our working directory?
                    var afsGridSquareDirectory = this.settings.WorkingDirectory + afs2GridSquare.Name;

                    //#MOD
                    // Writes in addition a PS1 PowerShell Script for download of GeoTiff-images provided from OpenTopography of the selected gridsquare for Altitude Check (API Key needed to be add in Settings)

                    //if ((this.mainForm.ActionsRunning) && (settings.OpenTopographyApiKey != "") && ((settings.TreesDetectionAltitudeCheck.Value)))
                    if ((settings.OpenTopographyApiKey != "") && ((settings.TreesDetectionAltitudeCheck.Value)))
                    {
                        // Create subdirectory '\qgis' for elevation data, if it not allready existing
                        var treesElevationDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees-elevation\";
                        if (!Directory.Exists(treesElevationDirectory))
                        {
                            Directory.CreateDirectory(treesElevationDirectory);
                        }
                        log.InfoFormat($"Writing and running in addition a PowerShell Script for download and conversion of GeoTiff image from OpenTopography for the tile {afs2GridSquare.Name}");

                        // Writing and running in addition a PowerShell Script for download and conversion of GeoTiff image from OpenTopography for the selected tile, if not allready done before
                        if ((File.Exists(treesElevationDirectory + afs2GridSquare.Name + ".xyz") == false) && (File.Exists(treesElevationDirectory + afs2GridSquare.Name + ".csv") == false))
                        {
                            var openTopographyAPIUrl = "https://portal.opentopography.org/API/globaldem?demtype=";
                            var openTopographyDemType = settings.OpenTopographyDataSet.Substring(0, settings.OpenTopographyDataSet.IndexOf(" "));

                            var boarderCorr = 0.0005; // Enlarging of the boarder box for a small overlapping of the images (actually fix value)
                            var eastLngCorr = afs2GridSquare.EastLongitude + boarderCorr;
                            var westLngCorr = afs2GridSquare.WestLongitude - boarderCorr;
                            var northLatCorr = afs2GridSquare.NorthLatitude + boarderCorr;
                            var southLatCorr = afs2GridSquare.SouthLatitude - boarderCorr;

                            string eastLng = eastLngCorr.ToString("#.#########", CultureInfo.InvariantCulture);
                            string westLng = westLngCorr.ToString("#.#########", CultureInfo.InvariantCulture);
                            string northLat = northLatCorr.ToString("#.#########", CultureInfo.InvariantCulture);
                            string southLat = southLatCorr.ToString("#.#########", CultureInfo.InvariantCulture);

                            string bbox = $@"&south={southLat}&north={northLat}&west={westLng}&east={eastLng}";

                            using (StreamWriter text = new StreamWriter($@"{treesElevationDirectory}_download_elevation_geotiff.ps1"))
                            {
                                text.WriteLine("Set-ExecutionPolicy Bypass -scope Process -Force");
                                text.WriteLine();
                                text.WriteLine("$client = new-object System.Net.WebClient");
                                text.WriteLine();
                                //text.WriteLine($@"Write-Host 'Proceeding download of the GeoTiff-Image from OpenTopography (Dataset {settings.OpenTopographyDataSet}):'");
                                text.WriteLine($@"Write-Host 'Proceeding download of the GeoTiff-Image from OpenTopography (Dataset SRTM GL3 (90m)):'");
                                text.WriteLine($@"Write-Host ''");
                                text.WriteLine($@"Write-Host 'Download of {afs2GridSquare.Name}.tif'");
                                text.WriteLine($@"Write-Host ''");
                                text.WriteLine($@"Write-Host 'Please wait ...'");
                                //text.WriteLine(($@"$client.DownloadFile('{openTopographyAPIUrl}{openTopographyDemType}{bbox}&outputFormat=GTiff&API_Key={settings.OpenTopographyApiKey}','{treesElevationDirectory}{afs2GridSquare.Name}.tif')"));
                                text.WriteLine(($@"$client.DownloadFile('{openTopographyAPIUrl}SRTMGL3{bbox}&outputFormat=GTiff&API_Key={settings.OpenTopographyApiKey}','{treesElevationDirectory}{afs2GridSquare.Name}.tif')"));
                                text.WriteLine();
                                text.WriteLine($@"Write-Host ''");
                                text.WriteLine($@"Write-Host 'Convert GeoTiff to ""xyz""-Elevation file for altitude check of TreesDetection'");
                                text.WriteLine($@"Write-Host ''");
                                text.WriteLine($@"{settings.QGISDirectory}gdal_translate {treesElevationDirectory}{afs2GridSquare.Name}.tif {treesElevationDirectory}{afs2GridSquare.Name}.xyz -of xyz");
                                text.WriteLine();
                                text.WriteLine($@"Write-Host ''");
                                if (this.settings.TreesDetectionQuit == false)
                                {
                                    text.WriteLine("Write-Host ''");
                                    text.WriteLine($@"Read-Host -Prompt 'Download finsihed - Press ENTER to quit'");
                                }
                            }
                            var proc = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = @"powershell.exe",
                                    Arguments = $@"-NoProfile -ExecutionPolicy ByPass -File ""{treesElevationDirectory}_download_elevation_geotiff.ps1""",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = false,
                                    RedirectStandardError = false,
                                    CreateNoWindow = false,
                                    WorkingDirectory = $@"{treesElevationDirectory}"
                                }
                            };

                            proc.Start();
                            //#MOD_k
                            if (this.settings.RunTreesDetectionDetection == true) 
                            {
                                proc.WaitForExit();
                            }
                        }
                    }

                    if (Directory.Exists(afsGridSquareDirectory)) 
                    {
                        var stitchedImageDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-stitched\";
                        var maskingImagesDirectory = afsGridSquareDirectory + @"\c-mask\" + this.settings.ZoomLevel + @"-stitched\";
                        var maskedImagesDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees-masked\";
                        var treesDetectedDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees\";

                        //#TRY_k
                        var downloadActionProgressPercentage = this.mainForm.CurrentActionProgressPercentage;
                        int downloadElevationDataProgressPercentage = (i + 1) * 100 / this.mainForm.SelectedAFS2GridSquares.Count();

                        if (downloadElevationDataProgressPercentage > downloadActionProgressPercentage)
                        {
                            this.mainForm.CurrentActionProgressPercentage = downloadElevationDataProgressPercentage;
                        }

                        //#DEVL_k
                        // Masks all png images within the stitchedImage directory using the masked images and saves them under maskedImages directory.
                        if (this.settings.RunTreesDetectionMask == true)
                        {
                            log.Info("Masking of Image Tiles");
                            //#WORKING_k
                            //int maskingRange = Math.Max((this.settings.ZoomLevel ?? 0) - 15,0);
                            //maskingRange = (int)Math.Pow(2, maskingRange);
                            int maskingRange = 1;
                            OsmMaskingBatchProcessor.RunMaskingBatch(stitchedImageDirectory, maskingImagesDirectory, maskedImagesDirectory, maskingRange);
                        }

                        // Skip running external TreesDetection if "Generate TSC/TOC Files" not selected (means masking images only)
                        if (this.settings.RunTreesDetectionDetection == false) 
                        {
                            continue;
                        }

                        // First build the chain of arguments (command line) for running external TreesDetection 
                        string argTreesDetection;

                        // Assign parameter for Installation Path for FS2TreesDetection containing the FS2TreesDetection.config-file
                        string argTreesDetectionConfig = "/p:" + this.settings.TreesDetectionDirectory + " ";

                        // Assign parameter Inputpath containing the aerial images for detection, depending if masked images are available (just check if folder exists)
                        string argTreesDetectionInput = "/i:";
                        //if (Directory.Exists(maskedImagesDirectory) && (this.settings.RunTreesDetectionMask == false)) 
                        if (Directory.Exists(maskedImagesDirectory))
                        {
                            argTreesDetectionInput = argTreesDetectionInput + maskedImagesDirectory + " ";
                        }
                        else 
                        {
                            argTreesDetectionInput = argTreesDetectionInput + stitchedImageDirectory + " ";
                        }

                        /* 
                        // Not used anymore, because masking is done before running TreesDetection
                        // Assign parameters containing the map images as a base for masking the aerial images and the path for saving the masked images (without ':' will not generate masked images) 
                        string argTreesDetectionMap = "/m ";
                        string argTreesDetectionMasked = "/s ";
                        if (this.settings.RunTreesDetectionMask == true)
                        {
                            // Does only create the masked images if the the masking image folder is available
                            if (Directory.Exists(maskingImagesDirectory))
                            {
                                if (!Directory.Exists(maskedImagesDirectory))
                                {
                                    Directory.CreateDirectory(maskedImagesDirectory);
                                }

                                //#WORKING_k
                                //new OsmBasedAlphaMasking()
                                //.OsmAlphaImageMasking("baseImagePfad.png", "osmImagePfad.png")
                                //.Save("maskedImagePfad.png", ImageFormat.Png);

                                argTreesDetectionMap = "/m:" + maskingImagesDirectory + " ";
                                argTreesDetectionMasked = "/s:" + maskedImagesDirectory + " ";
                            }
                        }
                        */

                        // Assign parameter for saving the detected trees in *.toc file with corresponding *.tsc file 
                        string argTreesDetectionOutput = "/o ";
                        if (this.settings.RunTreesDetectionDetection == true) 
                        {
                            if (!Directory.Exists(treesDetectedDirectory))
                            {
                                Directory.CreateDirectory(treesDetectedDirectory);
                            }

                            argTreesDetectionOutput = "/o:" + GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees\ "; ;
                        }

                        //#MOD
                        // Assign parameter concerning the density 
                        string argTreesDetectionDensity = "/d:" + this.settings.TreesDetectionDensity.ToString() + " ";

                        //#MOD
                        string argGridSquareBoundaryBox = "/b:" + afs2GridSquare.WestLongitude.ToString("#.#######", CultureInfo.InvariantCulture) + "," + afs2GridSquare.NorthLatitude.ToString("#.#######", CultureInfo.InvariantCulture) + ",";
                        argGridSquareBoundaryBox = argGridSquareBoundaryBox + afs2GridSquare.EastLongitude.ToString("#.#######", CultureInfo.InvariantCulture) + "," + afs2GridSquare.SouthLatitude.ToString("#.#######", CultureInfo.InvariantCulture) + " ";

                        string argTreesDetectionAltitudeCheck = "";
                        string argTreesDetectionAltitudeDirectory = "";
                        if ((this.settings.RunTreesDetectionDetection == true) && (this.settings.TreesDetectionAltitudeCheck == true))
                        {
                            argTreesDetectionAltitudeCheck = "/a:" + settings.TreesDetectionAltitudeMax + " ";
                            argTreesDetectionAltitudeDirectory = "/e:" + GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees-elevation\ "; 
                        }

                        //#MOD
                        string argTreesPresetIndex = "";
                        if (this.settings.TreesPresetIndex != null) 
                        {
                            argTreesPresetIndex = "/t:" + this.settings.TreesPresetIndex.ToString();

                            if (this.settings.TreesPresetHighTrees == true)
                                { argTreesPresetIndex = argTreesPresetIndex + "x"; }
                            else
                                { argTreesPresetIndex = argTreesPresetIndex + "o"; }

                            if (this.settings.TreesPresetBigShrubs == true)
                                { argTreesPresetIndex = argTreesPresetIndex + "x"; }
                            else
                                { argTreesPresetIndex = argTreesPresetIndex + "o"; }

                            argTreesPresetIndex = argTreesPresetIndex + " ";
                        }



                        string argTreesDetectionQuit = "";
                        if (this.settings.TreesDetectionQuit == true) 
                        {
                            argTreesDetectionQuit = "/q";
                        }

                        //#MOD_k
                        // Assemble all paramters into the command line
                        argTreesDetection = argTreesDetectionConfig + argTreesDetectionInput + argTreesDetectionOutput + argTreesDetectionDensity + argGridSquareBoundaryBox + argTreesDetectionAltitudeCheck + argTreesDetectionAltitudeDirectory + argTreesPresetIndex + argTreesDetectionQuit;

                        // Assign attributes to ProcessStartInfo
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.CreateNoWindow = false;
                        startInfo.UseShellExecute = false;
                        startInfo.FileName = this.settings.TreesDetectionDirectory + "\\" + "FS2TreesDetection.exe";
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.Arguments = argTreesDetection;

                        try
                        {
                            // Start the TreesDetection process with the attributes as specified.
                            using (Process exeProcess = Process.Start(startInfo))
                            {
                                // waiting for exit only for sequentiell downloads needed (makes probably no sense)
                                //exeProcess.WaitForExit();
                            }
                        }
                        catch
                        {
                            var messageBox = new CustomMessageBox("Could not run the application FS2TreesDetection.exe:\r Please check the path of the TreesDetection Directory in Settings",
                                "AeroScenery",
                                MessageBoxIcon.Warning);

                            messageBox.ShowDialog();
                        }
                    }

                    i++;
                }
            }

        }


        private void ResetGridSquare(string gridSquareName)
        {
            var existingGridSquare = this.dataRepository.FindGridSquare(gridSquareName);

            if (existingGridSquare != null)
            {
                this.dataRepository.DeleteGridSquare(gridSquareName);
            }
        }


        private void DownloadThreadProgress_ProgressChanged(object sender, DownloadThreadProgress progress)
        {
            if (this.mainForm.ActionsRunning)
            {
                var progressControl = this.mainForm.GetDownloadThreadProgressControl(progress.DownloadThreadNumber);
                var percentageProgress = (int)Math.Floor(((double)progress.FilesDownloaded / (double)progress.TotalFiles) * 100);

                progressControl.SetProgressPercentage(percentageProgress);

                progressControl.SetImageTileCount(progress.FilesDownloaded, progress.TotalFiles);

                var downloadActionProgressPercentage = this.mainForm.CurrentActionProgressPercentage;

                if (percentageProgress > downloadActionProgressPercentage)
                {
                    this.mainForm.CurrentActionProgressPercentage = percentageProgress;
                }
            }

        }


        private void TileStitcherProgress_ProgressChanged(object sender, TileStitcherProgress progress)
        {
            if (this.mainForm.ActionsRunning)
            {

                var currentStitchedImagePercentage = ((double)progress.CurrentStitchedImage / (double)progress.TotalStitchedImages);
                var nextStitchedImagePercentage = ((double)(progress.CurrentStitchedImage + 1) / (double)progress.TotalStitchedImages);

                var tilesPercentage = ((double)(progress.CurrentTilesRenderedForCurrentStitchedImage) / (double)progress.TotalImageTilesForCurrentStitchedImage);

                var percentageIncreaseBetweenThisStitchedImageAndNext = nextStitchedImagePercentage - currentStitchedImagePercentage;

                var finalPercentageDbl = (currentStitchedImagePercentage + (percentageIncreaseBetweenThisStitchedImageAndNext * tilesPercentage)) * 100;
                //Debug.WriteLine(finalPercentageDbl);

                var finalPercentage = (int)Math.Floor(finalPercentageDbl);

                if (finalPercentage > 100)
                {
                    finalPercentage = 100;
                }

                this.mainForm.CurrentActionProgressPercentage = finalPercentage;

            }

        }

        private void AFSFileGeneratorProgress_ProgressChanged(object sender, AFSFileGeneratorProgress progress)
        {
            if (this.mainForm.ActionsRunning)
            {
                var precentDone = ((double)progress.FilesCreated / (double)progress.TotalFiles) * 100;

                this.mainForm.CurrentActionProgressPercentage = (int)precentDone;

            }
        }

        public bool AllImageTilesDownloaded(List<ImageTile> imageTiles)
        {
            return true;
        }

        public void SaveSettings()
        {
            this.settingsService.SaveSettings(this.settings);
        }
        
        public string ApplicationPath
        {
            get
            {
                //#DEVL_k
                //Approach doesn't work when debugging in Visual Studio; but works when running as an executable --> looking for better solution 
                //var applicationUri = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                //var applicationLocalPath = new Uri(Path.GetDirectoryName(applicationUri)).LocalPath;

                string applicationLocalPath = AppContext.BaseDirectory;

                return applicationLocalPath;

            }
        }


    }
}
