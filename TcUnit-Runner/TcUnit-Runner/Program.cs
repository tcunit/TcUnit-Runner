using EnvDTE80;
using log4net;
using log4net.Config;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TCatSysManagerLib;

namespace TcUnit.TcUnit_Runner
{
    class Program
    {
        private static string VisualStudioSolutionFilePath = null;
        private static string TwinCATProjectFilePath = null;
        private static string TcUnitTaskName = null;
        private static string AmsNetId = null;
        private static VisualStudioInstance vsInstance;
        private static ILog log = LogManager.GetLogger("TcUnit-Runner");

        [STAThread]
        static int Main(string[] args)
        {
            bool showHelp = false;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPressHandler);
            log4net.GlobalContext.Properties["LogLocation"] = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config"));

            OptionSet options = new OptionSet()
                .Add("v=|VisualStudioSolutionFilePath=", "The full path to the TwinCAT project (sln-file)", v => VisualStudioSolutionFilePath = v)
                .Add("t=|TcUnitTaskName=", "[OPTIONAL] The name of the task running TcUnit defined under \"Tasks\"", t => TcUnitTaskName = t)
                .Add("a=|AmsNetId=", "[OPTIONAL] The AMS NetId of the device of where the project and TcUnit should run", a => AmsNetId = a)
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

            /*
             * This program consists of the following stages:
             * 1. Verification of input
             *    1.1. Verify that the user has supplied visual studio (VS) solution file
             *    1.2. Verify that the solution file exists
             * 2. Load TwinCAT project
             *    2.1. Find TwinCAT project in VS solution file
             *    2.2. Find which version of TwinCAT was used
             * 3. Load the VS DTE and TwinCAT XAE with the right version of TwinCAT using the remote manager
             * 4. Load the solution
             * 5. Check that the solution has at least one PLC-project
             * 6. Clean the solution
             * 7. Build the solution. Make sure that build was successful.
             * 8. Set target NetId to 127.0.0.1.1.1
             * 9. If user has provided 'TcUnitTaskName', iterate all PLC projects and do:
             *     9.1. Find the 'TcUnitTaskName', and set the <AutoStart> to TRUE and <Disabled> to FALSE for the TIRT^ of the TASK
             *     9.2. Iterate the rest of the tasks (if there are any), and set the <AutoStart> to FALSE and <Disabled> to TRUE for the TIRT^ of the task
             * 10. Enable boot project autostart for all PLC projects
             * 11. Activate configuration
             * 12. Restart TwinCAT
             * 13. Wait until TcUnit has reported all results and collect all results
             * 14. Write all results to xUnit compatible XML-file
             *

            /* Make sure the user has supplied the path for the Visual Studio solution file.
             * Also verify that this file exists.
             */
            if (showHelp || VisualStudioSolutionFilePath == null)
            {
                DisplayHelp(options);
                return Constants.RETURN_ERROR;
            }
            if (!File.Exists(VisualStudioSolutionFilePath))
            {
                log.Error("ERROR: Visual studio solution " + VisualStudioSolutionFilePath + " does not exist!");
                return Constants.RETURN_ERROR;
            }

            LogBasicInfo();
            MessageFilter.Register();

            TwinCATProjectFilePath = TcFileUtilities.FindTwinCATProjectFile(VisualStudioSolutionFilePath);
            if (String.IsNullOrEmpty(TwinCATProjectFilePath))
            {
                log.Error("ERROR: Did not find TwinCAT project file in solution. Is this a TwinCAT project?");
                return Constants.RETURN_TWINCAT_PROJECT_FILE_NOT_FOUND;
            }

            if (!File.Exists(TwinCATProjectFilePath))
            {
                log.Error("ERROR : TwinCAT project file " + TwinCATProjectFilePath + " does not exist!");
                return Constants.RETURN_TWINCAT_PROJECT_FILE_NOT_FOUND;
            }

            string tcVersion = TcFileUtilities.GetTcVersion(TwinCATProjectFilePath);
            if (String.IsNullOrEmpty(tcVersion))
            {
                log.Error("ERROR: Did not find TwinCAT version in TwinCAT project file path");
                return Constants.RETURN_TWINCAT_VERSION_NOT_FOUND;
            }

            try
            {
                vsInstance = new VisualStudioInstance(@VisualStudioSolutionFilePath, tcVersion);
                vsInstance.Load();
            }
            catch
            {
                log.Error("ERROR: Error loading VS DTE. Is the correct version of Visual Studio and TwinCAT installed? Is the TcUnit-Runner running with administrator privileges?");
                CleanUp();
                return Constants.RETURN_ERROR;
            }

