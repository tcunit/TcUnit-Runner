using EnvDTE80;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCatSysManagerLib;

namespace TcUnit.TcUnit_Runner
{
    /// <summary>
    /// This class is used to instantiate the Visual Studio Development Tools Environment (DTE)
    /// which is used to programatically access all the functions in VS.
    /// </summary>
    class VisualStudioInstance
    {
        private string @filePath = null;
        private string vsVersion = null;
        private string tcVersion = null;
        private EnvDTE80.DTE2 dte = null;
        private Type type = null;
        private EnvDTE.Solution visualStudioSolution = null;
        EnvDTE.Project pro = null;
        ILog log = LogManager.GetLogger("TcUnit-Runner");
        private bool loaded = false;

        public VisualStudioInstance(string @visualStudioSolutionFilePath)
        {
            this.filePath = visualStudioSolutionFilePath;
            string visualStudioVersion = FindVisualStudioVersion();
            string twinCatVersion = TcFileUtilities.GetTcVersion(visualStudioSolutionFilePath);
            this.vsVersion = visualStudioVersion;
            this.tcVersion = twinCatVersion;
        }

        public VisualStudioInstance(int vsVersionMajor, int vsVersionMinor)
        {
            string visualStudioVersion = vsVersionMajor.ToString() + "." + vsVersionMinor.ToString();
            this.vsVersion = visualStudioVersion;
        }

        /// <summary>
        /// Loads the development tools environment
        /// </summary>
        public void Load()
        {
            loaded = true;
            LoadDevelopmentToolsEnvironment(vsVersion);
            if (!String.IsNullOrEmpty(@filePath))
            {
                LoadSolution(@filePath);
                LoadProject();
            }
        }

        /// <summary>
        /// Closes the DTE and makes sure the VS process is completely shutdown
        /// </summary>
        public void Close()
        {
            if (loaded) {
                log.Info("Closing the Visual Studio Development Tools Environment (DTE), please wait...");
                Thread.Sleep(20000); // Avoid 'Application is busy'-problem (RPC_E_CALL_REJECTED 0x80010001 or RPC_E_SERVERCALL_RETRYLATER 0x8001010A)
                dte.Quit();
            }
            loaded = false;
        }

        /// <summary>
        /// Opens the main *.sln-file and finds the version of VS used for creation of the solution
        /// </summary>
        /// <returns>The version of Visual Studio used to create the solution</returns>
        private string FindVisualStudioVersion()
        {
            /* Find Visual Studio version */
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

        

        private void LoadDevelopmentToolsEnvironment(string visualStudioVersion)
        {
            /* Make sure the DTE loads with the same version of Visual Studio as the
             * TwinCAT project was created in
             */
            string VisualStudioProgId;


            /* TODO: Change this so it first tries
             1. The version of Visual Studio used to create the project.
             2. If it fails try to use any DTE starting from 12.0 (2013) up to the latest DTE
             3. If no DTE is found, give up
             - Also, if the version is greater than 15.0 (2017) or later, try to use the TcXaeShell first. If that fails
               used VisulStudio as DTE
             */
            if (visualStudioVersion.Equals("15.0"))
            {
                VisualStudioProgId = "TcXaeShell.DTE." + visualStudioVersion;
            } else
            {
                VisualStudioProgId = "VisualStudio.DTE." + visualStudioVersion;
            }
            type = System.Type.GetTypeFromProgID(VisualStudioProgId);
            log.Info("Loading the Visual Studio Development Tools Environment (DTE) version " + visualStudioVersion + "...");
            dte = (EnvDTE80.DTE2)System.Activator.CreateInstance(type);
            dte.UserControl = false; // have devenv.exe automatically close when launched using automation
            dte.SuppressUI = true;
            // Make sure all types of errors in the error list are collected
            dte.ToolWindows.ErrorList.ShowErrors = true;
            dte.ToolWindows.ErrorList.ShowMessages = true;
            dte.ToolWindows.ErrorList.ShowWarnings = true;

            // Load the correct version of TwinCAT using the remote manager in the automation interface
            log.Info("Using the TwinCAT remote manager to load TwinCAT version " + tcVersion + "...");
            ITcRemoteManager remoteManager = dte.GetObject("TcRemoteManager");
            remoteManager.Version = tcVersion;

            var tcAutomationSettings = dte.GetObject("TcAutomationSettings");
            tcAutomationSettings.SilentMode = true; // Only available from TC3.1.4020.0 and above
        }

        private void LoadSolution(string filePath)
        {
            visualStudioSolution = dte.Solution;
            visualStudioSolution.Open(@filePath);
        }

        private void LoadProject()
        {
            pro = visualStudioSolution.Projects.Item(1);
        }

        /// <returns>Returns null if no version was found</returns>
        public string GetVisualStudioVersion()
        {
            return this.vsVersion;
        }

        public EnvDTE.Project GetProject()
        {
            return this.pro;
        }

        public EnvDTE80.DTE2 GetDevelopmentToolsEnvironment()
        {
            return dte;
        }

        public void CleanSolution()
        {
            visualStudioSolution.SolutionBuild.Clean(true);
        }

        public void BuildSolution()
        {
            visualStudioSolution.SolutionBuild.Build(true);
        }

        public ErrorItems GetErrorItems()
        {
            return dte.ToolWindows.ErrorList.ErrorItems;
        }

    }
}