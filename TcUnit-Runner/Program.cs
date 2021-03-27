/*
* This program consists of the following stages:
* 1. Verification of input
*    1.1. Verify that the user has supplied visual studio (VS) solution file
*    1.2. Verify that the solution file exists
* 2. Load TwinCAT project
*    2.1. Find TwinCAT project in VS solution file
*    2.2. Find which version of TwinCAT was used
* 3. Load the VS DTE and TwinCAT XAE with the right version of TwinCAT using the remote manager
*    The "right" version of TwinCAT is decided according to these rules:
*    - If TwinCAT project version is forced (by -w argument to TcUnit-Runner), go with this version, otherwise...
*    - If TwinCAT project is pinned, go with this version, otherwise...
*    - Go with latest installed version of TwinCAT
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
*/

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
using System.Xml;
using System.Xml.XPath;
using TCatSysManagerLib;
using TwinCAT.Ads;

namespace TcUnit.TcUnit_Runner
{
    class Program
    {
        private static string VisualStudioSolutionFilePath = null;
        private static string TwinCATProjectFilePath = null;
        private static string TcUnitTaskName = null;
        private static string ForceToThisTwinCATVersion = null;
        private static string AmsNetId = null;
        private static List<int> AmsPorts = new List<int>();
        private static string Timeout = null;
        private static VisualStudioInstance vsInstance;
        private static ILog log = LogManager.GetLogger("TcUnit-Runner");

        [STAThread]
        static void Main(string[] args)
        {
            bool showHelp = false;
            bool enableDebugLoggingLevel = false;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPressHandler);
            log4net.GlobalContext.Properties["LogLocation"] = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config"));

