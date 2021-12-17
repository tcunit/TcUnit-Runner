﻿using EnvDTE80;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        private string @filePath = null;
        private string vsVersion = null;
        private string tcVersion = null;
        private string forceToThisTwinCatVersion = null;
        private EnvDTE80.DTE2 dte = null;
        private Type type = null;
        private EnvDTE.Solution visualStudioSolution = null;
        private EnvDTE.SolutionBuild solutionBuild = null;
        EnvDTE.Project pro = null;
        ILog log = LogManager.GetLogger("TcUnit-Runner");
        private bool loaded = false;

        public VisualStudioInstance(string @visualStudioSolutionFilePath, string twinCatVersion, string forceToThisTwinCatVersion)
        {
            this.filePath = visualStudioSolutionFilePath;
            string visualStudioVersion = VisualStudioTools.FindVisualStudioVersion(@filePath);
            this.vsVersion = visualStudioVersion;
            this.tcVersion = twinCatVersion;
            this.forceToThisTwinCatVersion = forceToThisTwinCatVersion;
        }

        public VisualStudioInstance(int vsVersionMajor, int vsVersionMinor)
        {
            string visualStudioVersion = vsVersionMajor.ToString() + "." + vsVersionMinor.ToString();
            this.vsVersion = visualStudioVersion;
        }

        /// <summary>
        /// Loads the development tools environment. It does this by:
        /// 1. First checking whether there is an existing VS-DTE process for this solution already created, and in that case attach to it.
        ///    If an existing process doesn't exist, go to step 2.
        /// 2. Create a new instance of the VS DTE
        /// TODO: Implement the AttachToExistingDte() function
        /// </summary>
        public void Load(bool isTcVersionPinned)
        {
            // First try attach to an existing process
            //log.Info("Checking if there is an existing Visual Studio process to attach to ...");
            //string progId = VisualStudioDteAvailable(vsVersion);

            //dte = AttachToExistingDte(progId);
            //AttachToExistingDte();

            //log.Info("DTE: " + dte.ToString());

            if (dte == null)
            {
                // log.Info("... none existing Visual Studio process found. Creating new instance of visual studio DTE.");
                // If existing process doesn't exist, load a new DTE process
                LoadDevelopmentToolsEnvironment(vsVersion, isTcVersionPinned);
            }
            else
            {
                log.Info("... existing Visual Studio process found! Re-using.");
            }
        }

        /// <summary>
        /// Loads the solution-file defined when creating this object
        /// </summary>
        public void LoadSolution()
        {
            if (!String.IsNullOrEmpty(@filePath))
            {
                LoadSolution(@filePath);
                LoadProject();
                loaded = true;
            }
        }

        /// <summary>
        /// Closes the DTE and makes sure the VS process is completely shutdown
        /// </summary>
        public void Close()
        {
            if (loaded) {
                log.Info("Closing the Visual Studio Development Tools Environment (DTE)...");
                Thread.Sleep(20000); // Avoid 'Application is busy'-problem (RPC_E_CALL_REJECTED 0x80010001 or RPC_E_SERVERCALL_RETRYLATER 0x8001010A)
                dte.Quit();
            }
            loaded = false;
        }

        private void LoadDevelopmentToolsEnvironment(string visualStudioVersion, bool isTwinCatVersionPinned)
        {
            /* Make sure the DTE loads with the same version of Visual Studio as the
             * TwinCAT project was created in
             */
            
            // Load the DTE
            string VisualStudioProgId = VisualStudioDteAvailable(visualStudioVersion);
            bool isVersionAvailable = false;
            bool isForceVersionAvailable = false;
            dte.UserControl = false; // have devenv.exe automatically close when launched using automation
            dte.SuppressUI = true;
            // Make sure all types of errors in the error list are collected
            dte.ToolWindows.ErrorList.ShowErrors = true;
            dte.ToolWindows.ErrorList.ShowMessages = true;
            dte.ToolWindows.ErrorList.ShowWarnings = true;
            // First set the SilentMode and then try to open the Remote Manager
            var tcAutomationSettings = dte.GetObject("TcAutomationSettings");
            tcAutomationSettings.SilentMode = true; // Only available from TC3.1.4020.0 and above

            // Load the correct version of TwinCAT using the remote manager in the automation interface
            ITcRemoteManager remoteManager = dte.GetObject("TcRemoteManager");

            var allTwinCatVersions = new List<Version>();
            Version latestTwinCatVersion = null;
   
            // Check if version is installed
            foreach (var possibleVersion in remoteManager.Versions)
            {
                // Add installed TwinCAT version to versions list
                allTwinCatVersions.Add(new Version(possibleVersion));

                if (possibleVersion == this.tcVersion)
                {
                    isVersionAvailable = true;
                }
                // Check if forced version is installed
                if (possibleVersion == this.forceToThisTwinCatVersion && this.forceToThisTwinCatVersion != null)
                {
                    isForceVersionAvailable = true;
                    log.Info("The forced TwinCAT version is available");
                }
            }

            // Find latest installed TwinCAT version
            latestTwinCatVersion = allTwinCatVersions.Max();

            // If the version is pinned, check if the pinned version is available
            if (isTwinCatVersionPinned && (String.IsNullOrEmpty(this.forceToThisTwinCatVersion)))
            {
                if (isVersionAvailable)
                {
                    log.Info("The pinned TwinCAT version is available");
                } else {
                    log.Error("The pinned TwinCAT version is not available");
                    throw new Exception("The pinned TwinCAT version is not available");
                }
            }


            /* The basic procedure/priority for selection of which TwinCAT version should be used is as this:
             * - If TwinCAT project version is forced (by -w argument to TcUnit-Runner), go with this version, otherwise...
             * - If TwinCAT project is pinned, go with this version, otherwise...
             * - Go with latest installed version of TwinCAT
             */
            if (isTwinCatVersionPinned && isVersionAvailable && (String.IsNullOrEmpty(this.forceToThisTwinCatVersion)))
            {
                remoteManager.Version = this.tcVersion;
            }
            else if (isForceVersionAvailable)
            {
                log.Info("TwinCAT version is forced to " + this.forceToThisTwinCatVersion);
                remoteManager.Version = this.forceToThisTwinCatVersion;
            }
            else if (!isForceVersionAvailable && (!String.IsNullOrEmpty(this.forceToThisTwinCatVersion)))
            {
                log.Error("The forced TwinCAT version " + this.forceToThisTwinCatVersion + " is not available");
                throw new Exception("The forced TwinCAT version is not available");
            }
            else if (latestTwinCatVersion != null)
            {
                remoteManager.Version = latestTwinCatVersion.ToString();
            }
            else
            {
               log.Error("It´s not possible to run TwinCAT in this configuration (TwinCAT-ForcedVersion / pinned TwinCAT Version / installed TwinCAT Version ");
               throw new Exception("Wrong configuration for this TwinCAT version");
            }

            // Log the Version which will be loaded
            log.Info("Using the TwinCAT remote manager to load TwinCAT version '" + remoteManager.Version + "'...");
        }


        /// <summary>
        /// Returns any version of Visual Studio that is available on the machine, first trying with the provided version as parameter.
        /// If a version is found, it loads the DTE.
        /// If it fails try to use any DTE starting from the latest DTE to the oldest DTE (12.0 - VS 2013)
        /// If no DTE is found, return null
        /// If the version is greater than 15.0 (2017) or later, try to use the TcXaeShell first.
        /// If that fails use VisulStudio as DTE.
        /// </summary>
        /// <param name="visualStudioVersion"></param>
        /// <returns>The full visual studio prog id (for example "VisualStudio.DTE.15.0") or null if not found</returns>
        private string VisualStudioDteAvailable(string visualStudioVersion)
        {
            /* Try to load the DTE with the same version of Visual Studio as the
             * TwinCAT project was created in
             */
            string VisualStudioProgId;

            Version vsVersion15 = new Version("15.0"); // Beckhoff started with TcXaeShell from version 15.0 (VS2017)
            Version vsVersion = new Version(visualStudioVersion);

            // Check if the TcXaeShell is installed for everything equal or above version 15.0 (VS2017)
            if (vsVersion >= vsVersion15)
            {
                VisualStudioProgId = "TcXaeShell.DTE." + visualStudioVersion;
            }
            else
            {
                VisualStudioProgId = "VisualStudio.DTE." + visualStudioVersion;
            }

            if (TryLoadDte(VisualStudioProgId))
            {
                return VisualStudioProgId;
            } else
            {
                List<string> VisualStudioProgIds = new List<string>();
                VisualStudioProgIds.Add("VisualStudio.DTE.16.0"); // VS2019
                VisualStudioProgIds.Add("TcXaeShell.DTE.15.0"); // TcXaeShell (VS2017)
                VisualStudioProgIds.Add("VisualStudio.DTE.15.0"); // VS2017
                VisualStudioProgIds.Add("VisualStudio.DTE.14.0"); // VS2015
                VisualStudioProgIds.Add("VisualStudio.DTE.12.0"); // VS2013

                foreach (string visualStudioProgIdent in VisualStudioProgIds)
                {
                    if (TryLoadDte(visualStudioProgIdent))
                    {
                        return visualStudioProgIdent;
                    }
                }
            }

            // None found, return null
            return null;

        }
        
        /// <summary>
        /// Tries to load the selected version of VisualStudioProgramIdentity
        /// </summary>
        /// <param name="visualStudioProgIdentity"></param>
        /// <returns>True if successful. False if failed loading DTE</returns>
        private bool TryLoadDte(string visualStudioProgIdentity)
        {
            log.Info("Trying to load the Visual Studio Development Tools Environment (DTE) version '" + visualStudioProgIdentity + "' ...");
            type = System.Type.GetTypeFromProgID(visualStudioProgIdentity);
            try
            {
                dte = (EnvDTE80.DTE2)System.Activator.CreateInstance(type);
                log.Info("...SUCCESSFUL!");
                return true;
            } catch
            {
                log.Info("...FAILED!");
                return false;
            }
        }

        /// <summary>
        /// Searches for DTE instances in the ROT snapshot and returns a Hashtable with all found instances.
        /// This Hashtable may then be used by the method attachToExistingDte() to select single DTE instances.
        /// </summary>
        //private Hashtable GetIDEInstances(bool openSolutionsOnly, string progId)
        private Hashtable GetIDEInstances(bool openSolutionsOnly)
        {
            Hashtable runningIDEInstances = new Hashtable();
            Hashtable runningObjects = GetRunningObjectTable();
            IDictionaryEnumerator rotEnumerator = runningObjects.GetEnumerator();
            while (rotEnumerator.MoveNext())
            {
                string candidateName = (string)rotEnumerator.Key;
                //if (!candidateName.StartsWith("!" + progId))
                //    continue;
                EnvDTE.DTE ide = rotEnumerator.Value as EnvDTE.DTE;
                if (ide == null)
                    continue;
                if (openSolutionsOnly)
                {
                    try
                    {
                        string solutionFile = ide.Solution.FullName;
                        if (solutionFile != String.Empty)
                            runningIDEInstances[candidateName] = ide;
                    }
                    catch { }
                }
                else
                    runningIDEInstances[candidateName] = ide;
            }
            return runningIDEInstances;
        }

        
        /// <summary>
        /// Queries the Running Object Table (ROT) for a snapshot of all running processes in the table. Returns all found processes in a Hashtable, which are then used by
        /// getIdeInstances() for further filtering for DTE instances.
        /// </summary>
        private Hashtable GetRunningObjectTable()
        {
            Hashtable result = new Hashtable();
            IntPtr numFetched = IntPtr.Zero;
            IRunningObjectTable runningObjectTable;
            IEnumMoniker monikerEnumerator;
            IMoniker[] monikers = new IMoniker[1];
            GetRunningObjectTable(0, out runningObjectTable);
            runningObjectTable.EnumRunning(out monikerEnumerator);
            monikerEnumerator.Reset();
            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                IBindCtx ctx;
                CreateBindCtx(0, out ctx);
                string runningObjectName;
                monikers[0].GetDisplayName(ctx, null, out runningObjectName);
                object runningObjectVal;
                runningObjectTable.GetObject(monikers[0], out runningObjectVal);
                result[runningObjectName] = runningObjectVal;
            }
            return result;
        }

        // TODO: Implement the AttachToExistingDte() function
        /// <summary>
        /// Uses the GetIdeInstances() method to select a DTE instance based on its solution path and, when found,
        /// attaches a new DTE object to this instance.
        /// </summary>
        //private EnvDTE.DTE AttachToExistingDte(string progId)
        private EnvDTE.DTE AttachToExistingDte()
        {
            EnvDTE.DTE dteReturn = null;
            //Hashtable dteInstances = GetIDEInstances(false, progId);
            Hashtable dteInstances = GetIDEInstances(false);
            IDictionaryEnumerator hashtableEnumerator = dteInstances.GetEnumerator();

            while (hashtableEnumerator.MoveNext())
            {
                EnvDTE.DTE dteTemp = hashtableEnumerator.Value as EnvDTE.DTE;
                if (dteTemp.Solution.FullName == filePath)
                {
                    log.Info("Found solution in list of all open DTE objects.");
                    dteReturn = dteTemp;
                    break;
                }
            }
            return dteReturn;
        }


        private void LoadSolution(string filePath)
        {
            visualStudioSolution = dte.Solution;
            solutionBuild = dte.Solution.SolutionBuild;
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
            Thread.Sleep(1000);
            solutionBuild.Clean(true);
        }

        public void BuildSolution()
        {
            Thread.Sleep(1000);
            solutionBuild.Build(false);
            SpinWait.SpinUntil(() => solutionBuild.BuildState == EnvDTE.vsBuildState.vsBuildStateDone);
        }

        public ErrorItems GetErrorItems()
        {
            return dte.ToolWindows.ErrorList.ErrorItems;
        }

    }
}