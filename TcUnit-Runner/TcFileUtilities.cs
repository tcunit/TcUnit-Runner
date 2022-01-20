using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class TcFileUtilities
    {
        private static ILog log = LogManager.GetLogger("TcUnit-Runner");

        // Singleton constructor
        private TcFileUtilities()
        { }

        /// <summary>
        /// Returns the version of TwinCAT that was used to create the PLC project
        /// </summary>
        /// <returns>Empty string if not found</returns>
        public static string GetTcVersion(string TwinCATProjectFilePath)
        {
            string tcVersion = "";
            System.IO.StreamReader file = new System.IO.StreamReader(@TwinCATProjectFilePath);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.Contains("TcVersion"))
                {
                    // Get the string between TcVersion=" and "
                    int start = line.IndexOf("TcVersion=\"") + 11;
                    string subString = line.Substring(start);
                    int end = subString.IndexOf("\"");
                    if (end > 0)
                    {
                        tcVersion = subString.Substring(0, end);
                        log.Info("In TwinCAT project file, found TwinCAT version " + tcVersion);
                    }
                    break;
                }
            }
            file.Close();
            return tcVersion;
        }



        /// <summary>
        /// Returns the complete path to the TwinCAT project file.
        /// </summary>
        /// <returns>The path to the TwinCAT project file. Empty if not found.</returns>
        public static string FindTwinCATProjectFile(string VisualStudioSolutionFilePath)
        {
            var VisualStudioSolutionAbsFilePath = Path.Combine(Directory.GetCurrentDirectory(), VisualStudioSolutionFilePath);
            log.Debug($"Looking for TwinCAT project in {VisualStudioSolutionAbsFilePath} ...");

            var VisualStudioSolutionDirectoryPath = Path.GetDirectoryName(VisualStudioSolutionAbsFilePath);

            /* Find first .tsproj in .sln file*/
            string line;
            string tcProjectFilePath = "";
            string tcProjectFile = "";

            StreamReader file = new StreamReader(@VisualStudioSolutionFilePath);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("Project"))
                {
                    tcProjectFile = Utilities.GetUntilOrEmpty(line, ".tsproj");
                    tcProjectFile = tcProjectFile.Split(',').ElementAtOrDefault(1) ?? tcProjectFile; // get middle part
                    tcProjectFile = tcProjectFile.Trim('"', ' ');
                    break;
                }
            }
            file.Close();

            if (!string.IsNullOrEmpty(tcProjectFile))
            {
                tcProjectFilePath = VisualStudioSolutionDirectoryPath + "\\" + tcProjectFile + ".tsproj";
                log.Debug($"Found canddidate: {tcProjectFilePath}");

                if (File.Exists(tcProjectFilePath))
                {
                    log.Debug($"Found file directly: {tcProjectFilePath}");
                }
                else { // strip paths 
                    log.Debug($"Stripping dirs from: {tcProjectFilePath}");
                    int indexOfTcProjectFile = tcProjectFile.LastIndexOf("\"") + 1;
                    try
                    {
                        tcProjectFile = tcProjectFile.Substring(indexOfTcProjectFile, (tcProjectFile.Length - indexOfTcProjectFile));

                        // Add visual studio solution directory path
                        tcProjectFilePath = VisualStudioSolutionDirectoryPath + "\\" + tcProjectFile + ".tsproj";
                    }
                    catch (Exception e)
                    {
                        log.Warn("Failed to strip paths", e);
                    }
                }
            }
            return tcProjectFilePath;
        }
    }
}
