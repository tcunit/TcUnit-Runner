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
        public const int RETURN_NO_PLC_PROJECT_IN_TWINCAT_PROJECT = 3;
        public const int RETURN_VISUAL_STUDIO_VERSION_NOT_INSTALLED = 4;
        public const int RETURN_BUILD_ERROR = 5;
        public const int RETURN_TWINCAT_VERSION_NOT_FOUND = 6;
        public const int RETURN_TWINCAT_PROJECT_FILE_NOT_FOUND = 7;
        public const int RETURN_ARGUMENT_ERROR = 8;
        public const int RETURN_VISUAL_STUDIO_SOLUTION_PATH_NOT_PROVIDED = 9;
        public const int RETURN_VISUAL_STUDIO_SOLUTION_PATH_NOT_FOUND = 10;
        public const int RETURN_ERROR_LOADING_VISUAL_STUDIO_DTE = 11;
        public const int RETURN_ERROR_LOADING_VISUAL_STUDIO_SOLUTION = 12;
        public const int RETURN_ERROR_FINDING_VISUAL_STUDIO_SOLUTION_VERSION = 13;
        public const int RETURN_NOT_POSSIBLE_TO_PARSE_REAL_TIME_TASK_XML_DATA = 14;
        public const int RETURN_FAILED_FINDING_DEFINED_UNIT_TEST_TASK_IN_TWINCAT_PROJECT = 15;
        public const int RETURN_TASK_COUNT_NOT_EQUAL_TO_ONE = 16;
        public const int RETURN_TIMEOUT = 17;


        public const string XUNIT_RESULT_FILE_NAME = "TcUnit_xUnit_results.xml";
        public const string RT_CONFIG_ROUTE_SETTINGS_SHORTCUT = "TIRR"; // Shortcut for "Real-Time Configuration^Route Settings"
        public const string PLC_CONFIGURATION_SHORTCUT = "TIPC"; // Shortcut for "PLC Configuration"
        public const string REAL_TIME_CONFIGURATION_ADDITIONAL_TASKS = "TIRT"; // Shortcut for "Real-Time Configuration^Additional Tasks" or " Real-Time Configuration" TAB "Additional Tasks"
        public const string LOCAL_AMS_NET_ID = "127.0.0.1.1.1";
    }
}
