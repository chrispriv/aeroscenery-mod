using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using AeroScenery.ImageProcessing;
using AeroScenery.Controls;
using System.Windows;

namespace AeroScenery.UI
{
    public partial class SettingsForm : Form
    {
        private readonly ILog log = LogManager.GetLogger("AeroScenery");

        private ImageProcessingPreviewForm imageProcessingPreviewForm;

        private bool updateImagePreview;
        //#MOD
        private bool showMessageStartAppAgain = false;

        public SettingsForm()
        {
            InitializeComponent();
            this.updateImagePreview = true;

            //#MOD
            ToolTip toolTip1 = new ToolTip();
            toolTip1.IsBalloon = true;
            toolTip1.InitialDelay = 500;
            toolTip1.SetToolTip(this.sdkGeoConvertHelpImage, "Aerofly FS2 GeoConvert as part of the Aerofly FS2 SDK is needed for conversion of images (there is a link under 'Get Aerofly FS2 SDK').\nAfter downloading it set the path to the root folder of the SDK containing the subfolder '...\\aerofly_fs2_geoconvert\\'.");

            ToolTip toolTip2 = new ToolTip();
            toolTip2.IsBalloon = true;
            toolTip2.InitialDelay = 500;
            toolTip2.SetToolTip(this.elevationMapHelpImage, "An account with API Key is needed for free download of elevation data from OpenTopography.org.\nIf a key is set, then the additional option 'Download Elevation Data (30m)' for selected area will appear.\nAfter running with the option set just run the PowerShell script _download_elevation_geotiff.ps1 for download.\nRunning the mesh_conv.bat batch file will generate the tth files for AeroFly elevation data based on the input aerial images using GeoConvert process.");

            ToolTip toolTip3 = new ToolTip();
            toolTip3.IsBalloon = true;
            toolTip3.InitialDelay = 500;
            toolTip3.SetToolTip(this.conversionForMobileHelpImage, "Aerofly FS2 Content Converter installed as part of the Aerofly FS2 SDK is needed for additional conversion of images to compatible ttc-files.\nIf this option is set the step 'Generate AID / TMC' Files will create an additional working folder for conversion of the raw images created by GeoConvert process.\nAfter termination of GeoConvert process just run the content_converter_config_mobile.bat batch file to get working ttc files for Mobile (Android).");

            ToolTip toolTip4 = new ToolTip();
            toolTip4.IsBalloon = true;
            toolTip4.InitialDelay = 500;
            toolTip4.SetToolTip(this.treesDetectionHelpImage, "With the link to the TreesDetection App (available on flight-sim.org), cultivations with trees can be created for Aerofly FS2 on the basis of satellite photos (for FS4 trees are already included).\nIf the path to the separate App is set, then the additional option 'Run TreesDetection' will appear (for better results to avoid trees on roads, water etc. you may choose the option 'Create Mask (optional)'.\nYou also have the ability to choose between trees presets and density, as well as set the upper tree line depending on the region.");

            ToolTip toolTip5 = new ToolTip();
            toolTip5.IsBalloon = true;
            toolTip5.InitialDelay = 500;
            toolTip5.SetToolTip(this.elevationQGISHelpImage, "...\n...");

            ToolTip toolTip6 = new ToolTip();
            toolTip6.IsBalloon = true;
            toolTip6.InitialDelay = 500;
            toolTip6.SetToolTip(this.imageProcessingHelpImage, "This option allows you to adjust images before GeoConvert process.\nAfter changing the parameters just run the single step 'Stitch Image Tiles' again to aply the changes.\nThe option 'Remove alpha chanel' replaces the alpha chanel of the sea with a default dark blue color (only works with masked Google images).");

            ToolTip toolTip7 = new ToolTip();
            toolTip6.IsBalloon = true;
            toolTip6.InitialDelay = 500;
            toolTip6.SetToolTip(this.movingMapElevationHelpImage, "...\n...\n...");

            ToolTip toolTip8 = new ToolTip();
            toolTip6.IsBalloon = true;
            toolTip6.InitialDelay = 500;
            toolTip6.SetToolTip(this.movingMapConnectionHelpImage, "...\n...\n...");

        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;

            settings.WorkingDirectory = pathWithTrailingDirectorySeparatorChar(this.workingFolderTextBox.Text);
            settings.AeroSceneryDBDirectory = pathWithTrailingDirectorySeparatorChar(this.aeroSceneryDatabaseFolderTextBox.Text);
            settings.AFS2SDKDirectory = pathWithTrailingDirectorySeparatorChar(this.afsSDKFolderTextBox.Text);
            settings.AFS2UserDirectory = pathWithTrailingDirectorySeparatorChar(this.afs2UserFolderTextBox.Text);
            //#MOD
            //settings.AFS4UserDirectory = pathWithTrailingDirectorySeparatorChar(this.afs4UserFolderTextBox.Text);
            settings.QGISDirectory = pathWithTrailingDirectorySeparatorChar(this.qgisFolderTextBox.Text);

            //#MOD_k
            settings.GeoTiffElevationMapFilename = this.movingMapElevationFileNameSetBox.Text;

            //#MOD
            settings.AFSSceneryFolder = pathWithTrailingDirectorySeparatorChar(this.afsSceneryFolderTextBox.Text);
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace(" ", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("#", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("%", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("*", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("&", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("?", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("/", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("+", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace(".", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace(",", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace(":", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace(";", "");
            settings.AFSSceneryFolder = settings.AFSSceneryFolder.Replace("__", "_");

            if (settings.AFS2SDKDirectory.Contains("aerofly_fs_2_geoconvert"))
            {
                settings.AFS2SDKDirectory = settings.AFS2SDKDirectory.Replace("aerofly_fs_2_geoconvert.exe", "");
                settings.AFS2SDKDirectory = settings.AFS2SDKDirectory.Replace("aerofly_fs_2_geoconvert", "");
                settings.AFS2SDKDirectory = settings.AFS2SDKDirectory.Replace("\\\\", "");
                settings.AFS2SDKDirectory = pathWithTrailingDirectorySeparatorChar(settings.AFS2SDKDirectory);
            }


            settings.UserAgent = this.userAgentTextBox.Text;

            if (!String.IsNullOrEmpty(this.downloadWaitTextBox.Text))
            {
                settings.DownloadWaitMs = int.Parse(this.downloadWaitTextBox.Text);
            }

            if (!String.IsNullOrEmpty(this.downloadWaitRandomTextBox.Text))
            {
                settings.DownloadWaitRandomMs = int.Parse(this.downloadWaitRandomTextBox.Text);
            }

            //#MOD
            if (Convert.ToInt32(this.simultaneousDownloadsComboBox.Text) != settings.SimultaneousDownloads)
            {
                showMessageStartAppAgain = true;
                settings.SimultaneousDownloads = Convert.ToInt32(this.simultaneousDownloadsComboBox.Text);
            }
            /*
            switch (this.simultaneousDownloadsComboBox.SelectedIndex)
            {
                case 0:
                    settings.SimultaneousDownloads = 4;
                    break;
                case 1:
                    settings.SimultaneousDownloads = 6;
                    break;
                case 2:
                    settings.SimultaneousDownloads = 8;
                    break;
            }
            */

            if (!String.IsNullOrEmpty(this.maxTilesPerStitchedImageTextBox.Text))
            {
                settings.MaximumStitchedImageSize = int.Parse(this.maxTilesPerStitchedImageTextBox.Text);
            }

            // Index 0 = yes, index 1 = no
            if (this.gcWriteImagesWithMaskCombo.SelectedIndex == 0)
            {
                settings.GeoConvertWriteImagesWithMask = true;
            }
            else
            {
                settings.GeoConvertWriteImagesWithMask = false;
            }

            if (this.gcWriteRawFilesComboBox.SelectedIndex == 0)
            {
                settings.GeoConvertWriteRawFiles= true;
            }
            else
            {
                settings.GeoConvertWriteRawFiles = false;
            }

            settings.GeoConvertUseWrapper = useGeoConvertWrapperCheckBox.Checked;
            settings.ShowMultipleConcurrentSquaresWarning = multipleConcurrentSquaresWarningCheckBox.Checked;

            //if (this.gcDoMultipleSmallerRunsComboBox.SelectedIndex == 0)
            //{
            //    settings.GeoConvertDoMultipleSmallerRuns= true;
            //}
            //else
            //{
            //    settings.GeoConvertDoMultipleSmallerRuns = false;
            //}

            settings.USGSPassword = this.usgsPasswordTextBox.Text.Trim();
            settings.USGSUsername = this.usgsUsernameTextBox.Text.Trim();
            settings.LinzApiKey = this.linzKeyTextBox.Text.Trim();
            //#MOD
            settings.MapboxApiKey = this.mapboxKeyTextBox.Text.Trim();

            //#MOD
            settings.OpenTopographyApiKey = this.openTopographyAPITextBox.Text.Trim();
            settings.OpenTopographyDataSet = this.openTopographyDataSetTextBox.Text;
            settings.HereWeGoApiKey = this.herewegoKeyTextBox.Text.Trim();

            //#MOD
            settings.TreesDetectionDirectory = pathWithTrailingDirectorySeparatorChar(this.treesDetectionFolderTextBox.Text);
            settings.TreesDetectionDensity = this.treesDetectionDensitySlider.Value;
            settings.TreesDetectionQuit = treesDetectionQuitCheckBox.Checked;
            settings.TreesDetectionAltitudeMax = this.treesDetectionAltitudeSlider.Value;
            settings.TreesDetectionAltitudeCheck = this.treesDetectionAltitudeCheckBox.Checked;

            //#MOD
            settings.CreateAddForMobile = createAddForMobileCheckBox.Checked;
            settings.DownloadOSMDataEnable = enableDownloadOsmDataCheckBox.Checked;
            if (settings.DownloadOSMDataEnable == false)
            {
                settings.DownloadOsmData = false;
            }

            settings.TreesPresetIndex = treesDetectionPresetComboBox.SelectedIndex;
            settings.TreesPresetHighTrees = treesDetectionHighTreesCheckBox.Checked;
            settings.TreesPresetBigShrubs = treesDetectionBigShrubsCheckBox.Checked;


            //#MOD_k
            settings.MovingMapElevationDataEnable = movingMapElevationEnableDataCheckBox.Checked;
            if (settings.MovingMapElevationDataEnable == false)
            {
                settings.MovingMapElevationData = false;
                settings.MovingMapElevationEnable3DCapture = false;
            }
            settings.MovingMapElevationFileName = movingMapElevationFileNameSetBox.Text;
            settings.MovingMapElevationDataRendering = movingMapElevationDataRenderingSlider.Value;;
            settings.MovingMapElevationEnable3DCapture = movingMapElevationEnable3DCaptureCheckBox.Checked;

            settings.ShrinkTMCGridSquareCoords = double.Parse(this.shrinkTMCGridSquaresTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture);

            // Image processing
            settings.EnableImageProcessing = this.imageProcessingEnabledCheckBox.Checked;
            settings.BrightnessAdjustment = this.imgProcBrightnessSlider.Value;
            settings.ContrastAdjustment = this.imgProcContrastSlider.Value;
            settings.SaturationAdjustment = this.imgProcSaturationSlider.Value;
            settings.SharpnessAdjustment = this.imgProcSharpnessSlider.Value;
            settings.RedAdjustment = this.imgProcRedSlider.Value;
            settings.GreenAdjustment = this.imgProcGreenSlider.Value;
            settings.BlueAdjustment = this.imgProcBlueSlider.Value;
            //#MOD
            settings.RemoveAlphaChannelAdjustment = this.imageRemoveAlphaChannelCheckBox.Checked;

            //#DEVL_k ### to be checked
            //settings.WaterMaskingProcessing = this.WaterMaskingEnabledCheckBox.Checked;
            settings.WaterMaskingEnable = this.WaterMaskingEnabledCheckBox.Checked;
            if (this.WaterMaskingEnabledCheckBox.Checked == false)
            {
                settings.WaterMaskingProcessing = false;
            }
            settings.WaterFadeThresholdDistance = this.waterFadeThresholdSlider.Value;
            settings.WaterReplaceThresholdDistance = this.waterReplaceThresholdSlider.Value;

            //#DEVL_k ###to be checked
            settings.AllowShiftCorrectionEnable = this.AllowShiftCorrectionEnabledCheckBox.Checked;
            if (this.AllowShiftCorrectionEnabledCheckBox.Checked == false) 
            { 
                settings.AllowShiftCorrectionProcessing = false;
                settings.AllowShiftCorrectionLevel = 0;
            } 

            AeroSceneryManager.Instance.SaveSettings();
            this.Hide();
            log.Info("Settings saved");

            //#MOD
            if (showMessageStartAppAgain) 
            {
                var messageBox = new CustomMessageBox("Please restart the App to make the changes effective.",
                    "AeroScenery",
                    MessageBoxIcon.Warning);

                messageBox.ShowDialog();
                
            }
        }

        private void SettingsForm_Shown(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;

            this.workingFolderTextBox.Text = settings.WorkingDirectory;
            this.aeroSceneryDatabaseFolderTextBox.Text = settings.AeroSceneryDBDirectory;
            this.afsSDKFolderTextBox.Text = settings.AFS2SDKDirectory;
            this.afs2UserFolderTextBox.Text = settings.AFS2UserDirectory;
            //#MOD
            this.qgisFolderTextBox.Text = settings.QGISDirectory;
            this.movingMapElevationFileNameSetBox.Text = settings.GeoTiffElevationMapFilename;
            this.afsSceneryFolderTextBox.Text = settings.AFSSceneryFolder;

            this.userAgentTextBox.Text = settings.UserAgent;
            this.downloadWaitTextBox.Text = settings.DownloadWaitMs.ToString();
            this.downloadWaitRandomTextBox.Text = settings.DownloadWaitRandomMs.ToString();

            //#MOD
            this.simultaneousDownloadsComboBox.Text = Convert.ToString(settings.SimultaneousDownloads);

            this.maxTilesPerStitchedImageTextBox.Text = settings.MaximumStitchedImageSize.ToString();

            //if (settings.GeoConvertDoMultipleSmallerRuns)
            //{
            //    this.gcDoMultipleSmallerRunsComboBox.SelectedIndex = 0;
            //}
            //else
            //{
            //    this.gcDoMultipleSmallerRunsComboBox.SelectedIndex = 1;
            //}

            if (settings.GeoConvertWriteImagesWithMask.Value)
            {
                this.gcWriteImagesWithMaskCombo.SelectedIndex = 0;
            }
            else
            {
                this.gcWriteImagesWithMaskCombo.SelectedIndex = 1;
            }

            if (settings.GeoConvertWriteRawFiles.Value)
            {
                this.gcWriteRawFilesComboBox.SelectedIndex = 0;
            }
            else
            {
                this.gcWriteRawFilesComboBox.SelectedIndex = 1;
            }

            useGeoConvertWrapperCheckBox.Checked = settings.GeoConvertUseWrapper.Value;
            multipleConcurrentSquaresWarningCheckBox.Checked = settings.ShowMultipleConcurrentSquaresWarning.Value;

            this.usgsUsernameTextBox.Text = settings.USGSUsername;
            this.usgsPasswordTextBox.Text = settings.USGSPassword;
            this.linzKeyTextBox.Text = settings.LinzApiKey;
            //#MOD
            this.mapboxKeyTextBox.Text = settings.MapboxApiKey;
            this.openTopographyAPITextBox.Text = settings.OpenTopographyApiKey;
            this.openTopographyDataSetTextBox.Text = settings.OpenTopographyDataSet;
            this.herewegoKeyTextBox.Text = settings.HereWeGoApiKey;

            //#MOD
            this.treesDetectionFolderTextBox.Text = settings.TreesDetectionDirectory;
            this.treesDetectionDensitySlider.Value = settings.TreesDetectionDensity.Value;
            this.treesDetectionDensityTextBox.Text = settings.TreesDetectionDensity.Value.ToString();
            this.treesDetectionQuitCheckBox.Checked = settings.TreesDetectionQuit.Value;

            this.treesDetectionAltitudeSlider.Value = settings.TreesDetectionAltitudeMax.Value;
            this.treesDetectionAltitudeTextBox.Text = settings.TreesDetectionAltitudeMax.Value.ToString();
            this.treesDetectionAltitudeCheckBox.Checked = settings.TreesDetectionAltitudeCheck.Value;

            //#MOD
            this.createAddForMobileCheckBox.Checked = settings.CreateAddForMobile.Value;
            this.enableDownloadOsmDataCheckBox.Checked = settings.DownloadOSMDataEnable.Value;

            this.treesDetectionPresetComboBox.SelectedIndex = settings.TreesPresetIndex.Value;
            this.treesDetectionHighTreesCheckBox.Checked = settings.TreesPresetHighTrees.Value;
            this.treesDetectionBigShrubsCheckBox.Checked = settings.TreesPresetBigShrubs.Value;

            //#DEVL_k
            this.movingMapElevationEnableDataCheckBox.Checked = settings.MovingMapElevationDataEnable.Value;
            this.movingMapElevationFileNameSetBox.Text = settings.MovingMapElevationFileName;
            this.movingMapElevationDataRenderingSlider.Value = settings.MovingMapElevationDataRendering.Value;
            this.movingMapElevationDataRenderingTextBox.Text = settings.MovingMapElevationDataRendering.Value.ToString();
            this.movingMapElevationEnable3DCaptureCheckBox.Checked = settings.MovingMapElevationEnable3DCapture.Value;
            if (movingMapElevationEnableDataCheckBox.Checked) 
                movingMapElevationEnable3DCaptureCheckBox.Enabled = true;

            this.shrinkTMCGridSquaresTextBox.Text = Convert.ToString(settings.ShrinkTMCGridSquareCoords, CultureInfo.InvariantCulture);

            // Image processing
            this.imageProcessingEnabledCheckBox.Checked = settings.EnableImageProcessing.Value;

            this.imgProcBrightnessSlider.Value = settings.BrightnessAdjustment.Value;
            this.imgProcBrightnessTextBox.Text = settings.BrightnessAdjustment.Value.ToString();

            this.imgProcContrastSlider.Value = settings.ContrastAdjustment.Value;
            this.imgProcContrastTextBox.Text = settings.ContrastAdjustment.Value.ToString();

            this.imgProcSaturationSlider.Value = settings.SaturationAdjustment.Value;
            this.imgProcSaturationTextBox.Text = settings.SaturationAdjustment.Value.ToString();

            this.imgProcSharpnessSlider.Value = settings.SharpnessAdjustment.Value;
            this.imgProcSharpnessTextBox.Text = settings.SharpnessAdjustment.Value.ToString();

            this.imgProcRedSlider.Value = settings.RedAdjustment.Value;
            this.imgProcRedTextBox.Text = settings.RedAdjustment.Value.ToString();
            this.imgProcGreenSlider.Value = settings.GreenAdjustment.Value;
            this.imgProcGreenTextBox.Text = settings.GreenAdjustment.Value.ToString();
            this.imgProcBlueSlider.Value = settings.BlueAdjustment.Value;
            this.imgProcBlueTextBox.Text = settings.BlueAdjustment.Value.ToString();

            //#MOD
            this.imageRemoveAlphaChannelCheckBox.Checked = settings.RemoveAlphaChannelAdjustment.Value;

            //#DEVL_k ### to be checked
            //this.WaterMaskingEnabledCheckBox.Checked = settings.WaterMaskingProcessing.Value;
            this.WaterMaskingEnabledCheckBox.Checked = settings.WaterMaskingEnable.Value;

            this.waterFadeThresholdSlider.Value = settings.WaterFadeThresholdDistance.Value;
            this.waterFadeThresholdTextBox.Text = settings.WaterFadeThresholdDistance.Value.ToString();
            this.waterReplaceThresholdSlider.Value = settings.WaterReplaceThresholdDistance.Value;
            this.waterReplaceThresholdTextBox.Text = settings.WaterReplaceThresholdDistance.Value.ToString();

            //#DEVL_k
            this.AllowShiftCorrectionEnabledCheckBox.Checked = settings.AllowShiftCorrectionEnable.Value;


            // Enable or disable sliders depending on whether image processing is enabled
            if (this.imageProcessingEnabledCheckBox.Checked)
            {
                this.ToggleImageProcessingControlsEnabled(true);
            }
            else
            {
                this.ToggleImageProcessingControlsEnabled(false);
            }

            //#DEVL_k
            if (this.WaterMaskingEnabledCheckBox.Checked)
            {
                this.ToggleImageWaterMaskingControlsEnabled(true);
            }
            else
            {
                this.ToggleImageWaterMaskingControlsEnabled(false);
            }
            //#DEVL_k ###probaby not needed
            if (this.AllowShiftCorrectionEnabledCheckBox.Checked)
            {
                //this.ToggleImageWaterMaskingControlsEnabled(true);
            }
            else
            {
                //this.ToggleImageWaterMaskingControlsEnabled(false);
            }


        }

        private void ToggleImageProcessingControlsEnabled(bool enabled)
        {
            this.imgProcBrightnessSlider.Enabled = enabled;
            this.imgProcBrightnessTextBox.Enabled = enabled;

            this.imgProcContrastSlider.Enabled = enabled;
            this.imgProcContrastTextBox.Enabled = enabled;

            this.imgProcSaturationSlider.Enabled = enabled;
            this.imgProcSaturationTextBox.Enabled = enabled;

            this.imgProcSharpnessSlider.Enabled = enabled;
            this.imgProcSharpnessTextBox.Enabled = enabled;

            this.imgProcRedSlider.Enabled = enabled;
            this.imgProcRedTextBox.Enabled = enabled;
            this.imgProcGreenSlider.Enabled = enabled;
            this.imgProcGreenTextBox.Enabled = enabled;
            this.imgProcBlueSlider.Enabled = enabled;
            this.imgProcBlueTextBox.Enabled = enabled;
        }

        private void ToggleImageWaterMaskingControlsEnabled(bool enabled)
        {
            this.waterFadeThresholdSlider.Enabled = enabled;
            this.waterFadeThresholdTextBox.Enabled = enabled;

            this.waterReplaceThresholdSlider.Enabled = enabled;
            this.waterReplaceThresholdTextBox.Enabled = enabled;
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void workingFolderButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            //MOD_i
            this.folderBrowserDialog1.SelectedPath = this.workingFolderTextBox.Text;

            DialogResult result = this.folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.workingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                //#FIX_f (else Cancel would not work)
                //settings.WorkingDirectory = this.workingFolderTextBox.Text;
            }
        }

        private void aerosceneryDatabaseFolderButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            //MOD_i
            this.folderBrowserDialog1.SelectedPath = this.aeroSceneryDatabaseFolderTextBox.Text;

            DialogResult result = this.folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.aeroSceneryDatabaseFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                //#FIX_f (else Cancel would not work)
                //settings.AeroSceneryDBDirectory = this.aeroSceneryDatabaseFolderTextBox.Text;
            }
        }

        private void sdkButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            //MOD_i
            this.folderBrowserDialog1.SelectedPath = this.afsSDKFolderTextBox.Text;

            DialogResult result = this.folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.afsSDKFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                //#FIX_f (else Cancel would not work)
                //settings.AFS2SDKDirectory = this.afsSDKFolderTextBox.Text;
            }

        }

        private void afsUserFolderButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            //MOD_i
            this.folderBrowserDialog1.SelectedPath = this.afs2UserFolderTextBox.Text;

            DialogResult result = this.folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.afs2UserFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                //#FIX_f (else Cancel would not work)
                //settings.AFS2UserDirectory = this.afsUserFolderTextBox.Text;
            }
        }

        //#MOD
        private void qgisFolderButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            this.folderBrowserDialog1.SelectedPath = this.qgisFolderTextBox.Text;

            DialogResult result = this.folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.qgisFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private string pathWithTrailingDirectorySeparatorChar(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                // They're always one character but EndsWith is shorter than
                // array style access to last path character. Change this
                // if performance are a (measured) issue.
                string separator1 = Path.DirectorySeparatorChar.ToString();
                string separator2 = Path.AltDirectorySeparatorChar.ToString();

                // Trailing white spaces are always ignored but folders may have
                // leading spaces. It's unusual but it may happen. If it's an issue
                // then just replace TrimEnd() with Trim(). Tnx Paul Groke to point this out.
                path = path.TrimEnd();

                // Argument is always a directory name then if there is one
                // of allowed separators then I have nothing to do.
                if (path.EndsWith(separator1) || path.EndsWith(separator2))
                    return path;

                // If there is the "alt" separator then I add a trailing one.
                // Note that URI format (file://drive:\path\filename.ext) is
                // not supported in most .NET I/O functions then we don't support it
                // here too. If you have to then simply revert this check:
                // if (path.Contains(separator1))
                //     return path + separator1;
                //
                // return path + separator2;
                if (path.Contains(separator2))
                    return path + separator2;

                // If there is not an "alt" separator I add a "normal" one.
                // It means path may be with normal one or it has not any separator
                // (for example if it's just a directory name). In this case I
                // default to normal as users expect.
                return path + separator1;
            }

            return path;

        }

        private void maxTilesPerStitchedImageTextBox_TextChanged(object sender, EventArgs e)
        {
            var numbersOnly = this.GetInteger(this.maxTilesPerStitchedImageTextBox.Text);

            if (this.maxTilesPerStitchedImageTextBox.Text != numbersOnly)
            {
                this.maxTilesPerStitchedImageTextBox.Text = numbersOnly;
            }

            int maxTiles = 0;
            if (int.TryParse(this.maxTilesPerStitchedImageTextBox.Text, out maxTiles))
            {
                int resolution = 256 * maxTiles;
                this.maxTilesPerStitchedImageInfoLabel.Text = string.Format("tiles x {0} tiles. ({1}px x {2}px)", maxTiles, resolution, resolution);
            }

        }

        private string GetInteger(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        private string GetDecimal(string input)
        {
            return new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());
        }

        private string GetSignedInteger(string input)
        {
            return new string(input.Where(c => char.IsDigit(c) || c == '-').ToArray());
        }

        private void downloadWaitTextBox_TextChanged(object sender, EventArgs e)
        {
            var numbersOnly = this.GetInteger(this.downloadWaitTextBox.Text);

            if (this.downloadWaitTextBox.Text != numbersOnly)
            {
                this.downloadWaitTextBox.Text = numbersOnly;
            }
        }

        private void downloadWaitRandomTextBox_TextChanged(object sender, EventArgs e)
        {
            var numbersOnly = this.GetInteger(this.downloadWaitRandomTextBox.Text);

            if (this.downloadWaitRandomTextBox.Text != numbersOnly)
            {
                this.downloadWaitRandomTextBox.Text = numbersOnly;
            }
        }

        private void createUSGSAccountLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ers.cr.usgs.gov/register/");
        }

        private void ShrinkTMCGridSquaresTextBox_TextChanged(object sender, EventArgs e)
        {
            var numbersOnly = this.GetDecimal(this.shrinkTMCGridSquaresTextBox.Text);

            if (this.shrinkTMCGridSquaresTextBox.Text != numbersOnly)
            {
                this.shrinkTMCGridSquaresTextBox.Text = numbersOnly;
            }
        }

        private void AddUserFolderToConfigButton_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }


        private async void UpdateImagePreview()
        {
            if (this.updateImagePreview)
            {
                if (this.imageProcessingPreviewForm != null)
                {
                    if (this.imageProcessingPreviewForm.Visible)
                    {
                        var imageProcessingSettings = new ImageProcessingSettings();
                        imageProcessingSettings.BrightnessAdjustment = this.imgProcBrightnessSlider.Value;
                        imageProcessingSettings.ContrastAdjustment = this.imgProcContrastSlider.Value;
                        imageProcessingSettings.SaturationAdjustment = this.imgProcSaturationSlider.Value;
                        imageProcessingSettings.SharpnessAdjustment = this.imgProcSharpnessSlider.Value;
                        imageProcessingSettings.RedAdjustment = this.imgProcRedSlider.Value;
                        imageProcessingSettings.GreenAdjustment = this.imgProcGreenSlider.Value;
                        imageProcessingSettings.BlueAdjustment = this.imgProcBlueSlider.Value;

                        await this.imageProcessingPreviewForm.UpdateImage(imageProcessingSettings);
                    }
                }
            }

        }


        private void ImgProcBrightnessSlider_ValueChanged(object sender, EventArgs e)
        {
            if (imgProcBrightnessTextBox.Text != imgProcBrightnessSlider.Value.ToString())
            {
                imgProcBrightnessTextBox.Text = imgProcBrightnessSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }


        private void ImgProcContrastSlider_ValueChanged(object sender, EventArgs e)
        {
            if (imgProcContrastTextBox.Text != imgProcContrastSlider.Value.ToString())
            {
                imgProcContrastTextBox.Text = imgProcContrastSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        private void ImgProcSaturationSliderValueChanged(object sender, EventArgs e)
        {
            if (imgProcSaturationTextBox.Text != imgProcSaturationSlider.Value.ToString())
            {
                imgProcSaturationTextBox.Text = imgProcSaturationSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        private void ImgProcSharpnessSlider_ValueChanged(object sender, EventArgs e)
        {
            if (imgProcSharpnessTextBox.Text != imgProcSharpnessSlider.Value.ToString())
            {
                imgProcSharpnessTextBox.Text = imgProcSharpnessSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        private void ImgProcRedSlider_ValueChanged(object sender, EventArgs e)
        {
            if (imgProcRedTextBox.Text != imgProcRedSlider.Value.ToString())
            {
                imgProcRedTextBox.Text = imgProcRedSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        private void ImgProcGreenSlider_ValueChanged(object sender, EventArgs e)
        {
            if (imgProcGreenTextBox.Text != imgProcGreenSlider.Value.ToString())
            {
                imgProcGreenTextBox.Text = imgProcGreenSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        private void ImgProcBlueSlider_ValueChanged(object sender, EventArgs e)
        {
            if (imgProcBlueTextBox.Text != imgProcBlueSlider.Value.ToString())
            {
                imgProcBlueTextBox.Text = imgProcBlueSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        private void imgProcBrightnessTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcBrightnessTextBox);
        }

        private void imgProcContrastTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcBlueTextBox);
        }

        private void imgProcSaturationTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcSaturationTextBox);        
        }

        private void imgProcSharpessTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcSharpnessTextBox);
        }

        private void imgProcRedTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcRedTextBox);
        }

        private void imgProcGreenTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcGreenTextBox);
        }

        private void imgProcBlueTextBox_Leave(object sender, EventArgs e)
        {
            this.fixLoneNegativeSign(imgProcBlueTextBox);
        }

        private void fixLoneNegativeSign(TextBox textBox)
        {
            if (textBox.Text == "-")
            {
                textBox.Text = "0";
            }
        }

        private void imgProcTextBoxTextChanged(TextBox textBox, TrackBar slider, int minValue, int maxValue)
        {
            var validatedText = this.GetSignedInteger(textBox.Text);

            if (validatedText != "-")
            {
                int intVal;

                if (int.TryParse(validatedText, out intVal))
                {
                    if (intVal < minValue)
                        intVal = minValue;

                    if (intVal > maxValue)
                        intVal = maxValue;

                    slider.Value = intVal;
                }

                if (textBox.Text != intVal.ToString())
                {
                    textBox.Text = intVal.ToString();
                }
            }
        }

        private void imgProcBrightnessTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcBrightnessTextBox, this.imgProcBrightnessSlider, -100, 100);
        }

