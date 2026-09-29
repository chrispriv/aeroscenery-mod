using AeroScenery.AFS2;
using AeroScenery.Common;
using AeroScenery.Controls;
using AeroScenery.OrthoPhotoSources;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AeroScenery.FileManagement
{
    public class SceneryInstaller
    {
        private readonly ILog log = LogManager.GetLogger("AeroScenery");
        private AFS2Grid afsGrid;

        //#MOD_k
        public SceneryInstaller()
        {
            this.afsGrid = new AFS2Grid();
        }
        /// <summary>
        /// Checks that there is somewhere to install to, and asks the user to confirm unless the
        /// caller has already taken that as read.
        /// </summary>
        public DialogResult ConfirmSceneryInstallation(AFS2GridSquare afs2GridSquare, bool promptUser)
        {
            var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + afs2GridSquare.Name;

            DialogResult result = DialogResult.No;

            // Does this grid square exist
            if (Directory.Exists(gridSquareDirectory))
            {
                // Do we have an Aerofly folder to install into?
                string afsSceneryInstallDirectory = DirectoryHelper.FindAFSSceneryInstallDirectory(AeroSceneryManager.Instance.Settings);

                if (afsSceneryInstallDirectory != null)
                {
                    if (!promptUser)
                    {
                        return DialogResult.Yes;
                    }

                    // Confirm that the user does want to install scenery
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("Are you sure you want to install the selected scenery for this grid square?");
                    sb.AppendLine("Any existing files in the same destination folder will be overwritten.");
                    sb.AppendLine("");
                    sb.AppendLine(String.Format("Destination: {0}", afsSceneryInstallDirectory));

                    var messageBox = new CustomMessageBox(sb.ToString(),
                        "AeroScenery",
                        MessageBoxIcon.Question);

                    messageBox.SetButtons(
                        new string[] { "Yes", "No" },
                        new DialogResult[] { DialogResult.Yes, DialogResult.No });

                    result = messageBox.ShowDialog();
                }
                else
                {
                    log.Error("Could not find a location to install scenery to.");

                    if (promptUser)
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.AppendLine("Could not find a location to install to.");
                        sb.AppendLine("");
                        sb.AppendLine("AeroScenery looks for 'Aerofly FS 4' and then 'Aerofly FS 2' in your Documents folder.");
                        sb.AppendLine("If your Aerofly user folder is somewhere else, set it as the AFS User Folder in Settings.");

                        var messageBox = new CustomMessageBox(sb.ToString(),
                            "AeroScenery",
                            MessageBoxIcon.Error);

                        result = messageBox.ShowDialog();
                    }
                }
            }
            else
            {

            }

            return result;
        }

        /*
        public DialogResult ConfirmSceneryInstallation(AFS2GridSquare afs2GridSquare)
        {
            var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + afs2GridSquare.Name;

            DialogResult result = DialogResult.No;

            // Does this grid square exist
            if (Directory.Exists(gridSquareDirectory))
            {
                // Do we have an Aerofly folder to install into?
                string afsSceneryInstallDirectory = DirectoryHelper.FindAFSSceneryInstallDirectory(AeroSceneryManager.Instance.Settings);

                if (afsSceneryInstallDirectory != null)
                {
                    // Confirm that the user does want to install scenery
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("Are you sure you want to install all scenery for this grid square?");
                    sb.AppendLine("Any existing files in the same destination folder will be overwritten.");
                    sb.AppendLine("");
                    sb.AppendLine(String.Format("Destination: {0}", afsSceneryInstallDirectory));

                    var messageBox = new CustomMessageBox(sb.ToString(),
                        "AeroScenery",
                        MessageBoxIcon.Question);

                    messageBox.SetButtons(
                        new string[] { "Yes", "No" },
                        new DialogResult[] { DialogResult.Yes, DialogResult.No });

                    result = messageBox.ShowDialog();
                }
                else
                {
                    // Can't find anywhere to install
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("Could not find a location to install to.");
                    sb.AppendLine("Either Aerofly FS2 is not installed or your 'My Documents\\Aerofly FS 2' folder has been removed.");

                    var messageBox = new CustomMessageBox(sb.ToString(),
                        "AeroScenery",
                        MessageBoxIcon.Error);

                    result = messageBox.ShowDialog();
                }
            }
            else
            {

            }

            return result;
        }
        */

        public DialogResult? CheckForDuplicateTTCFiles(AFS2GridSquare afs2GridSquare, bool promptUser, out List<string> ttcFiles)
        {
            // A null dialog result means that there are no duplicates, or the unattended
            // install continues with the filtered list.
            DialogResult? result = null;

            ttcFiles = this.CollectTtcFilesForCurrentImageSource(afs2GridSquare);

            List<string> ttcFileNames = new List<string>();

            foreach (var ttcFile in ttcFiles)
            {
                ttcFileNames.Add(Path.GetFileName(ttcFile));
            }

            if (ttcFileNames.Count != ttcFileNames.Distinct().Count())
            {
                log.WarnFormat("Duplicate ttc file names remain for grid square {0} after filtering to the current image source.", afs2GridSquare.Name);

                if (!promptUser)
                {
                    return null;
                }

                StringBuilder sbDuplicates = new StringBuilder();

                sbDuplicates.AppendLine(String.Format("Duplicate ttc files were found in the folder for grid square ({0})", afs2GridSquare.Name));
                sbDuplicates.AppendLine("This may be because you have downloaded this grid square with multiple map image providers.");
                sbDuplicates.AppendLine("If you continue with the install you may get a mismatched set of ttc files.");

                var duplicatesMessageBox = new CustomMessageBox(sbDuplicates.ToString(),
                    "AeroScenery",
                    MessageBoxIcon.Warning);

                duplicatesMessageBox.SetButtons(
                    new string[] { "Continue", "Cancel" },
                    new DialogResult[] { DialogResult.OK, DialogResult.Cancel });

                result = duplicatesMessageBox.ShowDialog();
            }

            return result;
        }

        public DialogResult ConfirmFsgSceneryCopy(AFS2GridSquare afs2GridSquare, bool promptUser)
        {
            var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + afs2GridSquare.Name;
            DialogResult result = DialogResult.No;

            if (!Directory.Exists(gridSquareDirectory))
            {
                return result;
            }

            string fsgDirectory = DirectoryHelper.FindFsgSceneryCopyDirectory(AeroSceneryManager.Instance.Settings, false);
            if (fsgDirectory == null)
            {
                log.Error("Could not find a location to copy FSG scenery to.");
                if (promptUser)
                {
                    var messageBox = new CustomMessageBox(
                        "Could not find a location to copy FSG scenery to.\nSet the FSG Scenery Working Folder in Settings.",
                        "AeroScenery",
                        MessageBoxIcon.Error);
                    result = messageBox.ShowDialog();
                }
                return result;
            }

            if (!promptUser)
            {
                return DialogResult.Yes;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Copy FSG Android scenery for this grid square into the FSG Scenery Working Folder?");
            sb.AppendLine("Any existing files in the same destination folder will be overwritten.");
            sb.AppendLine("");
            sb.AppendLine(String.Format("Destination: {0}", fsgDirectory));

            var confirmBox = new CustomMessageBox(sb.ToString(), "AeroScenery", MessageBoxIcon.Question);
            confirmBox.SetButtons(
                new string[] { "Yes", "No" },
                new DialogResult[] { DialogResult.Yes, DialogResult.No });
            return confirmBox.ShowDialog();
        }

        public DialogResult? CheckForDuplicateMobileTtcFiles(AFS2GridSquare afs2GridSquare, bool promptUser, out List<string> ttcFiles)
        {
            DialogResult? result = null;
            ttcFiles = this.CollectMobileTtcFilesForCurrentImageSource(afs2GridSquare);

            List<string> ttcFileNames = new List<string>();
            foreach (var ttcFile in ttcFiles)
            {
                ttcFileNames.Add(Path.GetFileName(ttcFile));
            }

            if (ttcFileNames.Count != ttcFileNames.Distinct().Count())
            {
                log.WarnFormat("Duplicate mobile ttc file names for grid square {0}.", afs2GridSquare.Name);
                if (!promptUser)
                {
                    return null;
                }

                var duplicatesMessageBox = new CustomMessageBox(
                    String.Format("Duplicate mobile ttc files were found for grid square ({0}).", afs2GridSquare.Name),
                    "AeroScenery",
                    MessageBoxIcon.Warning);
                duplicatesMessageBox.SetButtons(
                    new string[] { "Continue", "Cancel" },
                    new DialogResult[] { DialogResult.OK, DialogResult.Cancel });
                result = duplicatesMessageBox.ShowDialog();
            }

            return result;
        }

        /// <summary>
        /// Collects desktop GeoConvert .ttc files for the image source selected on the main window.
        /// Mobile conversion folders (*-ttc-mobile) are skipped; those go to the FSG copy action.
        /// </summary>
        private List<string> CollectTtcFilesForCurrentImageSource(AFS2GridSquare afs2GridSquare)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            var gridSquareDirectory = settings.WorkingDirectory + afs2GridSquare.Name;
            var sourceFolderName = OrthophotoSourceDirectoryName.GetDirectoryName(settings.OrthophotoSource.Value);
            var sourceDirectory = Path.Combine(gridSquareDirectory, sourceFolderName);

            if (!Directory.Exists(sourceDirectory))
            {
                log.WarnFormat("No working folder for image source {0} ({1}) in grid square {2}.",
                    settings.OrthophotoSource, sourceFolderName, afs2GridSquare.Name);
                return new List<string>();
            }

            var ttcFiles = this.EnumerateFilesRecursive(sourceDirectory, "*.ttc").ToList();
            log.InfoFormat("Install will copy {0} ttc file(s) from {1} for grid square {2}.",
                ttcFiles.Count, sourceDirectory, afs2GridSquare.Name);
            return ttcFiles;
        }

        private List<string> CollectMobileTtcFilesForCurrentImageSource(AFS2GridSquare afs2GridSquare)
        {
            var settings = AeroSceneryManager.Instance.Settings;
            var gridSquareDirectory = settings.WorkingDirectory + afs2GridSquare.Name;
            var sourceFolderName = OrthophotoSourceDirectoryName.GetDirectoryName(settings.OrthophotoSource.Value);
            var sourceDirectory = Path.Combine(gridSquareDirectory, sourceFolderName);
            var collected = new List<string>();

            if (!Directory.Exists(sourceDirectory))
            {
                log.WarnFormat("No working folder for image source {0} ({1}) in grid square {2}.",
                    settings.OrthophotoSource, sourceFolderName, afs2GridSquare.Name);
                return collected;
            }

            string[] mobileFolders;
            try
            {
                mobileFolders = Directory.GetDirectories(sourceDirectory, "*ttc-mobile", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                log.Warn("Could not enumerate mobile ttc folders.", ex);
                return collected;
            }

            foreach (var folder in mobileFolders)
            {
                if (!IsMobileTtcDirectory(folder))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(folder, "*.ttc"))
                {
                    if (IsMaskTtcFile(file))
                    {
                        continue;
                    }
                    collected.Add(file);
                }
            }

            log.InfoFormat("FSG copy will use {0} mobile ttc file(s) from {1} for grid square {2}.",
                collected.Count, sourceDirectory, afs2GridSquare.Name);
            return collected;
        }

        public async Task CopySceneryToFsgWorkingFolderAsync(AFS2GridSquare afs2GridSquare, List<string> ttcFiles)
        {
            var task = Task.Run(() =>
            {
                string imagesDirectory = DirectoryHelper.FindFsgSceneryCopyDirectory(AeroSceneryManager.Instance.Settings);
                if (imagesDirectory == null)
                {
                    return;
                }

                var level9GridSquare = afs2GridSquare;
                if (afs2GridSquare.Level != 9)
                {
                    level9GridSquare = this.afsGrid.GetGridSquareAtLatLon(afs2GridSquare.GetCenter().Lat, afs2GridSquare.GetCenter().Lng, 9);
                }

                var destDirectory = Path.Combine(imagesDirectory, level9GridSquare.Name);
                Directory.CreateDirectory(destDirectory);

                foreach (var ttcFilePath in ttcFiles)
                {
                    var destinationPath = Path.Combine(destDirectory, Path.GetFileName(ttcFilePath));
                    File.Copy(ttcFilePath, destinationPath, true);
                }
            });

            await task;
        }

        public async Task InstallSceneryAsync(AFS2GridSquare afs2GridSquare, List<string> ttcFiles)
        {
            var task = Task.Run(() =>
            {
                var gridSquareDirectory = AeroSceneryManager.Instance.Settings.WorkingDirectory + afs2GridSquare.Name;

                // Does this grid square exist
                if (Directory.Exists(gridSquareDirectory))
                {
                    // Do we have an Aerofly folder to install into?
                    string afsSceneryInstallDirectory = DirectoryHelper.FindAFSSceneryInstallDirectory(AeroSceneryManager.Instance.Settings);

                    if (afsSceneryInstallDirectory != null)
                    {
                        // We install ttc files into a folder of the level 9 grid square containing the selected grid square
                        var level9GridSquare = afs2GridSquare;

                        if (afs2GridSquare.Level != 9)
                        {
                            level9GridSquare = this.afsGrid.GetGridSquareAtLatLon(afs2GridSquare.GetCenter().Lat, afs2GridSquare.GetCenter().Lng, 9);
                        }

                        // This is now the level9 grid square that contains the selected grid square
                        var afsSceneryFinalInstallDirectory = String.Format(@"{0}\{1}", afsSceneryInstallDirectory, level9GridSquare.Name);

                        if (!Directory.Exists(afsSceneryFinalInstallDirectory))
                        {
                            Directory.CreateDirectory(afsSceneryFinalInstallDirectory);
                        }

                        // Copy the files over
                        foreach(var ttcFilePath in ttcFiles)
                        {
                            var filename = Path.GetFileName(ttcFilePath);
                            var destinationPath = String.Format(@"{0}/{1}", afsSceneryFinalInstallDirectory, filename);

                            // We want to overwrite files so that users can install updated files again
                            if (File.Exists(destinationPath))
                            {
                                File.Delete(destinationPath);
                            }
                            File.Copy(ttcFilePath, destinationPath);
                        }

                    }

                }

            });

            await task;
        }

        /// <summary>
        /// Recursively enumerates files. Silently fails if it doesn't have access to any files.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private IEnumerable<string> EnumerateFilesRecursive(string root, string pattern = "*")
        {
            var todo = new Queue<string>();
            todo.Enqueue(root);
            while (todo.Count > 0)
            {
                string dir = todo.Dequeue();
                string[] subdirs = new string[0];
                string[] files = new string[0];
                try
                {
                    subdirs = Directory.GetDirectories(dir);
                    files = Directory.GetFiles(dir, pattern);
                }
                catch (IOException)
                {
                }
                catch (System.UnauthorizedAccessException)
                {
                }

                foreach (string subdir in subdirs)
                {
                    if (IsMobileTtcDirectory(subdir))
                    {
                        continue;
                    }

                    todo.Enqueue(subdir);
                }
                foreach (string filename in files)
                {
                    yield return filename;
                }
            }
        }

        private static bool IsMaskTtcFile(string filePath)
        {
            var name = Path.GetFileName(filePath);
            return name.EndsWith("_mask.ttc", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMobileTtcDirectory(string directoryPath)
        {
            var name = Path.GetFileName(directoryPath);
            return name.EndsWith("-ttc-mobile", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("ttc-mobile", StringComparison.OrdinalIgnoreCase) >= 0;
        }


    }
}
