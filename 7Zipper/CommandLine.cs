using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipper
{
    internal class CommandLine
    {
        public static IDictionary<string, string[]> ParseCommandLineOptions(string[] args)
        {
            IDictionary<string, string[]> returnVal = new Dictionary<string, string[]>();

            string currentParamName = null;
            List<string> currentParamGroup = new List<string>();
            for (int idx = 0; idx < args.Length; idx++)
            {
                string currentArg = args[idx];
                if (string.IsNullOrEmpty(currentArg))
                {
                    continue;
                }

                if (currentArg.Equals("-"))
                {
                    continue;
                }

                if (currentArg.StartsWith("-"))
                {
                    if (currentParamName != null)
                    {
                        returnVal.Add(currentParamName, currentParamGroup.ToArray());
                    }

                    currentParamName = currentArg.TrimStart('-');
                    currentParamGroup.Clear();
                }
                else
                {
                    currentParamGroup.Add(currentArg);
                }
            }

            if (currentParamName != null)
            {
                returnVal.Add(currentParamName, currentParamGroup.ToArray());
            }

            return returnVal;
        }
    }
}