            OptionSet options = new OptionSet()
                .Add("v=|VisualStudioSolutionFilePath=", "The full path to the TwinCAT project (sln-file)", v => VisualStudioSolutionFilePath = v)
                .Add("t=|TcUnitTaskName=", "[OPTIONAL] The name of the task running TcUnit defined under \"Tasks\"", t => TcUnitTaskName = t)
                .Add("a=|AmsNetId=", "[OPTIONAL] The AMS NetId of the device of where the project and TcUnit should run", a => AmsNetId = a)
                .Add("w=|TcVersion=", "[OPTIONAL] The TwinCAT version to be used to load the TwinCAT project", w => ForceToThisTwinCATVersion = w)
                .Add("u=|Timeout=", "[OPTIONAL] Timeout the process with an error after X minutes", u => Timeout = u)
                .Add("d|debug", "[OPTIONAL] Increase debug message verbosity", d => enableDebugLoggingLevel = d != null)
                .Add("?|h|help", h => showHelp = h != null);
            try
            {
                options.Parse(args);

            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `TcUnit-Runner --help' for more information.");
                Environment.Exit(Constants.RETURN_ARGUMENT_ERROR);
            }

            
            if (showHelp)
            {
                DisplayHelp(options);
                Environment.Exit(Constants.RETURN_SUCCESSFULL);
            }

            /* Set logging level.
             * This is handled by changing the log4net.config file on the fly.
             * The following levels are defined in order of increasing priority:
             * - ALL
             * - DEBUG
             * - INFO
             * - WARN
             * - ERROR
             * - FATAL
             * - OFF
            */
            XmlDocument doc = new XmlDocument();
            doc.Load(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            XmlNode root = doc.DocumentElement;
            XmlNode subNode1 = root.SelectSingleNode("root");
            XmlNode nodeForModify = subNode1.SelectSingleNode("level");
            if (enableDebugLoggingLevel)
                nodeForModify.Attributes[0].Value = "DEBUG";
            else
                nodeForModify.Attributes[0].Value = "INFO";
            doc.Save(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
            System.Threading.Thread.Sleep(500); // A tiny sleep just to make sure that log4net manages to detect the change in the file

            /* Make sure the user has supplied the path for the Visual Studio solution file.
             * Also verify that this file exists.
             */
            if (VisualStudioSolutionFilePath == null)
            {
                log.Error("Visual studio solution path not provided!");
                Environment.Exit(Constants.RETURN_VISUAL_STUDIO_SOLUTION_PATH_NOT_PROVIDED);
            }

            if (!File.Exists(VisualStudioSolutionFilePath))
            {
                log.Error("Visual studio solution " + VisualStudioSolutionFilePath + " does not exist!");
                Environment.Exit(Constants.RETURN_VISUAL_STUDIO_SOLUTION_PATH_NOT_FOUND);
            }

            LogBasicInfo();

            /* Start a timeout for the process(es) if the user asked for it
             */
            if (Timeout != null)
            {
                log.Info("Timeout enabled - process(es) timesout after " +Timeout  +" minute(s)");
                System.Timers.Timer timeout = new System.Timers.Timer(Int32.Parse(Timeout) * 1000 * 60);
                timeout.Elapsed += KillProcess;
                timeout.AutoReset = false;
                timeout.Start();
            }

            MessageFilter.Register();

            TwinCATProjectFilePath = TcFileUtilities.FindTwinCATProjectFile(VisualStudioSolutionFilePath);
            if (String.IsNullOrEmpty(TwinCATProjectFilePath))
            {
                log.Error("Did not find TwinCAT project file in solution. Is this a TwinCAT project?");
                Environment.Exit(Constants.RETURN_TWINCAT_PROJECT_FILE_NOT_FOUND);
            }

            if (!File.Exists(TwinCATProjectFilePath))
            {
                log.Error("TwinCAT project file " + TwinCATProjectFilePath + " does not exist!");
                Environment.Exit(Constants.RETURN_TWINCAT_PROJECT_FILE_NOT_FOUND);
            }

            string tcVersion = TcFileUtilities.GetTcVersion(TwinCATProjectFilePath);

            if (String.IsNullOrEmpty(tcVersion))
            {
                log.Error("Did not find TwinCAT version in TwinCAT project file path");
                Environment.Exit(Constants.RETURN_TWINCAT_VERSION_NOT_FOUND);
            }

            try
            {
                vsInstance = new VisualStudioInstance(@VisualStudioSolutionFilePath, tcVersion, ForceToThisTwinCATVersion);
                bool isTcVersionPinned = XmlUtilities.IsTwinCATProjectPinned(TwinCATProjectFilePath);
                log.Info("Version is pinned: " + isTcVersionPinned);
                vsInstance.Load(isTcVersionPinned);
            }
            catch
            {
                log.Error("Error loading VS DTE. Is the correct version of Visual Studio and TwinCAT installed? Is the TcUnit-Runner running with administrator privileges?");
                CleanUpAndExitApplication(Constants.RETURN_ERROR_LOADING_VISUAL_STUDIO_DTE);
            }
       
            try
            {
                vsInstance.LoadSolution();
            }
            catch
            {
                log.Error("Error loading the solution. Try to open it manually and make sure it's possible to open and that all dependencies are working");
                CleanUpAndExitApplication(Constants.RETURN_ERROR_LOADING_VISUAL_STUDIO_SOLUTION);
            }

            if (vsInstance.GetVisualStudioVersion() == null)
            {
                log.Error("Did not find Visual Studio version in Visual Studio solution file");
                CleanUpAndExitApplication(Constants.RETURN_ERROR_FINDING_VISUAL_STUDIO_SOLUTION_VERSION);
            }


            AutomationInterface automationInterface = new AutomationInterface(vsInstance.GetProject());
            if (automationInterface.PlcTreeItem.ChildCount <= 0)
            {
                log.Error("No PLC-project exists in TwinCAT project");
                CleanUpAndExitApplication(Constants.RETURN_NO_PLC_PROJECT_IN_TWINCAT_PROJECT);
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
                        log.Error("Could not parse real time task XML data");
                        CleanUpAndExitApplication(Constants.RETURN_NOT_POSSIBLE_TO_PARSE_REAL_TIME_TASK_XML_DATA);
                    }
                }

                if (!foundTcUnitTaskName)
                {
                    log.Error("Could not find task '" + TcUnitTaskName + "' in TwinCAT project");
                    CleanUpAndExitApplication(Constants.RETURN_FAILED_FINDING_DEFINED_UNIT_TEST_TASK_IN_TWINCAT_PROJECT);
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
                    log.Error("The number of tasks is not equal to 1 (one). Found " + realTimeTasksTreeItem.ChildCount.ToString() + " number of tasks. Please provide which task is the TcUnit task");
                    CleanUpAndExitApplication(Constants.RETURN_TASK_COUNT_NOT_EQUAL_TO_ONE);
                }
            }


            /* Build the solution and collect any eventual errors. Make sure to
             * filter out everything that is an error
             */
            vsInstance.CleanSolution();
            vsInstance.BuildSolution();

            ErrorItems errorsBuild = vsInstance.GetErrorItems();

            int tcBuildWarnings = 0;
            int tcBuildError = 0;
            for (int i = 1; i <= errorsBuild.Count; i++)
            {
                ErrorItem item = errorsBuild.Item(i);
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
                /* Check whether the user has provided an AMS NetId. If so, use it. Otherwise use
                 * the local AMS NetId */
                if (String.IsNullOrEmpty(AmsNetId))
                    AmsNetId = Constants.LOCAL_AMS_NET_ID;

                log.Info("Setting target NetId to '" +AmsNetId +"'");
                automationInterface.ITcSysManager.SetTargetNetId(AmsNetId);
                log.Info("Enabling boot project and setting BootProjectAutostart on " + automationInterface.ITcSysManager.GetTargetNetId());

                for (int i = 1; i <= automationInterface.PlcTreeItem.ChildCount; i++)
                {
                    ITcSmTreeItem plcProject = automationInterface.PlcTreeItem.Child[i];
                    ITcPlcProject iecProject = (ITcPlcProject)plcProject;
                    iecProject.BootProjectAutostart = true;

                    /* add the port that is used for this PLC to the AmsPorts list that
                     * is later used to monitory the AdsState
                     */
                    string xmlString = plcProject.ProduceXml();
                    AmsPorts.Add(XmlUtilities.AmsPort(xmlString));
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
                log.Error("Build errors in project");
                CleanUpAndExitApplication(Constants.RETURN_BUILD_ERROR);
            }

            /* Establish a connection to the ADS router
             */
            TcAdsClient tcAdsClient = new TcAdsClient();

            /* Run TcUnit until the results have been returned */
            TcUnitResultCollector tcUnitResultCollector = new TcUnitResultCollector();
            ErrorList errorList = new ErrorList();

            log.Info("Waiting for results from TcUnit...");
            
            ErrorItems errorItems;

            while (true)
            {
                System.Threading.Thread.Sleep(10000);

                /* Monitor the AdsState for each PLC that is used in the
                 * solution. If we can't connect to the Ads Router, we just
                 * carry on.
                 */
                try
                {
                    foreach (int amsPort in AmsPorts)
                    {
                        tcAdsClient.Connect(AmsNetId, amsPort);
                        AdsState adsState = tcAdsClient.ReadState().AdsState;
                        if (adsState != AdsState.Run)
                        {
                            log.Error("Invalid AdsState "+adsState +"<>" +AdsState.Run +". This could indicate a PLC Exception, terminating ...");
                            Environment.Exit(Constants.RETURN_INVALID_ADSSTATE);
                        }
                    }
                }
                catch (Exception ex)
                { }
                finally
                {
                    tcAdsClient.Disconnect();
                }

                errorItems = vsInstance.GetErrorItems();

                var newErrors = errorList.AddNew(errorItems);
                if (log.IsDebugEnabled)
                {
                    foreach (var error in newErrors)
                    {
                        log.Debug(error.ErrorLevel + ": " + error.Description);
                    }
                }

                log.Info("... got " + errorItems.Count + " report lines so far.");
                if (tcUnitResultCollector.AreResultsAvailable(errorItems))
                {
                    log.Info("All results from TcUnit obtained");
                    /* The last test suite result can be returned after that we have received the test results, wait a few seconds
                     * and fetch again
                    */
                    System.Threading.Thread.Sleep(10000);
                    break;
                }

            }

            List<ErrorList.Error> errors = new List<ErrorList.Error>(errorList.Where(e => (e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh || e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelLow)));
            List<ErrorList.Error> errorsSorted = errors.OrderBy(o => o.Description).ToList();

            /* Parse all events (from the error list) from Visual Studio and store the results */
            TcUnitTestResult testResult = tcUnitResultCollector.ParseResults(errorsSorted, TcUnitTaskName);

            /* Write xUnit XML report */
            if (testResult != null)
            {
                // No need to check if file (VisualStudioSolutionFilePath) exists, as this has already been done
                string VisualStudioSolutionDirectoryPath = Path.GetDirectoryName(VisualStudioSolutionFilePath);
                string XUnitReportFilePath = VisualStudioSolutionDirectoryPath + "\\" + Constants.XUNIT_RESULT_FILE_NAME;
                log.Info("Writing xUnit XML file to " + XUnitReportFilePath);
                // Overwrites all existing content (if existing)
                XunitXmlCreator.WriteXml(testResult, XUnitReportFilePath);
            }

            CleanUpAndExitApplication(Constants.RETURN_SUCCESSFULL);
        }

        static void DisplayHelp(OptionSet p)
        {
            Console.WriteLine("Usage: TcUnit-Runner [OPTIONS]");
            Console.WriteLine("Loads the TcUnit-runner program with the selected visual studio solution and TwinCAT project.");
            Console.WriteLine("Example #1: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\"");
            Console.WriteLine("Example #2: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -t \"UnitTestTask\"");
            Console.WriteLine("Example #3: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -t \"UnitTestTask\" -a 192.168.4.221.1.1");
            Console.WriteLine("Example #4: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -w \"3.1.4024.11\"");
            Console.WriteLine("Example #5: TcUnit-Runner -v \"C:\\Jenkins\\workspace\\TcProject\\TcProject.sln\" -u 5");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Using the Timeout option the user may specify the longest time that the process 
        /// of this application is allowed to run. Sometimes (on low RAM machines), the
        /// DTE build process will hang and the only way to get out of this situation is
        /// to kill this process and any eventual Visual Studio process.
        /// </summary>
        static private void KillProcess(Object source, System.Timers.ElapsedEventArgs e)
        {
            log.Error("Timeout occured, killing process(es) ...");
            CleanUpAndExitApplication(Constants.RETURN_TIMEOUT);
        }

        /// <summary>
        /// Executed if user interrups the program (i.e. CTRL+C)
        /// </summary>
        static void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs args)
        {
            log.Info("Application interrupted by user");
            CleanUpAndExitApplication(Constants.RETURN_SUCCESSFULL);
        }

        /// <summary>
        /// Cleans the system resources (including the VS DTE) and exits the application
        /// </summary>
        private static void CleanUpAndExitApplication(int exitCode)
        {
            try
            {
                vsInstance.Close();
            }
            catch { }

            log.Info("Exiting application...");
            MessageFilter.Revoke();
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Prints some basic information about the current run of TcUnit-Runner
        /// </summary>
        private static void LogBasicInfo()
        {
            log.Info("TcUnit-Runner build: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            log.Info("TcUnit-Runner build date: " + Utilities.GetBuildDate(Assembly.GetExecutingAssembly()).ToShortDateString());
            log.Info("Visual Studio solution path: " + VisualStudioSolutionFilePath);
            log.Info("");
        }
    }
}