            try
            {
                vsInstance.LoadSolution();
            }
            catch
            {
                log.Error("ERROR: Error the solution. Try to open it manually and see that it's actually possible to open and all dependencies are working");
                CleanUp();
                return Constants.RETURN_ERROR;
            }

            if (vsInstance.GetVisualStudioVersion() == null)
            {
                log.Error("ERROR: Did not find Visual Studio version in Visual Studio solution file");
                CleanUp();
                return Constants.RETURN_ERROR;
            }

            AutomationInterface automationInterface = new AutomationInterface(vsInstance.GetProject());

            if (automationInterface.PlcTreeItem.ChildCount <= 0)
            {
                log.Error("ERROR: No PLC-project exists in TwinCAT project");
                CleanUp();
                return Constants.RETURN_NO_PLC_PROJECT_IN_TWINCAT_PROJECT;
            }

            ITcSmTreeItem realTimeTasksTreeItem = automationInterface.RealTimeTasksTreeItem;
            /* Task name provided */
            if (!String.IsNullOrEmpty(TcUnitTaskName))
            {
                log.Info("Setting task '" + TcUnitTaskName + "' enable and autostart, and all other tasks (if existing) to disable and non-autostart");
                bool foundTcUnitTaskName = false;

                /* Find all tasks, and check whether the user provided TcUnit task is amongst them.
                 * Also update the task object (Update <Disabled> and <Autostart>-tag)
                 */
                foreach (ITcSmTreeItem child in realTimeTasksTreeItem)
                {
                    ITcSmTreeItem testTreeItem = realTimeTasksTreeItem.LookupChild(child.Name);
                    string xmlString = testTreeItem.ProduceXml();
                    string newXmlString = "";

                    try
                    {
                        if (TcUnitTaskName.Equals(XmlUtilities.GetItemNameFromRealTimeTaskXML(xmlString)))
                        {
                            foundTcUnitTaskName = true;
                            newXmlString = XmlUtilities.SetDisabledAndAndAutoStartOfRealTimeTaskXml(xmlString, false, true);
                        }
                        else
                        {
                            newXmlString = XmlUtilities.SetDisabledAndAndAutoStartOfRealTimeTaskXml(xmlString, true, false);
                        }
                        testTreeItem.ConsumeXml(newXmlString);
                        System.Threading.Thread.Sleep(3000);
                    }
                    catch
                    {
                        log.Error("ERROR: Could not parse real time task XML data");
                        CleanUp();
                        return Constants.RETURN_ERROR;
                    }
                }

                if (!foundTcUnitTaskName)
                {
                    log.Error("ERROR: Could not find task " + TcUnitTaskName + " in TwinCAT project");
                    CleanUp();
                    return Constants.RETURN_ERROR;
                }

            }

            /* No task name provided */
            else
            {
                log.Info("No task name provided. Assuming only one task exists");
                /* Check that only one task exists */
                if (realTimeTasksTreeItem.ChildCount.Equals(1))
                {
                    // Get task name
                    ITcSmTreeItem child = realTimeTasksTreeItem.get_Child(1);
                    ITcSmTreeItem testTreeItem = realTimeTasksTreeItem.LookupChild(child.Name);
                    string xmlString = testTreeItem.ProduceXml();
                    TcUnitTaskName = XmlUtilities.GetItemNameFromRealTimeTaskXML(xmlString);
                    log.Info("Found task with name '" + TcUnitTaskName + "'");
                    string newXmlString = "";
                    newXmlString = XmlUtilities.SetDisabledAndAndAutoStartOfRealTimeTaskXml(xmlString, false, true);
                    testTreeItem.ConsumeXml(newXmlString);
                    System.Threading.Thread.Sleep(3000);
                }
                /* Less ore more than one task, which is an error */
                else
                {
                    log.Error("ERROR: The amount of tasks is not equal to 1 (one). Found " + realTimeTasksTreeItem.ChildCount.ToString() + " amount of tasks. Please provide which task is the TcUnit task");
                    CleanUp();
                    return Constants.RETURN_ERROR;
                }

            }


            /* Build the solution and collect any eventual errors. Make sure to
             * filter out everything that is an error
             */
            vsInstance.CleanSolution();
            vsInstance.BuildSolution();

            ErrorItems errors = vsInstance.GetErrorItems();

