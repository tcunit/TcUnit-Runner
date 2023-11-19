using System;
using TCatSysManagerLib;

namespace TcUnit.TcUnit_Runner.Plugin
{
    internal class AutomationInterface
    {
        private ITcSysManager15 sysManager = null;

        public AutomationInterface(EnvDTE.Project project)
        {
            sysManager = (ITcSysManager15)project.Object;
        }

        public ITcSysManager15 ITcSysManager => this.sysManager;
    }
}
