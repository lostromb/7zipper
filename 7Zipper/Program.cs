using Durandal.Common.Logger;
using Durandal.Common.Utils.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SevenZipper
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("7zipper.exe");
                Console.WriteLine("-command {compressfiles|compressdirs|extract|transcode|extractflat}");
                Console.WriteLine("-in (input path)");
                Console.WriteLine("-out (output path)");
                Console.WriteLine("-maxratio (max ratio to retain a compressed archive; default 0.9)");
                Console.WriteLine("-delete Delete completed inputs");
                Console.WriteLine("Commands:");
                Console.WriteLine("    compressfiles: ??");
                Console.WriteLine("    compressdirs: ??");
                Console.WriteLine("    extract: Take a directory containing many archives, extract those archives into individual output directories corresponding to each input archive's name");
                Console.WriteLine("    transcode: Convert archives (of any input format) to .7z archives with max compression, and only retain them if compression is superior to the original file.");
                Console.WriteLine("    extract-psx: Do step 1 of the big PSX library conversion");
                Console.WriteLine("    compress-psx: Do step 3 of the big PSX library conversion");
                return 0;
            }

            IDictionary<string, string[]> cmdLn = CommandLine.ParseCommandLineOptions(args);
            if (!cmdLn.ContainsKey("command"))
            {
                Console.WriteLine("Missing -command arg");
                return -1;
            }

            string command = cmdLn["command"][0];
            ILogger logger = new ConsoleLogger("7Zipper");

            if (string.Equals("compressfiles", command))
            {
                string inputPath;
                string outputPath;
                double maxRatio;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                if (!cmdLn.ContainsKey("maxratio"))
                {
                    maxRatio = 0.9;
                }
                else if (!double.TryParse(cmdLn["maxratio"][0], out maxRatio))
                {
                    Console.WriteLine("-maxratio arg is not a valid number format. Defaulting to 0.9");
                    maxRatio = 0.9;
                }

                bool delete = cmdLn.ContainsKey("delete");
                if (delete)
                {
                    logger.Log("DELETE mode is ON");
                }

                Actions.CompressFiles(inputPath, outputPath, maxRatio, delete, logger);
            }
            else if (string.Equals("compressdirs", command))
            {
                string inputPath;
                string outputPath;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                bool delete = cmdLn.ContainsKey("delete");
                if (delete)
                {
                    logger.Log("DELETE mode is ON");
                }

                Actions.CompressDirectories(inputPath, outputPath, delete, logger);
            }
            else if (string.Equals("extract", command))
            {
                string inputPath;
                string outputPath;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                bool delete = cmdLn.ContainsKey("delete");
                if (delete)
                {
                    logger.Log("DELETE mode is ON");
                }

                Actions.Extract(inputPath, outputPath, delete, logger);
            }
            else if (string.Equals("extract-psx", command))
            {
                string inputPath;
                string outputPath;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                Actions.ExtractPsx(inputPath, outputPath, logger);
            }
            else if (string.Equals("psx-fix-cue", command))
            {
                string inputPath;
                string outputPath;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                Actions.PsxFixCueFiles(inputPath, outputPath, logger);
            }
            else if (string.Equals("compress-psx", command))
            {
                string inputPath;
                string outputPath;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                Actions.CompressPsx(inputPath, outputPath, logger);
            }
            else if (string.Equals("transcode", command))
            {
                string inputPath;
                string outputPath;
                if (!cmdLn.ContainsKey("in"))
                {
                    Console.WriteLine("Missing -in arg");
                    return -1;
                }
                else
                {
                    inputPath = cmdLn["in"][0];
                }

                if (!cmdLn.ContainsKey("out"))
                {
                    Console.WriteLine("Missing -out arg");
                    return -1;
                }
                else
                {
                    outputPath = cmdLn["out"][0];
                }

                bool delete = cmdLn.ContainsKey("delete");
                if (delete)
                {
                    logger.Log("DELETE mode is ON");
                }

                Actions.Transcode(inputPath, outputPath, delete, logger);
            }
            else
            {
                logger.Log("Unknown command \"" + command + "\"", LogLevel.Err);
            }

            return 0;
        }
    }
}
