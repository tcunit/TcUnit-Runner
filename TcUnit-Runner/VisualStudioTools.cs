using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            string line;
            string vsVersion = null;

            System.IO.StreamReader file = new System.IO.StreamReader(@filePath);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("VisualStudioVersion"))
                {
                    string version = line.Substring(line.LastIndexOf('=') + 2);
                    log.Info("In Visual Studio solution file, found Visual Studio version " + version);
                    string[] numbers = version.Split('.');
                    string major = numbers[0];
                    string minor = numbers[1];

                    int n;
                    int n2;

                    bool isNumericMajor = int.TryParse(major, out n);
                    bool isNumericMinor = int.TryParse(minor, out n2);

                    if (isNumericMajor && isNumericMinor)
                    {
                        vsVersion = major + "." + minor;
                    }
                }
            }
            file.Close();
            return vsVersion;
        }
    }
}
