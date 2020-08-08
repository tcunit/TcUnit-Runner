using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class VisualStudioTools
    {
        private static ILog log = LogManager.GetLogger("TcUnit-Runner");

        // Singleton constructor
        private VisualStudioTools()
        { }

        /// <summary>
        /// Opens the provided *.sln-file and finds the version of VS used for creation of the solution
        /// </summary>
        /// <returns>The version of Visual Studio used to create the solution</returns>
        public static string FindVisualStudioVersion(string filePath)
        {
            /* Find visual studio version */
            string file;
            try
            {
                file = File.ReadAllText(@filePath);
            }
            catch (ArgumentException)
            {
                return null;
            }

            string pattern = @"^VisualStudioVersion\s+=\s+(?<version>\d+\.\d+)";
            Match match = Regex.Match(file, pattern, RegexOptions.Multiline);

            if (match.Success)
            {
                log.Info("In Visual Studio solution file, found visual studio version " + match.Groups[1].Value);
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
    }
}
