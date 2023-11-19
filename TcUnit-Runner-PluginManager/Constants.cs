using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner.PluginManager
{
    public static class Constants
    {
        /// <summary>
        /// The directory to load all plugins from.
        /// Currently, subdirectory where TcUnit-Runner.exe is located.
        /// </summary>
        public const string PLUGINS_DIR = ".\\plugins";
        public const string XML_PLUGINS_OPTIONS = ".TcUnitRunner";
    }
}
