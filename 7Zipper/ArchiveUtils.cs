using Durandal.Common.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipper
{
    internal class ArchiveUtils
    {
        public static CompressionResult CompressIndividualFile(DirectoryInfo binaryDirectory, FileInfo inputFile, FileInfo outputFile, ILogger logger)
        {
            logger.Log("Compressing file " + inputFile.Name + " to " + outputFile.Name + "...");

            if (outputFile.Exists)
            {
                logger.Log("Output archive " + outputFile.FullName + " already exists!", LogLevel.Wrn);
                return CompressionResult.DestFileAlreadyExists;
            }

            int resultCode = Interprocess.RunExecutableInSandbox(
                binaryDirectory.FullName + "\\7za.exe",
                "a -y -t7z -mx9 \"" + outputFile.FullName + "\" \"" + inputFile.FullName + "\"",
                logger);

            return resultCode == 0 ? CompressionResult.Success : CompressionResult.ProgramError;
        }

        public static CompressionResult CompressFiles(DirectoryInfo binaryDirectory, IEnumerable<FileInfo> inputFiles, FileInfo outputFile, ILogger logger)
        {
            logger.Log("Compressing files to " + outputFile.Name + "...");

            if (outputFile.Exists)
            {
                logger.Log("Output archive " + outputFile.FullName + " already exists!", LogLevel.Wrn);
                return CompressionResult.DestFileAlreadyExists;
            }

            // Create a list file in temp directory
            FileInfo listFile = new FileInfo(Path.GetTempFileName());
            try
            {
                File.WriteAllLines(listFile.FullName, inputFiles.Select((a) => a.FullName));

                int resultCode = Interprocess.RunExecutableInSandbox(
                    binaryDirectory.FullName + "\\7za.exe",
                    "a -y -t7z -mx9 \"" + outputFile.FullName + "\" \"@" + listFile.FullName + "\"",
                    logger);

                return resultCode == 0 ? CompressionResult.Success : CompressionResult.ProgramError;
            }
            finally
            {
                // clean up
                FileUtils.TryForceDeleteFile(listFile);
            }
        }

        public static CompressionResult CompressIndividualDirectory(DirectoryInfo binaryDirectory, DirectoryInfo inputDir, FileInfo outputFile, ILogger logger)
        {
            logger.Log("Compressing directory " + inputDir.Name + " to " + outputFile.Name + "...");

            if (outputFile.Exists)
            {
                logger.Log("Output archive " + outputFile.FullName + " already exists!", LogLevel.Wrn);
                return CompressionResult.DestFileAlreadyExists;
            }

            int resultCode = Interprocess.RunExecutableInSandbox(
                binaryDirectory.FullName + "\\7za.exe",
                "a -y -t7z -mx9 \"" + outputFile.FullName + "\" \"" + inputDir.FullName + "\\*\"",
                logger);

            return resultCode == 0 ? CompressionResult.Success : CompressionResult.ProgramError;
        }

        public static CompressionResult Extract7zFile(DirectoryInfo binaryDirectory, FileInfo inputFile, DirectoryInfo outputDirectory, ILogger logger)
        {
            logger.Log("Extracting " + inputFile.Name + " to " + outputDirectory.Name + "...");

            if (outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " already exists!", LogLevel.Wrn);
                return CompressionResult.DestFileAlreadyExists;
            }
            else
            {
                outputDirectory.Create();
            }

            int resultCode = Interprocess.RunExecutableInSandbox(
                binaryDirectory.FullName + "\\7za.exe",
                "x \"" + inputFile.FullName + "\" -o\"" + outputDirectory.FullName + "\"",
                logger);

            return resultCode == 0 ? CompressionResult.Success : CompressionResult.ProgramError;
        }

        public static CompressionResult ExtractRarFile(DirectoryInfo binaryDirectory, FileInfo inputFile, DirectoryInfo outputDirectory, ILogger logger)
        {
            logger.Log("Extracting " + inputFile.Name + " to " + outputDirectory.Name + "...");

            if (outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " already exists!", LogLevel.Wrn);
                return CompressionResult.DestFileAlreadyExists;
            }
            else
            {
                outputDirectory.Create();
            }

            int resultCode = Interprocess.RunExecutableInSandbox(
                binaryDirectory.FullName + "\\UnRAR.exe",
                "x \"" + inputFile.FullName + "\" \"" + outputDirectory.FullName + "\"",
                logger);

            return resultCode == 0 ? CompressionResult.Success : CompressionResult.ProgramError;
        }

        public static CompressionResult ExtractEcmFile(DirectoryInfo binaryDirectory, FileInfo inputFile, FileInfo outputFile, ILogger logger)
        {
            logger.Log("Extracting " + inputFile.Name + " to " + outputFile.Name + "...");

            if (outputFile.Exists)
            {
                logger.Log("Output file " + outputFile.FullName + " already exists!", LogLevel.Wrn);
                return CompressionResult.DestFileAlreadyExists;
            }

            int resultCode = Interprocess.RunExecutableInSandbox(
                string.Format("{0}\\unecm.exe", binaryDirectory.FullName),
                string.Format("\"{0}\" \"{1}\"", inputFile.FullName, outputFile.FullName),
                logger);

            return resultCode == 0 ? CompressionResult.Success : CompressionResult.ProgramError;
        }

        public static bool IsFileAnArchive(FileInfo fileName)
        {
            string extension = fileName.Extension;
            return string.Equals(".7z", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".zip", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".rar", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".gzip", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".bz2", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".cab", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".arc", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".gz", extension, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFileADiscImage(FileInfo fileName)
        {
            string extension = fileName.Extension;
            return string.Equals(".iso", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".img", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".bin", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".cue", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".ccd", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".mdf", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".mds", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".ecm", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".cdi", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".wav", extension, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(".sub", extension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