            int tcBuildWarnings = 0;
            int tcBuildError = 0;
            for (int i = 1; i <= errors.Count; i++)
            {
                ErrorItem item = errors.Item(i);
                if ((item.ErrorLevel != vsBuildErrorLevel.vsBuildErrorLevelLow))
                {
                    if (item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelMedium)
                        tcBuildWarnings++;
                    else if (item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                    {
                        tcBuildError++;
                        log.Error("Description: " + item.Description);
                        log.Error("ErrorLevel: " + item.ErrorLevel);
                        log.Error("Filename: " + item.FileName);
                    }
                }
            }


            /* If we don't have any errors, activate the configuration and 
             * start/restart TwinCAT */
            if (tcBuildError.Equals(0))
            {
                log.Info("Setting target NetId to 127.0.0.1.1.1 (localhost)");
                automationInterface.ITcSysManager.SetTargetNetId("127.0.0.1.1.1");
                log.Info("Enabling boot project and setting BootProjectAutostart on " + automationInterface.ITcSysManager.GetTargetNetId());

                for (int i = 1; i <= automationInterface.PlcTreeItem.ChildCount; i++)
                {
                    ITcSmTreeItem plcProject = automationInterface.PlcTreeItem.Child[i];
                    ITcPlcProject iecProject = (ITcPlcProject)plcProject;
                    iecProject.BootProjectAutostart = true;
                }
                System.Threading.Thread.Sleep(1000);
                automationInterface.ActivateConfiguration();

                // Wait
                System.Threading.Thread.Sleep(10000);

                /* Clean the solution. This is the only way to clean the error list which needs to be
                 * clean prior to starting the TwinCAT runtime */
                vsInstance.CleanSolution();

                // Wait
                System.Threading.Thread.Sleep(10000);

                automationInterface.StartRestartTwinCAT();
            }
            else
            {
                log.Error("ERROR: Build errors in project");
                CleanUp();
                return Constants.RETURN_BUILD_ERROR;
            }

            /* Run TcUnit until the results have been returned */
            TcUnitResultCollector tcUnitResultCollector = new TcUnitResultCollector();
            ErrorList errorList = new ErrorList();

            log.Info("Waiting for results from TcUnit...");
            while (true)
            {
                System.Threading.Thread.Sleep(10000);

                ErrorItems errorItems = vsInstance.GetErrorItems();
                log.Info("... got " + errorItems.Count + " report lines so far.");

                var newErrors = errorList.AddNew(errorItems);
                if (tcUnitResultCollector.AreResultsAvailable(newErrors))
                {
                    log.Info("All results from TcUnit obtained");
                    /* The last test suite result can be returned after that we have received the test results, wait a few seconds
                     * and fetch again
                    */
                    System.Threading.Thread.Sleep(3000);
                    errorList.AddNew(vsInstance.GetErrorItems());
                    break;
                }
            }

            /* Parse all events (from the error list) from Visual Studio and store the results */
            TcUnitTestResult testResult = tcUnitResultCollector.ParseResults(errorList, TcUnitTaskName);

            /* Write xUnit XML report */
            if (testResult != null)
            {
                // No need to check if file (VisualStudioSolutionFilePath) exists, as this has already been done
                string VisualStudioSolutionDirectoryPath = Path.GetDirectoryName(VisualStudioSolutionFilePath);
                string XUnitReportFilePath = VisualStudioSolutionDirectoryPath + "\\" + Constants.XUNIT_RESULT_FILE_NAME;
                log.Info("Writing xUnit XML file to " + XUnitReportFilePath);
                // Overwrites all existing content (if existing)
                System.IO.File.WriteAllText(XUnitReportFilePath, XunitXmlCreator.GetXmlString(testResult));
            }

            CleanUp();
            return Constants.RETURN_SUCCESSFULL;
        }

        static void DisplayHelp(OptionSet p)
        {
            Console.WriteLine("Usage: TcUnit-Runner [OPTIONS]");
            Console.WriteLine("Loads the TcUnit-runner program with the selected visual studio solution and TwinCAT project.");
            Console.WriteLine("Example #1: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\"");
            Console.WriteLine("Example #2: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -t \"UnitTestTask\"");
            Console.WriteLine("Example #3: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -t \"UnitTestTask\" -a 192.168.4.221.1.1");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Executed if user interrups the program (i.e. CTRL+C)
        /// </summary>
        static void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs args)
        {
            log.Info("Application interrupted by user");
            CleanUp();
            Environment.Exit(0);
        }

        /// <summary>
        /// Cleans the system resources (the VS DTE)
        /// </summary>
        private static void CleanUp()
        {
            try
            {
                vsInstance.Close();
            }
            catch { }

            log.Info("Exiting application...");
            MessageFilter.Revoke();
        }

        static void LogBasicInfo()
        {
            log.Info("TcUnit-Runner build: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info("TcUnit-Runner build date: " + Utilities.GetBuildDate(Assembly.GetExecutingAssembly()).ToShortDateString());
            log.Info("Visual Studio solution path: " + VisualStudioSolutionFilePath);
            log.Info("");
        }
    }
}
