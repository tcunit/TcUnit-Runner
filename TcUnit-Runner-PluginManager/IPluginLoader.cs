using System;


namespace TcUnit.TcUnit_Runner.PluginManager
{
    internal interface IPluginLoader
    {
        int RunAfterSolutionLoadedHooks(EnvDTE.Project project);
        int RunBeforeBuildHooks();
        int RunAfterBuildHooks();
        int RunBeforeActivateConfigurationHooks();
        int RunAfterRunningTestsHooks();

        /// <summary>
        /// Load the plugin options.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        void LoadOptions(string path = Constants.XML_PLUGINS_OPTIONS);
    }
}
