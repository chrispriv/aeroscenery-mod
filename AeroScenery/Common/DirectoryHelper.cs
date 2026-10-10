using System;
using System.IO;

namespace AeroScenery.Common
{
    public static class DirectoryHelper
    {
        public static string DefaultFs4InstallDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Aerofly FS 4",
                "addons",
                "scenery") + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Gets and checks the existence of the configured directory to install scenery into
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string FindAFSSceneryInstallDirectory(Settings settings)
        {
            string afsSceneryInstallDirectory = null;

            // Is a custom user directory configured?
            if (!String.IsNullOrEmpty(settings.AFS2UserDirectory))
            {
                string afsUserDirectoryPath = settings.AFS2UserDirectory;

                // Does the root directory exist
                if (Directory.Exists(afsUserDirectoryPath))
                {
                    // If no scenery sub-directory exists, create one
                    //string afsUserDirectorySceneryPath = afsUserDirectoryPath + @"scenery\";
                    string afsUserDirectorySceneryPath = afsUserDirectoryPath + settings.AFSSceneryFolder;

                    if (!Directory.Exists(afsUserDirectorySceneryPath))
                    {
                        Directory.CreateDirectory(afsUserDirectorySceneryPath);
                    }

                    // If no images sub-directory exists, create one
                    string afsUserDirectorySceneryImagesPath = afsUserDirectorySceneryPath + @"images\";

                    if (!Directory.Exists(afsUserDirectorySceneryImagesPath))
                    {
                        Directory.CreateDirectory(afsUserDirectorySceneryImagesPath);
                    }

                    afsSceneryInstallDirectory = afsUserDirectorySceneryImagesPath;
                }
            }
            else
            {
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string afsMyDocsSceneryPath = myDocumentsPath + @"\Aerofly FS 2\scenery\";

                // Does the My Documents AFS scenery directory exist? (It should)
                if (Directory.Exists(afsMyDocsSceneryPath))
                {
                    // There should already be an images sub-directory, but create one if not
                    string afsMyDocsSceneryImagesPath = afsMyDocsSceneryPath + @"images";

                    // If the images sub directory doesn't exist, create it
                    if (!Directory.Exists(afsMyDocsSceneryImagesPath))
                    {
                        Directory.CreateDirectory(afsMyDocsSceneryImagesPath);
                    }

                    afsSceneryInstallDirectory = afsMyDocsSceneryImagesPath;
                }
            }

            return afsSceneryInstallDirectory;
        }

        public static string GetWorkingSceneryName(Settings settings)
        {
            string sceneryName = (settings.AFSSceneryFolder ?? "myscenery").TrimEnd('\\', '/');
            if (String.IsNullOrEmpty(sceneryName))
            {
                sceneryName = "myscenery";
            }

            return sceneryName;
        }

        /// <summary>
        /// Images directory for FSG Android copy:
        /// {FsgWorkingDirectory}\fsg_scenery_{name}\fsg_scenery_{name}_images\scenery\images\
        /// The user later zips the _images folder and renames it to .tme.
        /// </summary>
        public static string GetFsgSceneryCopyDirectoryPath(Settings settings)
        {
            if (String.IsNullOrWhiteSpace(settings.FsgWorkingDirectory))
            {
                return null;
            }

            string root = settings.FsgWorkingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string sceneryName = GetWorkingSceneryName(settings);
            return Path.Combine(root, "fsg_scenery_" + sceneryName, "fsg_scenery_" + sceneryName + "_images", "scenery", "images")
                + Path.DirectorySeparatorChar;
        }

        /// <summary>Same as <see cref="GetFsgSceneryCopyDirectoryPath"/>, optionally creating the folders.</summary>
        public static string FindFsgSceneryCopyDirectory(Settings settings, bool createDirectories = true)
        {
            string imagesDir = GetFsgSceneryCopyDirectoryPath(settings);
            if (imagesDir == null)
            {
                return null;
            }

            string root = settings.FsgWorkingDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root))
            {
                if (!createDirectories)
                {
                    return null;
                }
                Directory.CreateDirectory(root);
            }

            if (createDirectories)
            {
                Directory.CreateDirectory(imagesDir);
            }
            return imagesDir;
        }
    }
}
