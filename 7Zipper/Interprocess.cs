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
    internal class Interprocess
    {
        public static int RunExecutableInSandbox(string exePath, string arguments, ILogger logger)
        {
            try
            {
                BufferedStream encoderStdin;
                ThreadedStreamReader encoderStdout;
                ThreadedStreamReader encoderStderr;
                Process encoderProcess;

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                encoderProcess = Process.Start(processInfo);
                try
                {
                    encoderStdout = new ThreadedStreamReader(encoderProcess.StandardOutput.BaseStream, 128, 131072);
                    encoderStderr = new ThreadedStreamReader(encoderProcess.StandardError.BaseStream, 128, 131072);
                    encoderStdin = new BufferedStream(encoderProcess.StandardInput.BaseStream, 131072);

                    byte[] outputBuffer = new byte[1024];
                    while (!encoderProcess.HasExited)
                    {
                        int outputBytesRead = encoderStdout.Read(outputBuffer, 0, outputBuffer.Length);
                        if (outputBytesRead > 0)
                        {
                            string message = Encoding.ASCII.GetString(outputBuffer, 0, outputBytesRead);
                            logger.Log(message, LogLevel.Vrb);
                        }

                        outputBytesRead = encoderStderr.Read(outputBuffer, 0, outputBuffer.Length);
                        if (outputBytesRead > 0)
                        {
                            string message = Encoding.ASCII.GetString(outputBuffer, 0, outputBytesRead);
                            logger.Log(message, LogLevel.Vrb);
                        }

                        Thread.Sleep(100);
                    }

                    while (!encoderStdout.EndOfStream)
                    {
                        int outputBytesRead = encoderStdout.Read(outputBuffer, 0, outputBuffer.Length);
                        if (outputBytesRead > 0)
                        {
                            string message = Encoding.ASCII.GetString(outputBuffer, 0, outputBytesRead);
                            logger.Log(message, LogLevel.Vrb);
                        }
                    }

                    while (!encoderStderr.EndOfStream)
                    {
                        int outputBytesRead = encoderStderr.Read(outputBuffer, 0, outputBuffer.Length);
                        if (outputBytesRead > 0)
                        {
                            string message = Encoding.ASCII.GetString(outputBuffer, 0, outputBytesRead);
                            logger.Log(message, LogLevel.Vrb);
                        }
                    }

                    return encoderProcess.ExitCode;
                }
                finally
                {
                    if (encoderProcess != null && !encoderProcess.HasExited)
                    {
                        encoderProcess.Kill();
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e, LogLevel.Err);
                return -1;
            }
        }
    }
}
