using AeroScenery.AFS2;
using AeroScenery.Common;
using AeroScenery.Controls;
using AeroScenery.Data;
using AeroScenery.Data.Mappers;
using AeroScenery.Data.Models;
using AeroScenery.Download;
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
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//#MOD_f
using System.Globalization;

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
        //#MOD_e
        private MapboxOrthophotoSource mapboxOrthophotoSource;
        //#MOD_b 
        private GoogleOrthomapSource googleOrthomapSource;
        private GoogleOrthoroadmapSource googleOrthoroadmapSource;
        private OSMMapsOrthomapSource osmMapsOrthomapSource;
        private CartoDBLightOrthomapSource cartoDBLightOrthomapSource;

        private DownloadManager downloadManager;

        private GeoConvertManager geoConvertManager;

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
            version = "1.1.3 MOD j by @chrispriv"; //#MOD_j
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
            //#MOD_e
            mapboxOrthophotoSource = new MapboxOrthophotoSource();
            //#MOD_b
            googleOrthomapSource = new GoogleOrthomapSource(); 
            googleOrthoroadmapSource = new GoogleOrthoroadmapSource(); 
            osmMapsOrthomapSource = new OSMMapsOrthomapSource();
            cartoDBLightOrthomapSource = new CartoDBLightOrthomapSource();

            this.mainForm = new MainForm();
            this.mainForm.StartStopClicked += async (sender, eventArgs) =>
            {
                //#MOD_g
                // Bug fix: Adding a delay for Start & Stops reduces the occurrence of an unhandled error when stopping the download (bug appears since the number of download threads has been increased from 4 to 8)
                await Task.Delay(600);

                if (this.mainForm.ActionsRunning)
                {
                    //#MOD_g
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
                //#MOD_e
                case OrthophotoSource.Mapbox:
                    tileDownloadDirectory += String.Format("\\{0}\\", OrthophotoSourceDirectoryName.Mapbox);
                    break;
                //#MOD_b
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
                //#MOD_h
                double selectedTilesEastLongitude = -180;
                double selectedTilesWestLongitude = 180;
                double selectedTilesNorthLatitude = -90;
                double selectedTilesSouthLatitude = 90;

                // Set settings on orthophoto sources
                this.linzOrthophotoSource.ApiKey = settings.LinzApiKey;
                //#MOD_e
                this.mapboxOrthophotoSource.ApiKey = settings.MapboxApiKey;
                //#MOD_h
                this.hereWeGoOrthophotoSource.ApiKey = settings.HereWeGoApiKey;

                int i = 0;
                foreach (AFS2GridSquare afs2GridSquare in this.mainForm.SelectedAFS2GridSquares.Values.Select(x => x.AFS2GridSquare))
                {
                    var currentGrideSquareMessage = String.Format("Working on AFS Grid Square {0} of {1}", i + 1, this.mainForm.SelectedAFS2GridSquares.Count());
                    this.mainForm.UpdateParentTaskLabel(currentGrideSquareMessage);
                    log.Info(currentGrideSquareMessage);

                    //#MOD_h
                    // Determine maximum coverage of all selected tiles for manual download of GeoTiff-images provided from OpenTopography
                    if (selectedTilesEastLongitude < afs2GridSquare.EastLongitude) { selectedTilesEastLongitude = afs2GridSquare.EastLongitude; }
                    if (selectedTilesWestLongitude > afs2GridSquare.WestLongitude) { selectedTilesWestLongitude = afs2GridSquare.WestLongitude; }
                    if (selectedTilesNorthLatitude < afs2GridSquare.NorthLatitude) { selectedTilesNorthLatitude = afs2GridSquare.NorthLatitude; }
                    if (selectedTilesSouthLatitude > afs2GridSquare.SouthLatitude) { selectedTilesSouthLatitude = afs2GridSquare.SouthLatitude; }

                    //#MOD_h
                    // If Action Running Check at the level of tiles check and create the working folders and subfolders (not done if only "Downlaod Elevation Data (30m) for selected area" selected) 
                    var afsGridSquareDirectory = this.settings.WorkingDirectory + afs2GridSquare.Name;

                    var tileDownloadDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"\";
                    var stitchedTilesDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-stitched\";

                    if ((this.Settings.DownloadImageTiles.Value || (this.Settings.DownloadImageTiles.Value || (this.Settings.FixMissingTiles.Value) || (this.Settings.StitchImageTiles.Value) || (this.Settings.GenerateAIDAndTMCFiles.Value) || (this.Settings.RunGeoConvert.Value) || (this.Settings.RunTreesDetection.Value)) && this.mainForm.ActionsRunning)) 
                    {
                        // Do we have a directory for this afs grid square in our working directory?
                        //var afsGridSquareDirectory = this.settings.WorkingDirectory + afs2GridSquare.Name;

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

                    // Download Imamge Tiles
                    if (this.Settings.DownloadImageTiles.Value && this.mainForm.ActionsRunning)
                    {
                        this.mainForm.UpdateChildTaskLabel("Calculating Image Tiles To Download");
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
                                //#MOD_e
                                case OrthophotoSource.Mapbox:
                                    imageTiles = mapboxOrthophotoSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                                    orthophotoSourceInstance = mapboxOrthophotoSource;
                                    break;
                                //#MOD_b
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

                        this.mainForm.UpdateChildTaskLabel("Downloading Image Tiles");
                        log.Info("Downloading Image Tiles");

                        // Capture the progress of each thread
                        var downloadThreadProgress = new Progress<DownloadThreadProgress>();
                        downloadThreadProgress.ProgressChanged += DownloadThreadProgress_ProgressChanged;

                        // Send the image tiles to the download manager
                        //#MOD_g
                        //await downloadManager.DownloadImageTiles(settings.OrthophotoSource.Value, imageTiles, downloadThreadProgress, tileDownloadDirectory, orthophotoSourceInstance);
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
                        }


                    }

                    //#MOD_h
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
                        //#MOD_h
                        await Task.Delay(100);
                    }

                    // Stitch Image Tiles
                    if (this.Settings.StitchImageTiles.Value && this.mainForm.ActionsRunning)
                    {
                        this.mainForm.UpdateChildTaskLabel("Stitching Image Tiles");
                        log.Info("Stitching Image Tiles");

                        // Capture the progress of the tile stitcher
                        var tileStitcherProgress = new Progress<TileStitcherProgress>();
                        tileStitcherProgress.ProgressChanged += TileStitcherProgress_ProgressChanged;

                        //#MOD_h
                        //await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectory, stitchedTilesDirectory, true, tileStitcherProgress);
                        await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectory, stitchedTilesDirectory, true, settings.OrthophotoSource.Value, tileStitcherProgress);
                    }

                    // Generate AID and TMC Files
                    if (this.Settings.GenerateAIDAndTMCFiles.Value && this.mainForm.ActionsRunning)
                    {
                        this.mainForm.UpdateChildTaskLabel("Generating AFS Metadata Files");
                        log.Info("Generating AFS Metadata Files");

                        // Capture the progress of the tile stitcher
                        var afsFileGeneratorProgress = new Progress<AFSFileGeneratorProgress>();
                        afsFileGeneratorProgress.ProgressChanged += AFSFileGeneratorProgress_ProgressChanged;


                        // Generate AID files for the image tiles
                        await afsFileGenerator.GenerateAFSFilesAsync(afs2GridSquare, stitchedTilesDirectory, GetTileDownloadDirectory(afsGridSquareDirectory), afsFileGeneratorProgress);

                    }


                    //#MOD_i
                    // Create additional Working Folder incl. tmc & bat file for conversion of images to ttc for mobile (to be run manually after GeoConvert process)
                    if (this.Settings.GenerateAIDAndTMCFiles.Value && this.settings.CreateAddForMobile.Value && this.mainForm.ActionsRunning)
                    {
                        var afsAddForMobileWorkingDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + @"\\" + this.settings.ZoomLevel + "-geoconvert-ttc-mobile";

                        if (!Directory.Exists(afsAddForMobileWorkingDirectory))
                        {
                            Directory.CreateDirectory(afsAddForMobileWorkingDirectory);

                            if (!Directory.Exists(afsAddForMobileWorkingDirectory + @"\\images"))
                            {
                                Directory.CreateDirectory(afsAddForMobileWorkingDirectory + @"\\images");
                            }

                            using (StreamWriter text = new StreamWriter($@"{afsAddForMobileWorkingDirectory}\content_converter_config_mobile.bat"))
                            {
                                text.WriteLine("start content_converter_config_mobile.tmc");
                            }
                            using (StreamWriter text = new StreamWriter($@"{afsAddForMobileWorkingDirectory}\content_converter_config_mobile.tmc"))
                            {
                                text.WriteLine("<[file][][]");
                                text.WriteLine("    <[tm_config][][]");
                                text.WriteLine();
                                text.WriteLine("        <[string8][base_output_folder][./]>");
                                text.WriteLine("        <[string8][texture_base_type][ttc_etc2]>");
                                text.WriteLine();
                                text.WriteLine("        <[list_tm_config_folderpair][folder_pairs][]");
                                text.WriteLine("            <[tm_config_folderpair][element][1]");
                                text.WriteLine($"                <[string8][input_folder][../{this.settings.ZoomLevel}-geoconvert-raw/]>");
                                text.WriteLine("                <[string8][output_folder][./images/]>");
                                text.WriteLine("                <[string8][type][place]>");
                                text.WriteLine("                <[uint32][recurse_level][0]>");
                                text.WriteLine("                <[list_string8][file_types][tsc tgi jpg bmp tif png toc]>");
                                text.WriteLine("                <[list_tm_texture_settings][texture_settings][]");
                                text.WriteLine("                    <[tm_config_folderpair][element][0]");
                                text.WriteLine("                        <[list_string8][regex][.*]>");
                                text.WriteLine("                        <[bool][compressed][true]>");
                                text.WriteLine("                        <[bool][compress_file][true]>");
                                text.WriteLine("                        <[bool][flip_vertical][true]>");
                                text.WriteLine("                        <[bool][mipmaps][true]>");
                                text.WriteLine("                        <[uint][max_size][2048]>");
                                text.WriteLine("                        <[bool][make_square][true]>");
                                text.WriteLine("                    >");
                                text.WriteLine("                >");
                                text.WriteLine("                <[tm_config_geometry_settings][geometry_settings][]");
                                text.WriteLine("                    <[float32][collision_mesh_quality][0]>");
                                text.WriteLine("                >");
                                text.WriteLine("            >");
                                text.WriteLine("        >");
                                text.WriteLine("    >");
                                text.WriteLine(">");

                            }
                        }
                    }

                    //#MOD_f
                    // Writes a PS1 PowerShell Script for manual download of OSM data of the selected gridsquare, this even if no other action is selected
                    //#MOD_h
                    //if (this.mainForm.ActionsRunning)
                    if (this.settings.DownloadOsmData.Value && this.mainForm.ActionsRunning)
                    {
                        // Create subdirectory for osm data, if it not allready existing
                        if (!Directory.Exists(afsGridSquareDirectory + "/osm"))
                        {
                            Directory.CreateDirectory(afsGridSquareDirectory + "/osm");
                        }
                        log.InfoFormat($"Writing in addition a PowerShell Script for manual download of OSM data for the tile {afs2GridSquare.Name}");

                        var osmOverpassUrl = "https://overpass-api.de/api/map?bbox=";

                        var boarderCorr = 0.0005; // Correction of the boarder box to avoid flickering houses (actually fix value)
                        var eastLngCorr = afs2GridSquare.EastLongitude - boarderCorr;
                        var westLngCorr = afs2GridSquare.WestLongitude + boarderCorr;
                        var northLatCorr = afs2GridSquare.NorthLatitude - boarderCorr;
                        var southLatCorr = afs2GridSquare.SouthLatitude + boarderCorr;

                        string eastLng = eastLngCorr.ToString("#.####", CultureInfo.InvariantCulture);
                        string westLng = westLngCorr.ToString("#.####", CultureInfo.InvariantCulture);
                        string northLat = northLatCorr.ToString("#.####", CultureInfo.InvariantCulture);
                        string southLat = southLatCorr.ToString("#.####", CultureInfo.InvariantCulture);

                        string bbox = $@"{westLng},{southLat},{eastLng},{northLat}";

                        using (StreamWriter text = new StreamWriter($@"{afsGridSquareDirectory}\osm\_download_osm_map.ps1"))
                        {
                            text.WriteLine("Set-ExecutionPolicy Bypass -scope Process -Force");
                            text.WriteLine();
                            text.WriteLine("$client = new-object System.Net.WebClient");
                            text.WriteLine();
                            text.WriteLine($@"Write-Host 'Proceeding download of the OSM Data from OpenStreetMap (Overpass API):'");
                            text.WriteLine($@"Write-Host 'Download of the file ""{afs2GridSquare.Name}.osm"" for the selected tile'");
                            text.WriteLine($@"Write-Host ''");
                            text.WriteLine($@"Write-Host 'Please wait ...'");
                            text.WriteLine(($@"$client.DownloadFile('{osmOverpassUrl}{bbox}','{afsGridSquareDirectory}\osm\{afs2GridSquare.Name}.osm')"));
                            text.WriteLine();
                            text.WriteLine($@"Write-Host ''");
                            text.WriteLine($@"Write-Host 'Download finsihed and saved in ""{afsGridSquareDirectory}\osm\...""'");

                            if (this.settings.TreesDetectionQuit == false) 
                            {
                                text.WriteLine($@"Write-Host ''");
                                text.WriteLine($@"Read-Host -Prompt 'Press ENTER to quit'");
                            }
                        }

                        //#MOD_h
                        var proc = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = @"powershell.exe",
                                Arguments = $@"-NoProfile -ExecutionPolicy ByPass -File ""{afsGridSquareDirectory}\osm\_download_osm_map.ps1""",
                                UseShellExecute = false,
                                RedirectStandardOutput = false,
                                RedirectStandardError = false,
                                CreateNoWindow = false,
                                WorkingDirectory = $@"{afsGridSquareDirectory}\osm\"
                            }
                        };

                        proc.Start();
                        //proc.WaitForExit();

                    }

                    //#MOD_h
                    // DOwnload of the masking images for TreesDetection if "Mask image (optional)" is selected
                    if (this.Settings.RunTreesDetectionMask.Value && this.Settings.RunTreesDetection.Value && this.mainForm.ActionsRunning) 
                    {
                        var afsGridSquareDirectoryMask = this.settings.WorkingDirectory + afs2GridSquare.Name;

                        var tileDownloadDirectoryMask = afsGridSquareDirectoryMask + @"\c-mask\" + this.settings.ZoomLevel + @"\";
                        var stitchedTilesDirectoryMask = afsGridSquareDirectoryMask + @"\c-mask\" + +this.settings.ZoomLevel + @"-stitched\";

                        // Do we have a directory for the afs grid square and this orthophoto source
                        if (!Directory.Exists(tileDownloadDirectoryMask))
                        {
                            Directory.CreateDirectory(tileDownloadDirectoryMask);
                        }

                        if (!Directory.Exists(stitchedTilesDirectoryMask))
                        {
                            Directory.CreateDirectory(stitchedTilesDirectoryMask);
                        }

                        this.mainForm.UpdateChildTaskLabel("Calculating Masking Image Tiles To Download");
                        log.Info("Calculating Masking Image Tiles To Download");

                        GenericOrthophotoSource orthophotoSourceInstance = null;

                        imageTiles = cartoDBLightOrthomapSource.ImageTilesForGridSquares(afs2GridSquare, settings.ZoomLevel.Value);
                        orthophotoSourceInstance = cartoDBLightOrthomapSource;


                        this.mainForm.UpdateChildTaskLabel("Downloading Masking Image Tiles");
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
                        //#MOD_h
                        await Task.Delay(100);

                        //}

                        // Stitch Masking Image Tiles
                        this.mainForm.UpdateChildTaskLabel("Stitching Masking Image Tiles");
                        log.Info("Stitching Masking Image Tiles");

                        // Capture the progress of the tile stitcher
                        var tileStitcherProgress = new Progress<TileStitcherProgress>();
                        tileStitcherProgress.ProgressChanged += TileStitcherProgress_ProgressChanged;

                        //#MOD_h
                        //await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectoryMask, stitchedTilesDirectoryMask, true, tileStitcherProgress);
                        await this.tileStitcher.StitchImageTilesAsync(tileDownloadDirectoryMask, stitchedTilesDirectoryMask, true, OrthophotoSource.CartoDBLight, tileStitcherProgress);

                    }

                    i++;

                }

                //#MOD_h (End of)

                // If required Move on to running Geoconvert for each tile (Image-Processing)
                if (this.settings.RunGeoConvert.Value && this.mainForm.ActionsRunning)
                {
                    this.StartGeoConvertProcess();
                }





                //#MOD_g
                // Adding Treesdetection from chrispriv (C) at this place 
                if (this.Settings.RunTreesDetection.Value && this.mainForm.ActionsRunning)
                {
                    this.StartTreesDetectionProcess();
                }

                //#MOD_h
                // Writes in addition a PS1 PowerShell Script for manual download of GeoTiff-images provided from OpenTopography (30m) for the selected area (API Key needed to be add in Settings), this even if no other action is selected
                if ((settings.OpenTopographyApiKey == "") && (this.settings.DownloadElevationData == true) && (this.mainForm.ActionsRunning))
                {
                    var messageBox = new CustomMessageBox("API Key need to be add in Settings to access OpenTopography for the download of elevation data of the selected area).\r\r Abort download ...",
                    "AeroScenery",
                    MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                else if ((this.settings.DownloadElevationData == true) && (this.mainForm.ActionsRunning))
                {
                    //#MOD_h
                    if (this.settings.QGISDirectory == "") 
                    {
                        var messageBox = new CustomMessageBox("Path to 'QGIS-bin Folder (incl. GDLA)' has to be set in Settings (usually 'C:\\OSGeo4W\\bin\\'). \rThan the GeoTIFF images are automatically decompressed and the peaks near the coast are removed.\r\r Continue anyway with manual handling using QGIS ...",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                        messageBox.ShowDialog();
                    }
                                        
                    RunDownloadElevationDataProcess(selectedTilesEastLongitude, selectedTilesWestLongitude, selectedTilesNorthLatitude, selectedTilesSouthLatitude);
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

        //#MOD_g
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
                //#MOD_h
                /*
                else if (settings.AFSLevelsToGenerate.Count == 0)
                {
                    var messageBox = new CustomMessageBox("Please choose one or more AFS levels to generate before running TreesDetection",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                */
                else if (settings.MaximumStitchedImageSize > 80) 
                {
                    var messageBox = new CustomMessageBox("TreesDetection supports only 'max. tiles per stiched images' up to 80 (20'480 x 20'480 px) in Settings",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                //#MOD_h -not needed anymore
                /*
                else if (settings.OrthophotoSource == OrthophotoSource.CartoDBLight)
                {
                    var messageBox = new CustomMessageBox("TreesDetection has to be run on the satelite images and not on the maps used for masking",
                        "AeroScenery",
                        MessageBoxIcon.Warning);

                    messageBox.ShowDialog();
                }
                */
                else
                {
                    if (this.mainForm.SelectedAFS2GridSquares.Count > 4)
                    {
                        var messageBox = new CustomMessageBox("You have selected more than four tiles, which may cause problems with the performance depending on your equipment.\r\r Continue anyway ...",
                            "AeroScenery",
                            MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                    }
                    /*
                    if (this.settings.ZoomLevel == 15)
                    {
                        var messageBox = new CustomMessageBox("You are operating TreesDetection with Zoom Level 15. For better results use Level 16.\r\r Continue anyway ...",
                            //#MOD_h
                            //"The selected Density Level in Settings will automatically be adapted. \r\r Continue anyway ...",
                            "AeroScenery",
                            MessageBoxIcon.Information);

                        messageBox.ShowDialog();
                    }
                    */
                    //#MOD_h
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

        //#MOD_g
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
                    log.Info(currentGrideSquareMessage);

                    // Do we have a directory for this afs grid square in our working directory?
                    var afsGridSquareDirectory = this.settings.WorkingDirectory + afs2GridSquare.Name;

                    //#MOD_h
                    // Writes in addition a PS1 PowerShell Script for download of GeoTiff-images provided from OpenTopography of the selected gridsquare for Altitude Check (API Key needed to be add in Settings)
                    if ((this.mainForm.ActionsRunning) && (settings.OpenTopographyApiKey != "") && ((settings.TreesDetectionAltitudeCheck.Value)))
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
                            proc.WaitForExit();
                        }
                    }

                    if (Directory.Exists(afsGridSquareDirectory)) 
                    {
                        var stitchedImageDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-stitched\";
                        var maskingImagesDirectory = afsGridSquareDirectory + @"\c-mask\" + this.settings.ZoomLevel + @"-stitched\";
                        var maskedImagesDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees-masked\";
                        var treesDetectedDirectory = GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees\";

                        string argTreesDetection;
                        // Assign parameter for Installation Path for FS2TreesDetection containing the FS2TreesDetection.config-file
                        string argTreesDetectionConfig = "/p:" + this.settings.TreesDetectionDirectory + " ";

                        // Assign parameter Inputpath containing the aerial images for detection, depending if masked images are available (just check if folder exists)
                        string argTreesDetectionInput = "/i:";
                        if (Directory.Exists(maskedImagesDirectory) && (this.settings.RunTreesDetectionMask == false)) 
                        {
                            argTreesDetectionInput = argTreesDetectionInput + maskedImagesDirectory + " ";
                        }
                        else 
                        {
                            argTreesDetectionInput = argTreesDetectionInput + stitchedImageDirectory + " ";
                        }

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


                                argTreesDetectionMap = "/m:" + maskingImagesDirectory + " ";
                                argTreesDetectionMasked = "/s:" + maskedImagesDirectory + " ";
                            }
                        }

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

                        //#MOD_i
                        // Assign parameter concerning the density 
                        //double treesDetectionDensity = Convert.ToDouble(this.settings.TreesDetectionDensity);
                        //string argTreesDetectionDensity = "/d:" + Convert.ToString(treesDetectionDensity + " ");
                        string argTreesDetectionDensity = "/d:" + this.settings.TreesDetectionDensity.ToString() + " ";

                        //#MOD_h
                        string argGridSquareBoundaryBox = "/b:" + afs2GridSquare.WestLongitude.ToString("#.#######", CultureInfo.InvariantCulture) + "," + afs2GridSquare.NorthLatitude.ToString("#.#######", CultureInfo.InvariantCulture) + ",";
                        argGridSquareBoundaryBox = argGridSquareBoundaryBox + afs2GridSquare.EastLongitude.ToString("#.#######", CultureInfo.InvariantCulture) + "," + afs2GridSquare.SouthLatitude.ToString("#.#######", CultureInfo.InvariantCulture) + " ";

                        //#MOD_h
                        string argTreesDetectionAltitudeCheck = "";
                        string argTreesDetectionAltitudeDirectory = "";
                        if ((this.settings.RunTreesDetectionDetection == true) && (this.settings.TreesDetectionAltitudeCheck == true))
                        {
                            argTreesDetectionAltitudeCheck = "/a:" + settings.TreesDetectionAltitudeMax + " ";
                            argTreesDetectionAltitudeDirectory = "/e:" + GetTileDownloadDirectory(afsGridSquareDirectory) + this.settings.ZoomLevel + @"-trees-elevation\ "; 
                        }

                        //#MOD_i
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

                        // Assemble all paramters into the Comman Line
                        //#MOD_h
                        //argTreesDetection = argTreesDetectionConfig + argTreesDetectionInput + argTreesDetectionOutput + argTreesDetectionMap + argTreesDetectionMasked + argTreesDetectionDensity + argGridSquareBoundaryBox + argTreesDetectionAltitudeCheck + argTreesDetectionAltitudeDirectory + argTreesDetectionQuit;
                        //#MOD_i
                        argTreesDetection = argTreesDetectionConfig + argTreesDetectionInput + argTreesDetectionOutput + argTreesDetectionMap + argTreesDetectionMasked + argTreesDetectionDensity + argGridSquareBoundaryBox + argTreesDetectionAltitudeCheck + argTreesDetectionAltitudeDirectory + argTreesPresetIndex + argTreesDetectionQuit;

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
                    else
                    {
                        // Working directory does not exist
                    }

                    i++;
                }
            }
        }





        //#MOD_h
        public void RunDownloadElevationDataProcess(double selectedTilesEastLongitude, double selectedTilesWestLongitude, double selectedTilesNorthLatitude, double selectedTilesSouthLatitude)
        {
            // Writes in addition a PS1 PowerShell Script for manual download of GeoTiff-images provided from OpenTopography (30m) for the selected area (API Key needed to be add in Settings), this even if no other action is selected

            var openTopographyAPIUrl = "https://portal.opentopography.org/API/globaldem?demtype=";
            //#MOD_i
            var meshResolution = "30m";
            if (settings.OpenTopographyDataSet.Substring(0, 4) == "USGS")
            {
                openTopographyAPIUrl = "https://portal.opentopography.org/API/usgsdem?datasetName=";
                if (settings.OpenTopographyDataSet.Substring(0, 7) == "USGS10m")
                {
                    meshResolution = "10m";
                }
            }

            var openTopographyDemType = settings.OpenTopographyDataSet.Substring(0, settings.OpenTopographyDataSet.IndexOf(" "));

            var workingAreaDataFolder = "map_00_elevation";
            workingAreaDataFolder = workingAreaDataFolder + "_" + DateTime.Now.ToString("yyyMMdd_HHmm") + "\\";

            // Create subdirecties for elevation data, if it not allready existing
            if (!Directory.Exists(Settings.WorkingDirectory + workingAreaDataFolder))
            {
                Directory.CreateDirectory(Settings.WorkingDirectory + workingAreaDataFolder);
            }
            if (!Directory.Exists(Settings.WorkingDirectory + workingAreaDataFolder + "input_aerial_images"))
            {
                Directory.CreateDirectory(Settings.WorkingDirectory + workingAreaDataFolder + "input_aerial_images");
            }
            if (!Directory.Exists(Settings.WorkingDirectory + workingAreaDataFolder + "output_elevation"))
            {
                Directory.CreateDirectory(Settings.WorkingDirectory + workingAreaDataFolder + "output_elevation");
            }

            log.InfoFormat($"Writing and running a PowerShell Script for download of GeoTiff image from OpenTopography for the selected area");

            // Convert coordinates of area-extention to Text (incl. enlarging for the downlaod of the GeoTiff-image) and creates text for the boundary box
            var eastLngText = selectedTilesEastLongitude.ToString("#.#########", CultureInfo.InvariantCulture);
            var westLngText = selectedTilesWestLongitude.ToString("#.#########", CultureInfo.InvariantCulture);
            var northLatText = selectedTilesNorthLatitude.ToString("#.#########", CultureInfo.InvariantCulture);
            var southLatText = selectedTilesSouthLatitude.ToString("#.#########", CultureInfo.InvariantCulture);

            //#MOD_i: 0.02 instead od 0.01 due to larger tile levels (7 & 8 supported now)
            var boarderCorr = 0.02; // Enlarging of the boarder box for a small overlapping of the GeoTiff-images to avoid bugs at the boarder (actually fix value that seams to work fine; there is also a mismatch with the real downloaded image)
            var eastLngCorr = selectedTilesEastLongitude + boarderCorr;
            var westLngCorr = selectedTilesWestLongitude - boarderCorr;
            var northLatCorr = selectedTilesNorthLatitude + boarderCorr;
            var southLatCorr = selectedTilesSouthLatitude - boarderCorr;

            var eastLngCorrText = eastLngCorr.ToString("#.#####", CultureInfo.InvariantCulture);
            var westLngCorrText = westLngCorr.ToString("#.#####", CultureInfo.InvariantCulture);
            var northLatCorrText = northLatCorr.ToString("#.#####", CultureInfo.InvariantCulture);
            var southLatCorrText = southLatCorr.ToString("#.#####", CultureInfo.InvariantCulture);

            var bbox = $@"&south={southLatCorrText}&north={northLatCorrText}&west={westLngCorrText}&east={eastLngCorrText}";

            // Creates PowerShell-Sricpt for manual dowload of the GeoTiff-Image: Downlaoded image has to be imported and exported using QGIS, will else not work (not compatible for immediate convertion)
            using (StreamWriter text = new StreamWriter($@"{Settings.WorkingDirectory}{workingAreaDataFolder}_download_elevation_geotiff.ps1"))
            {
                text.WriteLine("Set-ExecutionPolicy Bypass -scope Process -Force");
                text.WriteLine();
                text.WriteLine("$client = new-object System.Net.WebClient");
                text.WriteLine();
                text.WriteLine($@"Write-Host 'Proceeding download of the GeoTiff-Image from OpenTopography (Dataset {settings.OpenTopographyDataSet}):'");
                text.WriteLine("Write-Host ''");
                //#MOD_i
                text.WriteLine($@"Write-Host 'Download of the file ""dem_area_{meshResolution}.tif"" for the selected area '");
                text.WriteLine("Write-Host ''");
                text.WriteLine("Write-Host 'Please wait ...'");
                //#MOD_i
                text.WriteLine(($@"$client.DownloadFile('{openTopographyAPIUrl}{openTopographyDemType}{bbox}&outputFormat=GTiff&API_Key={settings.OpenTopographyApiKey}','input_aerial_images\dem_area_{meshResolution}_bak.tif')"));
                text.WriteLine("Write-Host ''");
                text.WriteLine($@"Write-Host 'Download finsihed and saved in ""{Settings.WorkingDirectory}{workingAreaDataFolder}input_aerial_images\...""'");
                text.WriteLine("Write-Host ''");

                if (settings.QGISDirectory == "")
                {
                    //#MOD_i
                    text.WriteLine($@"Copy '{Settings.WorkingDirectory}{workingAreaDataFolder}input_aerial_images\dem_area_{meshResolution}'_bak.tif' '{Settings.WorkingDirectory}{workingAreaDataFolder}input_aerial_images\dem_area_{meshResolution}.tif'");
                }
                else
                {
                    text.WriteLine();
                    text.WriteLine("Write-Host ''");
                    text.WriteLine("Write-Host '-------------------------------------------------------------------------------------------------------------------'");
                    text.WriteLine("Write-Host ''");
                    text.WriteLine($@"Write-Host 'Fix peaks near coastline (set negative elevations to zero)'");
                    text.WriteLine("Write-Host ''");
                    text.WriteLine("Write-Host 'Please wait ...'");
                    //#MOD_i
                    text.WriteLine($@"{settings.QGISDirectory}qgis_process-qgis run qgis:rastercalculator --LAYERS=""{Settings.WorkingDirectory}{workingAreaDataFolder}\input_aerial_images\dem_area_{meshResolution}_bak.tif"" --OUTPUT=""{Settings.WorkingDirectory}{workingAreaDataFolder}\input_aerial_images\dem_area_{meshResolution}.tif"" --EXPRESSION=""('dem_area_{meshResolution}_bak@1' >= 0) * 'dem_area_{meshResolution}_bak@1'""  --CRS=EPSG:4326 --distance_units=meters --area_units=m2 --ellipsoid=EPSG:4326");
                    //Decompress of GeoTiff (not needed if peak fix is allready done) 
                    //text.WriteLine($@"{settings.QGISDirectory}gdal_translate {Settings.WorkingDirectory}{workingAreaDataFolder}input_aerial_images\dem_area_30m_fixed.tif {Settings.WorkingDirectory}{workingAreaDataFolder}input_aerial_images\dem_area_30m_uncompressed.tif -co COMPRESS=NONE");
                }
                if (this.settings.TreesDetectionQuit == false)
                {
                    text.WriteLine("Write-Host ''");
                    text.WriteLine($@"Read-Host -Prompt 'Press ENTER to quit'");
                }

            }
            // Creates *.aid-File for GeoConvert-Process as a draft: Values have to be checked and addapted if necessary using GQIS
            using (StreamWriter text = new StreamWriter($@"{Settings.WorkingDirectory}{workingAreaDataFolder}input_aerial_images/dem_area_30m.aid"))
            {
                text.WriteLine("<[file][][]");
                text.WriteLine("    <[tm_aerial_image_definition][][]");
                //#MOD_i
                text.WriteLine($@"        <[string8][image][dem_area_{meshResolution}.tif]>");
                text.WriteLine("        <[string8][mask][]>");
                //#MOD_i
                if (meshResolution == "30m") // Default 30m
                {
                    text.WriteLine("        <[vector2_float64][steps_per_pixel][0.000277778 -0.000277778]> // [<Horizontal> -<Vertical(minus!)>] ");
                }
                else  // HighRes 10m for USGS10m
                {
                    text.WriteLine("        <[vector2_float64][steps_per_pixel][0.0000925926 -0.0000925926]> // [<Horizontal> -<Vertical(minus!)>] ");
                }
                text.WriteLine($@"        <[vector2_float64][top_left][{westLngCorrText} {northLatCorrText}]> // [<West> <Nord>]");
                text.WriteLine("        <[string8][coordinate_system][lonlat]>");
                text.WriteLine("        <[bool][flip_vertical][false]>");
                text.WriteLine("    >");
                text.WriteLine(">");
            }

            //#MOD_h
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"powershell.exe",
                    Arguments = $@"-NoProfile -ExecutionPolicy ByPass -File ""{Settings.WorkingDirectory}{workingAreaDataFolder}_download_elevation_geotiff.ps1""",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = false,
                    WorkingDirectory = $@"{Settings.WorkingDirectory}{workingAreaDataFolder}\"
                }
            };

            proc.Start();
            //proc.WaitForExit();

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
                if (!Directory.Exists(Settings.WorkingDirectory + workingAreaDataFolder + "shader_dx11"))
                {
                    Directory.CreateDirectory(Settings.WorkingDirectory + workingAreaDataFolder + "shader_dx11");
                    foreach (string newPath in Directory.GetFiles(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/shader_dx11", "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(newPath, newPath.Replace(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/shader_dx11", Settings.WorkingDirectory + workingAreaDataFolder + "shader_dx11"), true);
                    }
                }
                if (!Directory.Exists(Settings.WorkingDirectory + workingAreaDataFolder + "texture"))
                {
                    Directory.CreateDirectory(Settings.WorkingDirectory + workingAreaDataFolder + "texture");
                    foreach (string newPath in Directory.GetFiles(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/texture", "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(newPath, newPath.Replace(Settings.AFS2SDKDirectory + "aerofly_fs_2_geoconvert/texture", Settings.WorkingDirectory + workingAreaDataFolder + "texture"), true);
                    }
                }

                // Creates *.bat File for starting GeoConvert-Process (Elevation-Processing)
                using (StreamWriter text = new StreamWriter($@"{Settings.WorkingDirectory}{workingAreaDataFolder}mesh_conv.bat"))
                {
                    //text.WriteLine($@"start /D {Settings.AFS2SDKDirectory}aerofly_fs_2_geoconvert\ aerofly_fs_2_geoconvert.exe {Settings.WorkingDirectory}map_00_area_data/mesh_conv.tmc");
                    text.WriteLine($@"start {Settings.AFS2SDKDirectory}aerofly_fs_2_geoconvert\aerofly_fs_2_geoconvert.exe mesh_conv.tmc");
                }

                // Creates *.tmc File needed for the GeoConvert-Process (for 30m-Meshes up to level 10 resp. up to level 11 for 10m-Meshes)
                using (StreamWriter text = new StreamWriter($@"{Settings.WorkingDirectory}{workingAreaDataFolder}mesh_conv.tmc"))
                {
                    text.WriteLine("<[file][][]");
                    text.WriteLine("    <[tmcolormap_regions][][]");
                    text.WriteLine("        <[string] [folder_source_files][input_aerial_images/]>");
                    text.WriteLine("        <[bool]   [write_images_with_mask][false]>");
                    text.WriteLine("        <[bool]   [write_ttc_files][false]>");
                    text.WriteLine("        <[bool]   [do_heightmaps][true]>");
                    //text.WriteLine("        <[string8][folder_destination_ttc][./scenery/images/]>");
                    text.WriteLine("        <[string8][folder_destination_heightmaps][output_elevation/]>");
                    text.WriteLine("        <[bool]   [always_overwrite][true]>");
                    text.WriteLine("");
                    text.WriteLine("        <[list][region_list][]");
                    text.WriteLine("");

                    text.WriteLine("			<[tmheightmap_region][element][0]");
                    text.WriteLine("                <[uint32]              [level]  [8]>");
                    text.WriteLine($@"			    <[vector2_float64]     [lonlat_min]   [{westLngText} {southLatText}]>// [<West> <Süd>]");
                    text.WriteLine($@"			    <[vector2_float64]     [lonlat_max]   [{eastLngText} {northLatText}]>// [<Ost> <Nord>]");
                    text.WriteLine("			    <[bool]                [write_images_with_mask][false]>");
                    text.WriteLine("            >");
                    text.WriteLine("");
                    text.WriteLine("			<[tmheightmap_region][element][1]");
                    text.WriteLine("                <[uint32]              [level]  [9]>");
                    text.WriteLine($@"			    <[vector2_float64]     [lonlat_min]   [{westLngText} {southLatText}]>// [<West> <Süd>]");
                    text.WriteLine($@"			    <[vector2_float64]     [lonlat_max]   [{eastLngText} {northLatText}]>// [<Ost> <Nord>]");
                    text.WriteLine("			    <[bool]                [write_images_with_mask][false]>");
                    text.WriteLine("            >");
                    text.WriteLine("");
                    text.WriteLine("			<[tmheightmap_region][element][2]");
                    text.WriteLine("                <[uint32]              [level]  [10]>");
                    text.WriteLine($@"			    <[vector2_float64]     [lonlat_min]   [{westLngText} {southLatText}]>// [<West> <Süd>]");
                    text.WriteLine($@"			    <[vector2_float64]     [lonlat_max]   [{eastLngText} {northLatText}]>// [<Ost> <Nord>]");
                    text.WriteLine("			    <[bool]                [write_images_with_mask][false]>");
                    text.WriteLine("            >");
                    //#MOD_i
                    if (meshResolution == "10m") 
                    {
                        text.WriteLine("			<[tmheightmap_region][element][2]");
                        text.WriteLine("                <[uint32]              [level]  [11]>");
                        text.WriteLine($@"			    <[vector2_float64]     [lonlat_min]   [{westLngText} {southLatText}]>// [<West> <Süd>]");
                        text.WriteLine($@"			    <[vector2_float64]     [lonlat_max]   [{eastLngText} {northLatText}]>// [<Ost> <Nord>]");
                        text.WriteLine("			    <[bool]                [write_images_with_mask][false]>");
                        text.WriteLine("            >");
                    }

                    text.WriteLine("        >");
                    text.WriteLine("    >");
                    text.WriteLine(">");
                }

                //#MOD_i
                // CReates addition bat- & tmc-file for mobiles on Android
                if (Settings.CreateAddForMobile == true) 
                {
                    // Creates additional *.bat File for starting GeoConvert-Process (Elevation-Processing) on Android
                    using (StreamWriter text = new StreamWriter($@"{Settings.WorkingDirectory}{workingAreaDataFolder}mesh_conv_for_mobile.bat"))
                    {
                        text.WriteLine($@"start {Settings.AFS2SDKDirectory}aerofly_fs_2_geoconvert\aerofly_fs_2_geoconvert.exe mesh_conv_for_mobile.tmc");
                    }

                    // Creates additional *.tmc File needed for the GeoConvert-Process (for 30m-Meshes up to level 10) on Android
                    using (StreamWriter text = new StreamWriter($@"{Settings.WorkingDirectory}{workingAreaDataFolder}mesh_conv_for_mobile.tmc"))
                    {
                        text.WriteLine("<[file][][]");
                        text.WriteLine("    <[tmcolormap_regions][][]");
                        text.WriteLine("        <[string] [folder_source_files][input_aerial_images/]>");
                        text.WriteLine("        <[bool]   [write_images_with_mask][false]>");
                        text.WriteLine("        <[bool]   [write_ttc_files][false]>");
                        text.WriteLine("        <[bool]   [do_heightmaps][true]>");
                        //text.WriteLine("        <[string8][folder_destination_ttc][./scenery/images/]>");
                        text.WriteLine("        <[string8][folder_destination_heightmaps][output_elevation/]>");
                        text.WriteLine("        <[bool]   [always_overwrite][true]>");
                        text.WriteLine("");
                        text.WriteLine("        <[list][region_list][]");
                        text.WriteLine("");

                        text.WriteLine("			<[tmheightmap_region][element][0]");
                        text.WriteLine("                <[uint32]              [level]  [7]>");
                        text.WriteLine($@"			    <[vector2_float64]     [lonlat_min]   [{westLngText} {southLatText}]>// [<West> <Süd>]");
                        text.WriteLine($@"			    <[vector2_float64]     [lonlat_max]   [{eastLngText} {northLatText}]>// [<Ost> <Nord>]");
                        text.WriteLine("			    <[bool]                [write_images_with_mask][false]>");
                        text.WriteLine("            >");
                        text.WriteLine("");
                        text.WriteLine("			<[tmheightmap_region][element][1]");
                        text.WriteLine("                <[uint32]              [level]  [10]>");
                        text.WriteLine($@"			    <[vector2_float64]     [lonlat_min]   [{westLngText} {southLatText}]>// [<West> <Süd>]");
                        text.WriteLine($@"			    <[vector2_float64]     [lonlat_max]   [{eastLngText} {northLatText}]>// [<Ost> <Nord>]");
                        text.WriteLine("			    <[bool]                [write_images_with_mask][false]>");
                        text.WriteLine("            >");

                        text.WriteLine("        >");
                        text.WriteLine("    >");
                        text.WriteLine(">");
                    }
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
                var applicationUri = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var applicationLocalPath = new Uri(Path.GetDirectoryName(applicationUri)).LocalPath;
                return applicationLocalPath;

            }
        }


    }
}
