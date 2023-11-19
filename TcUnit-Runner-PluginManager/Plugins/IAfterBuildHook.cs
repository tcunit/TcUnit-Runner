using log4net;
using TcUnit.TcUnit_Runner.PluginManager.Options;

namespace TcUnit.TcUnit_Runner.PluginManager.Plugins
{
    public interface IAfterBuildHook : IPluginBase
    {
        int Run(EnvDTE.Project project, ILog log, PluginOptions options);
    }
}
