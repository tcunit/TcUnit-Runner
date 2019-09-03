using EnvDTE80;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCatSysManagerLib;

namespace TcUnit.TcUnit_Runner
{
    class Program
    {
        private static string VisualStudioSolutionFilePath = null;
        private static string TwinCATProjectFilePath = null;

        [STAThread]
        static int Main(string[] args)
        {
            bool showHelp = false;

            OptionSet options = new OptionSet()
                .Add("v=|VisualStudioSolutionFilePath=", v => VisualStudioSolutionFilePath = v)
                .Add("t=|TwinCATProjectFilePath=", t => TwinCATProjectFilePath = t)
                .Add("?|h|help", h => showHelp = h != null);

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `TcUnit-Runner --help' for more information.");
                return Constants.RETURN_ERROR;
            }
            options.Parse(args);

            /* Make sure the user has supplied the paths for both the Visual Studio solution file
             * and the TwinCAT project file. Also verify that these two files exists.
             */
            if (showHelp || VisualStudioSolutionFilePath == null || TwinCATProjectFilePath == null)
            {
                DisplayHelp(options);
                return Constants.RETURN_ERROR;
            }
            if (!File.Exists(VisualStudioSolutionFilePath))
            {
                Console.WriteLine("ERROR: Visual studio solution " + VisualStudioSolutionFilePath + " does not exist!");
                return Constants.RETURN_ERROR;
            }
            if (!File.Exists(TwinCATProjectFilePath))
            {
                Console.WriteLine("ERROR : TwinCAT project file " + TwinCATProjectFilePath + " does not exist!");
                return Constants.RETURN_ERROR;
            }

            /* Find visual studio version */
            string vsVersion = "";
            string line;
            bool foundVsVersionLine = false;
            System.IO.StreamReader file = new System.IO.StreamReader(@VisualStudioSolutionFilePath);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("VisualStudioVersion"))
                {
                    string version = line.Substring(line.LastIndexOf('=') + 2);
                    Console.WriteLine("In Visual Studio solution file, found visual studio version " + version);
                    string[] numbers = version.Split('.');
                    string major = numbers[0];
                    string minor = numbers[1];

                    bool isNumericMajor = int.TryParse(major, out int n);
                    bool isNumericMinor = int.TryParse(minor, out int n2);

                    if (isNumericMajor && isNumericMinor)
                    {
                        vsVersion = major + "." + minor;
                        foundVsVersionLine = true;
                    }
                    break;
                }
            }
            file.Close();

            if (!foundVsVersionLine)
            {
                Console.WriteLine("Did not find Visual studio version in Visual studio solution file");
                return Constants.RETURN_ERROR;
            }

            /* Find TwinCAT project version */
            string tcVersion = "";
            bool foundTcVersionLine = false;
            file = new System.IO.StreamReader(@TwinCATProjectFilePath);
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
                        foundTcVersionLine = true;
                        Console.WriteLine("In TwinCAT project file, found version " + tcVersion);
                    }
                    break;
                }
            }
            file.Close();
            if (!foundTcVersionLine)
            {
                Console.WriteLine("Did not find TcVersion in TwinCAT project file");
                return Constants.RETURN_ERROR;
            }

            MessageFilter.Register();

            /* Make sure the DTE loads with the same version of Visual Studio as the
             * TwinCAT project was created in
             */
            string VisualStudioProgId = "VisualStudio.DTE." + vsVersion;
            Type type = System.Type.GetTypeFromProgID(VisualStudioProgId);
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)System.Activator.CreateInstance(type);

            //dte.SuppressUI = true;
            //dte.MainWindow.Visible = false;
            EnvDTE.Solution visualStudioSolution = dte.Solution;
            visualStudioSolution.Open(@VisualStudioSolutionFilePath);
            EnvDTE.Project pro = visualStudioSolution.Projects.Item(1);

            ITcRemoteManager remoteManager = dte.GetObject("TcRemoteManager");
            remoteManager.Version = tcVersion;
            var settings = dte.GetObject("TcAutomationSettings");
            settings.SilentMode = true; // Only available from TC3.1.4020.0 and above


            /* Build the solution and collect any eventual errors. Make sure to
             * filter out everything that is an error
             */
            
           visualStudioSolution.SolutionBuild.Clean(true);
           visualStudioSolution.SolutionBuild.Build(true);

           ErrorItems errors = dte.ToolWindows.ErrorList.ErrorItems;

           Console.WriteLine("Errors count: " + errors.Count);
           int tcStaticAnalysisWarnings = 0;
           int tcStaticAnalysisErrors = 0;
           for (int i = 1; i <= errors.Count; i++)
           {
               ErrorItem item = errors.Item(i);
               if ((item.ErrorLevel != vsBuildErrorLevel.vsBuildErrorLevelLow))
               {
                   Console.WriteLine("Description: " + item.Description);
                   Console.WriteLine("ErrorLevel: " + item.ErrorLevel);
                   Console.WriteLine("Filename: " + item.FileName);
                   if (item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelMedium)
                       tcStaticAnalysisWarnings++;
                   else if (item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                       tcStaticAnalysisErrors++;
               }
           }


            /* If we don't have any errors, activate the configuration and 
            * start/restart TwinCAT */
            if (tcStaticAnalysisErrors == 0) {
                /* Clean the solution. This is the only way to clean the error list which needs to be
                 * clean prior to starting the TwinCAT runtime */
                dte.Solution.SolutionBuild.Clean(true);

                ITcSysManager sysMan = pro.Object;
                sysMan.ActivateConfiguration();
                sysMan.StartRestartTwinCAT();
            }

            while (true) {
                System.Threading.Thread.Sleep(1000);
                errors = dte.ToolWindows.ErrorList.ErrorItems;
                Console.WriteLine(Environment.NewLine);
                for (int i = 1; i <= errors.Count; i++)
                {
                    ErrorItem item = errors.Item(i);
                    if ((item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh))
                    {
                        Console.WriteLine("Description: " + item.Description);
                    }
                }
            }
            

            dte.Quit();

            MessageFilter.Revoke();

            /* Return the result to the user */
            if (tcStaticAnalysisErrors > 0)
                return Constants.RETURN_ERROR;
            else
                return Constants.RETURN_SUCCESSFULL;
        }

        static void DisplayHelp(OptionSet p)
        {
            Console.WriteLine("Usage: TcUnit-Runner [OPTIONS]");
            Console.WriteLine("Loads the TcUnit-runner program with the selected visual studio solution and TwinCAT project.");
            Console.WriteLine("Example: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -t \"C:\\Jenkins\\workspace\\TcProject\\PlcProject1\\PlcProj.tsproj\"");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

    }
}
