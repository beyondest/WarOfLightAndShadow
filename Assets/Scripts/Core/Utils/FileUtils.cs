using System;
using System.IO;

namespace SparFlame.Core
{
    public static class FileUtils
    {
        /// <summary>
        /// Copy directory recursively
        /// </summary>
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            // make sure target folder exists
            if(!Directory.Exists(sourceDir))return;
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFilePath, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectory(dir, targetSubDir);
            }
        }

        /// <summary>
        /// Delete all files and folders in a directory
        /// </summary>
        public static void ClearDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            foreach (var file in Directory.GetFiles(folderPath))
            {
                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

    }
}