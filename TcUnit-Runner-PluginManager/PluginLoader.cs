using log4net;
using log4net.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using TcUnit.TcUnit_Runner.PluginManager.Options;
using TcUnit.TcUnit_Runner.PluginManager.Plugins;

namespace TcUnit.TcUnit_Runner.PluginManager
{
    public class PluginLoader : IPluginLoader
    {
        [ImportMany(typeof(IAfterSolutionLoadedHook))]
        private Lazy<IAfterSolutionLoadedHook>[] _afterSolutionLoadedHooks;
        [ImportMany(typeof(IBeforeBuildHook))]
        private Lazy<IBeforeBuildHook>[] _beforeBuildHooks;
        [ImportMany(typeof(IAfterBuildHook))]
        private Lazy<IAfterBuildHook>[] _afterBuildHooks;
        [ImportMany(typeof(IBeforeActivateConfigurationHook))]
        private Lazy<IBeforeActivateConfigurationHook>[] _beforeActivateConfigurationHooks;
        [ImportMany(typeof(IAfterRunningTestsHook))]
        private Lazy<IAfterRunningTestsHook>[] _afterRunningTestsHooks;

        private ILog _log;

        private PluginOptions _pluginOptions;

        public PluginLoader(ILog log)
        {
            _log = log;
        }

        public void LoadOptions(string path = Constants.XML_PLUGINS_OPTIONS)
        {
            _log.Info(string.Format("Loading options for plugins from '{0}'", path));
            _pluginOptions = PluginOptions.LoadPluginOptions(_log, path);
        }
        public PluginOptions Options { get { return _pluginOptions; } }

        public int RunAfterBuildHooks()
        {
            throw new NotImplementedException();
        }

        public int RunAfterRunningTestsHooks()
        {
            throw new NotImplementedException();
        }

        public int RunAfterSolutionLoadedHooks(EnvDTE.Project project)
        {
            if (_afterSolutionLoadedHooks == null)
            {
                _log.Info("AfterSolutionLoaded: no hooks registered");
                return 0;
            }

            foreach(var lazyHook in _afterSolutionLoadedHooks)
            {
                var hook = lazyHook.Value;
                _log.Info(string.Format("AfterSolutionLoaded: Running plugin {0} ...", hook.Name));
                var result = hook.Run(project, _log, Options);
                if(result < 0)
                {
                    _log.Error(string.Format("Plugin {0} failed with exit code: {1}", hook.Name, result));
                    return result;
                }
            }

            return 0;
        }

        public int RunBeforeActivateConfigurationHooks()
        {
            throw new NotImplementedException();
        }

        public int RunBeforeBuildHooks()
        {
            throw new NotImplementedException();
        }

        public void Load()
        {
            try
            {
                var di = GetPluginsDirectory(Options);
                if (!di.Exists)
                {
                    throw new DirectoryNotFoundException(di.FullName);
                }
                var catalog = new AggregateCatalog();
                LoadPluginsFromDirectoryRecursive(catalog, di);
                var container = new CompositionContainer(catalog);
                container.ComposeParts(this);
            }
            catch (Exception ex)
            {
                _log.Error("Error when loading plugins", ex);
            }
        }

        private DirectoryInfo GetPluginsDirectory(PluginOptions pluginOptions)
        {
            var pluginDir = pluginOptions.GetPluginsDirectory();
            var di = new DirectoryInfo(string.IsNullOrEmpty(pluginDir) ? Constants.PLUGINS_DIR : pluginDir);
            return di;
        }

        private void LoadPluginsFromDirectoryRecursive(AggregateCatalog catalog, DirectoryInfo di)
        {
            Queue<string> directories = new Queue<string>();
            directories.Enqueue(di.FullName);
            while (directories.Count > 0)
            {
                var directory = directories.Dequeue();
                //Load plugins in this folder
                var directoryCatalog = new DirectoryCatalog(directory);
                catalog.Catalogs.Add(directoryCatalog);

                //Add subDirectories to the queue
                var subDirectories = Directory.GetDirectories(directory);
                foreach (string subDirectory in subDirectories)
                {
                    directories.Enqueue(subDirectory);
                }
            }
        }
    }
}
