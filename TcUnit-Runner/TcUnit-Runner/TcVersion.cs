using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class TcVersion
    {
        private static ILog log = LogManager.GetLogger("TcUnit-Runner");

        // Singleton constructor
        private TcVersion()
        { }

        /// <summary>
        /// Returns the version of TwinCAT that was used to create the PLC project
        /// </summary>
        public static string GetTcVersion(string TwinCATProjectFilePath)
        {
            string tcVersion = "";
            System.IO.StreamReader file = new System.IO.StreamReader(@TwinCATProjectFilePath);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.Contains("TcVersion"))
                {
                    string version = line.Substring(line.LastIndexOf("TcVersion=\""));
                    int pFrom = version.IndexOf("TcVersion=\"") + "TcVersion=\"".Length;
                    int pTo = version.LastIndexOf("\">");
                    if (pTo > pFrom)
                    {
                        tcVersion = version.Substring(pFrom, pTo - pFrom);
                        log.Info("In TwinCAT project file, found TwinCAT version " + tcVersion);
                    }
                    break;
                }
            }
            file.Close();
            return tcVersion;
        }
    }
}
