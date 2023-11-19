using EnvDTE;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcUnit.TcUnit_Runner.PluginManager.Options;
using TcUnit.TcUnit_Runner.PluginManager.Plugins;

namespace TcUnit.TcUnit_Runner.Plugin
{

    [Export(typeof(IAfterSolutionLoadedHook))]
    public class VariantManagerPlugin : IAfterSolutionLoadedHook
    {
        private ILog _log;

        public string Name { get { return "VariantManager"; } }

        public int Run(Project project, ILog log, PluginOptions options)
        {
            _log = log;
            AutomationInterface ai = new AutomationInterface(project);
            return SetProjectVariant(ai, options);
        }


        private int SetProjectVariant(AutomationInterface automationInterface, PluginOptions options)
        {
            var variant = GetVariantFromOptions(options);

            if (string.IsNullOrEmpty(variant))
            {
                _log.Error("No variant provided");
                return -1;
            }

            try
            {
                //Using newer version of ITcSysManager to be able to set variant
                automationInterface.ITcSysManager.CurrentProjectVariant = variant;
                _log.Info("Variant selected with name: " + variant);
                // Wait
                System.Threading.Thread.Sleep(1000);
                return 0;
            }
            catch
            {
                _log.Error("Unable to set variant: " + variant + ". Please provide an existing variant from the project.");
                return -1;
            }

        }

        private string GetVariantFromOptions(PluginOptions options)
        {
            var plugin = options.GetPluginNodeByAttributeName(Name);
            return plugin.GetPluginNodeValueByName("Variant");
        }

    }
}
