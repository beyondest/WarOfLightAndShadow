using System;
using System.IO;

namespace SparFlame.Core
{
    public static class FileUtils
    {
        /// <summary>
        /// 递归复制文件夹内容到目标路径，并保持目录结构
        /// </summary>
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            // 确保目标文件夹存在
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            if(!Directory.Exists(sourceDir))return;
            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFilePath, overwrite: true);
            }

            // 递归复制子目录
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                CopyDirectory(dir, targetSubDir);
            }
        }

        /// <summary>
        /// 删除文件夹下的所有文件和子文件夹，但保留该文件夹本身
        /// </summary>
        public static void ClearDirectory(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            // 删除所有文件
            foreach (var file in Directory.GetFiles(folderPath))
            {
                File.Delete(file);
            }

            // 删除所有子目录
            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                Directory.Delete(dir, recursive: true);
            }
        }


        //     public static class FileHelper
        //     {
        //         /// <summary>
        //         /// 获取目录下最后修改的文件路径
        //         /// </summary>
        //         /// <param name="directoryPath">目录路径</param>
        //         /// <returns>最后修改文件的完整路径，如果目录为空则返回 null</returns>
        //         public static string GetLastModifiedFile(string directoryPath)
        //         {
        //             if (!Directory.Exists(directoryPath))
        //                 throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        //
        //             var files = Directory.GetFiles(directoryPath);
        //             if (files.Length == 0)
        //                 return null;
        //
        //             string lastFile = null;
        //             DateTime lastWrite = DateTime.MinValue;
        //
        //             foreach (var file in files)
        //             {
        //                 DateTime writeTime = File.GetLastWriteTime(file);
        //                 if (writeTime > lastWrite)
        //                 {
        //                     lastWrite = writeTime;
        //                     lastFile = file;
        //                 }
        //             }
        //
        //             return lastFile;
        //         }
        // }
    }
}