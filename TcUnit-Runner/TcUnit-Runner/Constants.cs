using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    static class Constants
    {
        public const int RETURN_SUCCESSFULL = 0;
        public const int RETURN_ERROR = 1;
        public const int RETURN_NO_HOSTS_FOUND = 2;
        public const int RETURN_NO_PLC_PROJECT_IN_SOLUTION = 3;
        public const int RETURN_VISUAL_STUDIO_VERSION_NOT_INSTALLED = 4;
        public const int RETURN_BUILD_ERROR = 5;
        public const int RETURN_TWINCAT_VERSION_NOT_FOUND = 6;
        public const string XUNIT_RESULT_FILE_NAME = "TcUnit_xUnit_results.xml";

        public const string RT_CONFIG_ROUTE_SETTINGS_SHORTCUT = "TIRR"; // Shortcut for "Real-Time Configuration^Route Settings"
        public const string PLC_CONFIGURATION_SHORTCUT = "TIPC"; // Shortcut for "PLC Configuration"
    }
}
