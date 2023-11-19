using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcUnit.TcUnit_Runner.PluginManager.Options;

namespace TcUnit.TcUnit_Runner.PluginManager.Plugins
{
    /// <summary>
    /// Base interface where all plugins/hooks derive from.
    /// </summary>
    public interface IPluginBase
    {
        /// <summary>
        /// Name/identifier for the plugin/hook.
        /// </summary>
        string Name { get; }
    }
}
