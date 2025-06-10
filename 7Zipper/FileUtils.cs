using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipper
{
    internal class FileUtils
    {
        public static void TryForceDeleteFile(FileInfo file)
        {
            try
            {
                file.Refresh();
                if (file.Exists)
                {
                    file.Delete();
                }
            }
            catch (Exception) { }
        }

        public static void TryForceDeleteDirectory(DirectoryInfo dir)
        {
            try
            {
                dir.Refresh();
                if (dir.Exists)
                {
                    dir.Delete(true);
                }
            }
            catch (Exception) { }
        }

        public static string ReplaceFileExtension(string fileName, string newExtension)
        {
            int periodIdx = fileName.LastIndexOf('.');
            if (periodIdx < 0)
            {
                return fileName + "." + newExtension;
            }
            else
            {
                if (string.IsNullOrEmpty(newExtension))
                {
                    // Remove extension
                    return fileName.Substring(0, periodIdx);
                }
                else
                {
                    return fileName.Substring(0, periodIdx + 1) + newExtension;
                }
            }
        }
    }
}
