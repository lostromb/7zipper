using Durandal.API.Utils;
using Durandal.Common.Logger;
using Durandal.Common.Utils;
using Durandal.Common.Utils.MathExt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SevenZipper
{
    internal class Actions
    {
        public static int CompressFiles(string inputPath, string outputPath, double maxRatio, bool delete, ILogger logger)
        {
            DirectoryInfo binaryDirectory = new DirectoryInfo(".\\binaries");
            if (!binaryDirectory.Exists)
            {
                logger.Log("Binary directory " + binaryDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            logger.Log("Running batch compression of all individual files in  " + inputDirectory.FullName);
            foreach (FileInfo inputFile in inputDirectory.EnumerateFiles())
            {
                if (ArchiveUtils.IsFileAnArchive(inputFile))
                {
                    logger.Log("Input file " + inputFile.FullName + " is already an archive; skipping...", LogLevel.Wrn);
                    continue;
                }

                string outputFileName = FileUtils.ReplaceFileExtension(inputFile.Name, "7z");
                FileInfo outputFile = new FileInfo(outputDirectory.FullName + "\\" + outputFileName);
                CompressionResult compressResult = ArchiveUtils.CompressIndividualFile(binaryDirectory, inputFile, outputFile, logger);

                outputFile.Refresh();
                if (compressResult == CompressionResult.Success)
                {
                    // Evaluate the ratio of compression. If it was too low, delete the created archive
                    long inputFileSize = inputFile.Length;
                    long outputFileSize = outputFile.Length;
                    double ratio = (double)outputFileSize / (double)inputFileSize;
                    logger.Log("Compression ratio was " + ratio);

                    if (delete)
                    {
                        if (ratio > maxRatio)
                        {
                            logger.Log("Compression ratio was too low. Deleting archive " + outputFile.Name, LogLevel.Wrn);
                            outputFile.Delete();
                        }
                        else
                        {
                            logger.Log("Deleting input file " + inputFile.Name);
                            inputFile.Delete();
                        }
                    }
                }
                else if (compressResult == CompressionResult.DestFileAlreadyExists)
                {
                }
                else if (compressResult == CompressionResult.ProgramError)
                {
                    logger.Log("Compression FAILED", LogLevel.Err);
                    if (delete)
                    {
                        FileUtils.TryForceDeleteFile(outputFile);
                    }
                }
            }

            return 0;
        }

        public static int CompressDirectories(string inputPath, string outputPath, bool delete, ILogger logger)
        {
            DirectoryInfo binaryDirectory = new DirectoryInfo(".\\binaries");
            if (!binaryDirectory.Exists)
            {
                logger.Log("Binary directory " + binaryDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            logger.Log("Running batch compression of all individual directories in  " + inputDirectory.FullName);
            foreach (DirectoryInfo directoryToCompress in inputDirectory.EnumerateDirectories())
            {
                // While there is a single subdirectory with the same name at the outer directory, go 1 level deeper (on the assumption that someone else made too many wrapper folders while compressing)
                DirectoryInfo actualDirectoryToCompress = directoryToCompress;
                bool tryRecurse = true;
                while (tryRecurse)
                {
                    IList<DirectoryInfo> subDirs = actualDirectoryToCompress.EnumerateDirectories().ToList();
                    IList<FileInfo> subFiles = actualDirectoryToCompress.EnumerateFiles().ToList();
                    if (subFiles.Count == 0 &&
                        subDirs.Count == 1 &&
                        string.Equals(subDirs[0].Name, actualDirectoryToCompress.Name, StringComparison.Ordinal))
                    {
                        logger.Log("Directory has extra nested levels; recursing 1 level deeper");
                        actualDirectoryToCompress = subDirs[0];
                        tryRecurse = true;
                    }
                    else
                    {
                        tryRecurse = false;
                    }
                }

                string outputFileName = FileUtils.ReplaceFileExtension(actualDirectoryToCompress.Name, "7z");
                FileInfo outputFile = new FileInfo(outputDirectory.FullName + "\\" + outputFileName);
                CompressionResult compressResult = ArchiveUtils.CompressIndividualDirectory(binaryDirectory, actualDirectoryToCompress, outputFile, logger);

                outputFile.Refresh();
                if (compressResult == CompressionResult.Success)
                {
                    if (delete)
                    {
                        logger.Log("Deleting input directory " + directoryToCompress.Name);
                        directoryToCompress.Delete(true);
                    }
                }
                else if (compressResult == CompressionResult.DestFileAlreadyExists)
                {
                }
                else if (compressResult == CompressionResult.ProgramError)
                {
                    logger.Log("Compression FAILED", LogLevel.Err);
                    if (delete)
                    {
                        FileUtils.TryForceDeleteFile(outputFile);
                    }
                }
            }

            return 0;
        }

        public static int Extract(string inputPath, string outputPath, bool delete, ILogger logger)
        {
            DirectoryInfo binaryDirectory = new DirectoryInfo(".\\binaries");
            if (!binaryDirectory.Exists)
            {
                logger.Log("Binary directory " + binaryDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            logger.Log("Running batch extraction of all individual files in  " + inputDirectory.FullName);
            foreach (FileInfo inputFile in inputDirectory.EnumerateFiles())
            {
                if (!ArchiveUtils.IsFileAnArchive(inputFile))
                {
                    logger.Log("Input file " + inputFile.FullName + " is not an archive; skipping...", LogLevel.Wrn);
                    continue;
                }

                string extractionDirectoryName = FileUtils.ReplaceFileExtension(inputFile.Name, null);
                DirectoryInfo extractionDirectory = new DirectoryInfo(outputDirectory.FullName + "\\" + extractionDirectoryName);

                if (extractionDirectory.Exists)
                {
                    logger.Log("Output directory " + extractionDirectory.FullName + " already exists; skipping...", LogLevel.Wrn);
                    continue;
                }

                CompressionResult extractionResult;
                if (inputFile.Extension.Equals(".rar", StringComparison.OrdinalIgnoreCase))
                {
                    extractionResult = ArchiveUtils.ExtractRarFile(binaryDirectory, inputFile, extractionDirectory, logger);
                }
                else
                {
                    extractionResult = ArchiveUtils.Extract7zFile(binaryDirectory, inputFile, extractionDirectory, logger);
                }

                if (extractionResult == CompressionResult.Success)
                {
                    if (delete)
                    {
                        logger.Log("Deleting input file " + inputFile.Name);
                        inputFile.Delete();
                    }
                }
                else if (extractionResult == CompressionResult.DestFileAlreadyExists)
                {
                }
                else if (extractionResult == CompressionResult.ProgramError)
                {
                    logger.Log("Extraction FAILED", LogLevel.Err);
                }
            }

            return 0;
        }

        public static int ExtractPsx(string inputPath, string outputPath, ILogger logger)
        {
            DirectoryInfo binaryDirectory = new DirectoryInfo(".\\binaries");
            if (!binaryDirectory.Exists)
            {
                logger.Log("Binary directory " + binaryDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo tempDir = new DirectoryInfo(Environment.CurrentDirectory + "\\Temp");
            if (tempDir.Exists)
            {
                tempDir.Delete(true);
            }

            logger.Log("Temp directory is " + tempDir.FullName, LogLevel.Std);
            logger.Log("Running batch extraction of all individual files in  " + inputDirectory.FullName + " and flattening output");
            foreach (FileInfo inputFile in inputDirectory.EnumerateFiles())
            {
                if (!ArchiveUtils.IsFileAnArchive(inputFile))
                {
                    logger.Log("Input file " + inputFile.FullName + " is not an archive; skipping...", LogLevel.Wrn);
                    continue;
                }

                string gameName = FileUtils.ReplaceFileExtension(inputFile.Name, null);
                CompressionResult extractionResult;
                if (inputFile.Extension.Equals(".rar", StringComparison.OrdinalIgnoreCase))
                {
                    extractionResult = ArchiveUtils.ExtractRarFile(binaryDirectory, inputFile, tempDir, logger);
                }
                else
                {
                    extractionResult = ArchiveUtils.Extract7zFile(binaryDirectory, inputFile, tempDir, logger);
                }

                if (extractionResult == CompressionResult.Success)
                {
                }
                else if (extractionResult == CompressionResult.DestFileAlreadyExists)
                {
                    logger.Log("Archive extraction failed: Destination already exists", LogLevel.Err);
                    return -1;
                }
                else if (extractionResult == CompressionResult.ProgramError)
                {
                    logger.Log("Archive extraction FAILED", LogLevel.Err);
                    //return -1;
                }

                // Extract ecm files in the output directory
                foreach (FileInfo extractedFile in tempDir.EnumerateFiles("*.ecm", SearchOption.AllDirectories))
                {
                    FileInfo unEcmedFile = new FileInfo(extractedFile.Directory.FullName + "\\" + FileUtils.ReplaceFileExtension(extractedFile.Name, null));
                    extractionResult = ArchiveUtils.ExtractEcmFile(binaryDirectory, extractedFile, unEcmedFile, logger);

                    if (extractionResult == CompressionResult.Success)
                    {
                    }
                    else if (extractionResult == CompressionResult.DestFileAlreadyExists)
                    {
                    }
                    else if (extractionResult == CompressionResult.ProgramError)
                    {
                        logger.Log("ECM extraction FAILED", LogLevel.Err);
                        return -1;
                    }

                    // Delete original .ecm
                    FileUtils.TryForceDeleteFile(extractedFile);
                }

                // Make a count of how many of each file type we have in the output. This is to detect if there were multiple discs in the original archive
                Counter<string> fileTypeCounter = new Counter<string>();

                // Since .bin files are almost always keyed off the name of their corresponding .cue, also capture that file name for later use
                string cueFileName = null;
                string binFileName = null;
                foreach (FileInfo extractedFile in tempDir.EnumerateFiles("*.*", SearchOption.AllDirectories))
                {
                    fileTypeCounter.Increment(extractedFile.Extension.ToLowerInvariant());
                    if (string.Equals(".cue", extractedFile.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        cueFileName = FileUtils.ReplaceFileExtension(extractedFile.Name, null);
                    }
                    else if (string.Equals(".bin", extractedFile.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        binFileName = FileUtils.ReplaceFileExtension(extractedFile.Name, null);
                    }
                }

                if (fileTypeCounter.GetCount(".cue") > 1 ||
                    fileTypeCounter.GetCount(".iso") > 1 ||
                    fileTypeCounter.GetCount(".mdf") > 1 ||
                    fileTypeCounter.GetCount(".ccd") > 1 ||
                    fileTypeCounter.GetCount(".img") > 1)
                {
                    logger.Log("Processing failed: Found multiple iso files in one archive and don't know how to handle", LogLevel.Err);
                    return -1;
                }

                // Detect if there is a .bin with no corresponding cue. If so, generate a boilerplate one
                if (cueFileName == null &&
                    binFileName != null)
                {
                    logger.Log("Found .bin file without corresponding .cue, generating default cuesheet for " + gameName + ". Please check redump later for correctness.", LogLevel.Wrn);
                    string[] cuesheet = new string[]
                    {
                        "FILE \"" + binFileName + ".bin\" BINARY",
                        "  TRACK 01 MODE2/2352",
                        "    INDEX 01 00:00:00"
                    };

                    File.WriteAllLines(tempDir.FullName + "\\" + binFileName + ".cue", cuesheet);
                    cueFileName = binFileName;
                }

                // catch mismatches between bin and cue file name
                if (cueFileName != null && binFileName == null)
                {
                    binFileName = cueFileName;
                }

                // Reprocess all output files based on the desired file name (the game name)
                foreach (FileInfo extractedFile in tempDir.EnumerateFiles("*.*", SearchOption.AllDirectories))
                {
                    if (ArchiveUtils.IsFileADiscImage(extractedFile))
                    {
                        if (string.Equals(".bin", extractedFile.Extension, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(".wav", extractedFile.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            // Rename bin files while also taking special care to preserve track numbers
                            string newFileName = extractedFile.Directory.FullName + "\\" + extractedFile.Name.Replace(binFileName, gameName);
                            extractedFile.MoveTo(newFileName);
                        }
                        else if (string.Equals(".cue", extractedFile.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            // Also process links in the cue file to match the renamed .bins, while also renaming the .cue itself
                            string[] cuesheet = File.ReadAllLines(extractedFile.FullName);
                            for (int c = 0; c < cuesheet.Length; c++)
                            {
                                cuesheet[c] = cuesheet[c].Replace(binFileName, gameName);
                            }

                            extractedFile.MoveTo(extractedFile.Directory.FullName + "\\" + gameName + ".cue.old");
                            FileUtils.TryForceDeleteFile(extractedFile);
                            File.WriteAllLines(extractedFile.Directory.FullName + "\\" + gameName + ".cue", cuesheet);
                        }
                        else
                        {
                            // All other image files we can just rename uniformly
                            // TODO preserve multi-disc naming here?
                            string newFileName = extractedFile.Directory.FullName + "\\" + gameName + extractedFile.Extension;
                            extractedFile.MoveTo(newFileName);
                        }
                    }
                    else
                    {
                        logger.Log("Deleting non ISO file " + extractedFile.Name, LogLevel.Wrn);
                        FileUtils.TryForceDeleteFile(extractedFile);
                    }
                }

                // Take all files from temp dir and copy them to output
                foreach (FileInfo extractedFile in tempDir.EnumerateFiles("*.*", SearchOption.AllDirectories))
                {
                    FileInfo targetFile = new FileInfo(outputDirectory.FullName + "\\" + extractedFile.Name);
                    if (targetFile.Exists)
                    {
                        logger.Log("The target file " + targetFile.FullName + " already exists!", LogLevel.Wrn);
                    }
                    else
                    {
                        extractedFile.MoveTo(targetFile.FullName);
                    }
                }

                // Clean temp dir for next staging
                tempDir.Delete(true);
            }

            return 0;
        }

        public static int PsxFixCueFiles(string inputPath, string outputPath, ILogger logger)
        {
            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            logger.Log("Running CUE fix for all .cue files in  " + inputDirectory.FullName);

            Regex replacer = new Regex("\".+?.BIN\"", RegexOptions.IgnoreCase);
            foreach (FileInfo cueFile in inputDirectory.EnumerateFiles("*.cue"))
            {
                logger.Log("Fixing " + cueFile.FullName);
                string binFileName = FileUtils.ReplaceFileExtension(cueFile.Name, "bin");
                string[] cuesheet = File.ReadAllLines(cueFile.FullName);
                for (int c = 0; c < cuesheet.Length; c++)
                {
                    cuesheet[c] = DurandalUtils.RegexReplace(replacer, cuesheet[c], "\"" + binFileName + "\"");
                }

                File.WriteAllLines(outputDirectory.FullName + "\\" + cueFile.Name, cuesheet);
            }

            return 0;
        }

        public static int CompressPsx(string inputPath, string outputPath, ILogger logger)
        {
            DirectoryInfo binaryDirectory = new DirectoryInfo(".\\binaries");
            if (!binaryDirectory.Exists)
            {
                logger.Log("Binary directory " + binaryDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            logger.Log("Running batch compression of all PSX discs in  " + inputDirectory.FullName);
            IDictionary<string, List<FileInfo>> gameNameMappings = new Dictionary<string, List<FileInfo>>();

            Regex gameNameMatcher = new Regex("^(.+?) [\\.\\[\\(]");
            Regex discNameMatcher = new Regex(" \\(Disc \\d of \\d\\)", RegexOptions.IgnoreCase);
            foreach (FileInfo sourceFile in inputDirectory.EnumerateFiles())
            {
                string gameName = DurandalUtils.RegexRip(gameNameMatcher, sourceFile.Name, 1);
                if (string.IsNullOrEmpty(gameName))
                {
                    logger.Log("Can't parse game name " + sourceFile.Name, LogLevel.Err);
                    continue;
                }

                if (!gameNameMappings.ContainsKey(gameName))
                {
                    gameNameMappings.Add(gameName, new List<FileInfo>());
                }

                gameNameMappings[gameName].Add(sourceFile);
            }

            foreach (KeyValuePair<string, List<FileInfo>> gameMapping in gameNameMappings)
            {
                gameMapping.Value.Sort(new FileComparer());
                string archiveName = gameMapping.Value[0].Name;
                archiveName = DurandalUtils.RegexRemove(discNameMatcher, archiveName);
                archiveName = FileUtils.ReplaceFileExtension(archiveName, "7z");
                logger.Log("Compressing " + archiveName + " with input files " + string.Join(",", gameMapping.Value));
                FileInfo outputFile = new FileInfo(outputDirectory.FullName + "\\" + archiveName);
                CompressionResult compressResult = ArchiveUtils.CompressFiles(binaryDirectory, gameMapping.Value, outputFile, logger);

                outputFile.Refresh();
                if (compressResult == CompressionResult.Success)
                {
                }
                else if (compressResult == CompressionResult.DestFileAlreadyExists)
                {
                }
                else if (compressResult == CompressionResult.ProgramError)
                {
                    logger.Log("Compression FAILED", LogLevel.Err);
                }
            }

            return 0;
        }

        private class FileComparer : IComparer<FileInfo>
        {
            public int Compare(FileInfo x, FileInfo y)
            {
                return x.FullName.CompareTo(y.FullName);
            }
        }

        public static int Transcode(string inputPath, string outputPath, bool delete, ILogger logger)
        {
            DirectoryInfo binaryDirectory = new DirectoryInfo(".\\binaries");
            if (!binaryDirectory.Exists)
            {
                logger.Log("Binary directory " + binaryDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo inputDirectory = new DirectoryInfo(inputPath);
            if (!inputDirectory.Exists)
            {
                logger.Log("Input directory " + inputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            DirectoryInfo outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists)
            {
                logger.Log("Output directory " + outputDirectory.FullName + " does not exist!", LogLevel.Err);
                return -1;
            }

            logger.Log("Running transcoding of all individual archives in  " + inputDirectory.FullName);
            foreach (FileInfo inputArchiveFile in inputDirectory.EnumerateFiles())
            {
                // Calculate various filenames
                string extractionDirectoryName = Guid.NewGuid().ToString("N");
                string originalArchiveNameWithoutExtension = FileUtils.ReplaceFileExtension(inputArchiveFile.Name, null);
                DirectoryInfo tempDirectory = new DirectoryInfo(outputDirectory.FullName + "\\" + extractionDirectoryName);

                string trascodedArchiveFileName = FileUtils.ReplaceFileExtension(inputArchiveFile.Name, "7z");
                FileInfo transcodedArchiveFile = new FileInfo(outputDirectory.FullName + "\\" + trascodedArchiveFileName);

                if (!ArchiveUtils.IsFileAnArchive(inputArchiveFile))
                {
                    logger.Log("Input file " + inputArchiveFile.FullName + " is not an archive; skipping...", LogLevel.Wrn);
                    continue;
                }

                if (transcodedArchiveFile.Exists)
                {
                    logger.Log("Output files " + transcodedArchiveFile.FullName + " already exists; skipping...", LogLevel.Wrn);
                    continue;
                }

                // Extract to temp directory
                CompressionResult extractionResult;
                if (inputArchiveFile.Extension.Equals(".rar", StringComparison.OrdinalIgnoreCase))
                {
                    extractionResult = ArchiveUtils.ExtractRarFile(binaryDirectory, inputArchiveFile, tempDirectory, logger);
                }
                else
                {
                    extractionResult = ArchiveUtils.Extract7zFile(binaryDirectory, inputArchiveFile, tempDirectory, logger);
                }

                if (extractionResult == CompressionResult.ProgramError)
                {
                    logger.Log("Compression FAILED", LogLevel.Err);
                    if (delete)
                    {
                        FileUtils.TryForceDeleteDirectory(tempDirectory);
                    }
                }

                // Now compress that temp directory into a 7z
                // While there is a single subdirectory with the same name at the outer directory, go 1 level deeper (on the assumption that someone else made too many wrapper folders while compressing)
                DirectoryInfo actualDirectoryToCompress = tempDirectory;
                bool tryRecurse = true;
                while (tryRecurse)
                {
                    IList<DirectoryInfo> subDirs = actualDirectoryToCompress.EnumerateDirectories().ToList();
                    IList<FileInfo> subFiles = actualDirectoryToCompress.EnumerateFiles().ToList();
                    if (subFiles.Count == 0 &&
                        subDirs.Count == 1 &&
                        string.Equals(subDirs[0].Name, originalArchiveNameWithoutExtension, StringComparison.Ordinal))
                    {
                        logger.Log("Directory has extra nested levels; recursing 1 level deeper");
                        actualDirectoryToCompress = subDirs[0];
                        tryRecurse = true;
                    }
                    else
                    {
                        tryRecurse = false;
                    }
                }

                CompressionResult compressResult = ArchiveUtils.CompressIndividualDirectory(binaryDirectory, actualDirectoryToCompress, transcodedArchiveFile, logger);
                if (compressResult == CompressionResult.Success)
                {
                    logger.Log("Deleting temp directory " + tempDirectory.Name);
                    tempDirectory.Delete(true);

                    if (delete)
                    {
                        logger.Log("Deleting input file " + inputArchiveFile.Name);
                        inputArchiveFile.Delete();
                    }
                }
                else if (compressResult == CompressionResult.ProgramError)
                {
                    logger.Log("Compression FAILED", LogLevel.Err);
                    if (delete)
                    {
                        FileUtils.TryForceDeleteFile(transcodedArchiveFile);
                    }
                }
            }

            return 0;
        }
    }
}
