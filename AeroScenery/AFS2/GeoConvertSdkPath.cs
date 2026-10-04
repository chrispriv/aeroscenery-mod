using System;
using System.IO;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Settings store the path to aerofly_fs_2_geoconvert.exe. Older installs stored the SDK
    /// root or the aerofly_fs_2_geoconvert folder; those are resolved here.
    /// </summary>
    public static class GeoConvertSdkPath
    {
        public const string ExeFileName = "aerofly_fs_2_geoconvert.exe";
        public const string FolderName = "aerofly_fs_2_geoconvert";

        public static string NormalizeToExe(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            string path = input.Trim().Trim('"');
            path = path.Replace('/', '\\');

            try
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }

                if (Directory.Exists(path))
                {
                    string fromDir = FindExeUnderDirectory(path);
                    if (!string.IsNullOrEmpty(fromDir))
                    {
                        return fromDir;
                    }
                }

                path = path.TrimEnd('\\');
                if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.IsPathRooted(path) ? Path.GetFullPath(path) : path;
                }

                if (!Path.IsPathRooted(path))
                {
                    return path;
                }

                string nextToFolder = Path.Combine(path, ExeFileName);
                if (File.Exists(nextToFolder))
                {
                    return Path.GetFullPath(nextToFolder);
                }

                string nested = Path.Combine(path, FolderName, ExeFileName);
                if (File.Exists(nested))
                {
                    return Path.GetFullPath(nested);
                }

                if (string.Equals(Path.GetFileName(path), FolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(Path.Combine(path, ExeFileName));
                }

                return Path.GetFullPath(Path.Combine(path, FolderName, ExeFileName));
            }
            catch (Exception)
            {
                return path;
            }
        }

        public static string GetDirectory(string stored)
        {
            string exe = NormalizeToExe(stored);
            if (string.IsNullOrEmpty(exe))
            {
                return "";
            }

            try
            {
                return Path.GetDirectoryName(exe) ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string FindExeUnderDirectory(string directory)
        {
            string direct = Path.Combine(directory, ExeFileName);
            if (File.Exists(direct))
            {
                return Path.GetFullPath(direct);
            }

            string nested = Path.Combine(directory, FolderName, ExeFileName);
            if (File.Exists(nested))
            {
                return Path.GetFullPath(nested);
            }

            return "";
        }
    }
}