        private void imgProcContrastTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcContrastTextBox, this.imgProcContrastSlider, -100, 100);
        }

        private void imgProcSaturationTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcSaturationTextBox, this.imgProcSaturationSlider, -100, 100);
        }

        private void imgProcSharpessTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcSharpnessTextBox, this.imgProcSharpnessSlider, 0, 10);
        }

        private void imgProcRedTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcRedTextBox, this.imgProcRedSlider, -100, 100);
        }

        private void imgProcGreenTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcGreenTextBox, this.imgProcGreenSlider, -100, 100);
        }

        private void imgProcBlueTextBox_TextChanged(object sender, EventArgs e)
        {
            this.imgProcTextBoxTextChanged(this.imgProcBlueTextBox, this.imgProcBlueSlider, -100, 100);
        }

        private void imageProcessingEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.imageProcessingEnabledCheckBox.Checked)
            {
                this.ToggleImageProcessingControlsEnabled(true);
            }
            else
            {
                this.ToggleImageProcessingControlsEnabled(false);
                //#MOD
                this.imageRemoveAlphaChannelCheckBox.Checked = false;
                //#DEVL_k #TEMP

            }
        }

        private void ShowPreviewWindowButton_Click(object sender, EventArgs e)
        {
            if (this.imageProcessingPreviewForm != null)
            {
                if (this.imageProcessingPreviewForm.IsDisposed)
                {
                    this.imageProcessingPreviewForm = null;
                }
                else
                {
                    this.imageProcessingPreviewForm.Show();
                }
            }

            if (this.imageProcessingPreviewForm == null)
            {
                this.imageProcessingPreviewForm = new ImageProcessingPreviewForm();
            }


            this.imageProcessingPreviewForm.StartPosition = FormStartPosition.Manual;
            this.imageProcessingPreviewForm.Height = this.Height;
            this.imageProcessingPreviewForm.Left = this.Right;
            this.imageProcessingPreviewForm.Top = this.Top;

            this.imageProcessingPreviewForm.Show();
            this.UpdateImagePreview();
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            this.updateImagePreview = false;
            this.imgProcBrightnessSlider.Value = 0;
            this.imgProcContrastSlider.Value = 0;
            this.imgProcSaturationSlider.Value = 0;
            this.imgProcSharpnessSlider.Value = 0;
            this.imgProcRedSlider.Value = 0;
            this.imgProcGreenSlider.Value = 0;
            this.imgProcBlueSlider.Value = 0;
            this.updateImagePreview = true;

            this.UpdateImagePreview();
        }
 
        private void LinkLabel1_Click(object sender, EventArgs e)
        {
            //#MOD
            //System.Diagnostics.Process.Start("https://www.linz.govt.nz/data/linz-data-service/guides-and-documentation/creating-an-api-key");
            System.Diagnostics.Process.Start("https://basemaps.linz.govt.nz/?i=nz-satellite-2021-2022-10m#@-41.3768088,172.9687500,z5.2493");
        }
 


        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://account.mapbox.com/auth/signup/");
        }


        //#MOD
        private void TreesDetectionDirectoryButton_Click(object sender, EventArgs e)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            this.folderBrowserDialog1.SelectedPath = this.treesDetectionFolderTextBox.Text;

            DialogResult result = this.folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.treesDetectionFolderTextBox.Text = folderBrowserDialog1.SelectedPath;

                if ((settings.TreesDetectionDirectory == "") && (this.treesDetectionFolderTextBox.Text != "")) 
                {
                    showMessageStartAppAgain = true;
                }

            }
        }

        private void TreesDetectionDensitySlider_ValueChanged(object sender, EventArgs e)
        {
            if (treesDetectionDensityTextBox.Text != treesDetectionDensitySlider.Value.ToString())
            {
                treesDetectionDensityTextBox.Text = treesDetectionDensitySlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        //#MOD
        private void TreesDetectionResetButton_Click(object sender, EventArgs e)
        {
            this.treesDetectionDensitySlider.Value = 6;
            this.treesDetectionAltitudeSlider.Value = 7;
            this.treesDetectionAltitudeCheckBox.Checked = false;

            this.UpdateImagePreview();
        }
        //#MOD
        private void LinkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://portal.opentopography.org/login");
        }
        //#MOD
        private void LinkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://platform.here.com/");
        }

        //#MOD
        private void LinkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://trac.osgeo.org/osgeo4w/");
        }

        //#MOD
        private void TreesDetectionAltitudeSlider_ValueChanged(object sender, EventArgs e)
        {
            if (treesDetectionAltitudeTextBox.Text != treesDetectionAltitudeSlider.Value.ToString())
            {
                treesDetectionAltitudeTextBox.Text = treesDetectionAltitudeSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        //#MOD
        private void AfsSceneryFolderTextBox_TextChanged(object sender, EventArgs e)
        {
            this.afsSceneryFolderTextBox.Text = pathWithTrailingDirectorySeparatorChar(this.afsSceneryFolderTextBox.Text);
            this.afsSceneryFolderTextBox.Text = this.afsSceneryFolderTextBox.Text.ToLower();
            this.afsSceneryFolderTextBox.Text = this.afsSceneryFolderTextBox.Text.Replace("aerofly_fs_2_geoconvert", "");
            this.afsSceneryFolderTextBox.Text = this.afsSceneryFolderTextBox.Text.Replace("\\\\", "\\");
        }

        //#DEVL_k
        private void WaterMaskingEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.WaterMaskingEnabledCheckBox.Checked)
            {
                this.ToggleImageWaterMaskingControlsEnabled(true);
            }
            else
            {
                this.ToggleImageWaterMaskingControlsEnabled(false);
            }
        }

        private void WaterFadeThresholdSlider_ValueChanged(object sender, EventArgs e)
        {
            if (waterFadeThresholdTextBox.Text != waterFadeThresholdSlider.Value.ToString())
            {
                waterFadeThresholdTextBox.Text = waterFadeThresholdSlider.Value.ToString();
            }

            if (waterReplaceThresholdSlider.Value < waterFadeThresholdSlider.Value)
            {
                waterReplaceThresholdSlider.Value = waterFadeThresholdSlider.Value;
                waterReplaceThresholdTextBox.Text = waterFadeThresholdSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }
        private void WaterReplaceThresholdSlider_ValueChanged(object sender, EventArgs e)
        {
            if (waterReplaceThresholdTextBox.Text != waterReplaceThresholdSlider.Value.ToString())
            {
                waterReplaceThresholdTextBox.Text = waterReplaceThresholdSlider.Value.ToString();
            }

            if (waterFadeThresholdSlider.Value > waterReplaceThresholdSlider.Value) 
            {
                waterFadeThresholdSlider.Value = waterReplaceThresholdSlider.Value;
                waterFadeThresholdTextBox.Text = waterReplaceThresholdSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }

        //#DEVL_k
        private void MovingMapElevationDataRenderingSlider_ValueChanged(object sender, EventArgs e)
        {
            if (movingMapElevationDataRenderingTextBox.Text != movingMapElevationDataRenderingSlider.Value.ToString())
            {
                movingMapElevationDataRenderingTextBox.Text = movingMapElevationDataRenderingSlider.Value.ToString();
            }

            this.UpdateImagePreview();
        }
        //#DEVL_k
        private void MovingMapElevationEnableDataBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!movingMapElevationEnableDataCheckBox.Checked)
            {
                movingMapElevationEnable3DCaptureCheckBox.Checked = false;
                movingMapElevationEnable3DCaptureCheckBox.Enabled = false;
            }
            else movingMapElevationEnable3DCaptureCheckBox.Enabled = true;

        }
    }
}
