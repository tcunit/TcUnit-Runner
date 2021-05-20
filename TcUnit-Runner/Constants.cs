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
        public const int RETURN_TESTS_FAILED = 1;
        public const int RETURN_ERROR_NO_TEST_RESULTS = 2;

        public const int RETURN_ERROR = 10;
        public const int RETURN_NO_HOSTS_FOUND = 11;
        public const int RETURN_NO_PLC_PROJECT_IN_TWINCAT_PROJECT = 12;
        public const int RETURN_VISUAL_STUDIO_VERSION_NOT_INSTALLED = 13;
        public const int RETURN_BUILD_ERROR = 14;
        public const int RETURN_TWINCAT_VERSION_NOT_FOUND = 15;
        public const int RETURN_TWINCAT_PROJECT_FILE_NOT_FOUND = 16;
        public const int RETURN_ARGUMENT_ERROR = 17;
        public const int RETURN_VISUAL_STUDIO_SOLUTION_PATH_NOT_PROVIDED = 18;
        public const int RETURN_VISUAL_STUDIO_SOLUTION_PATH_NOT_FOUND = 19;
        public const int RETURN_ERROR_LOADING_VISUAL_STUDIO_DTE = 20;
        public const int RETURN_ERROR_LOADING_VISUAL_STUDIO_SOLUTION = 21;
        public const int RETURN_ERROR_FINDING_VISUAL_STUDIO_SOLUTION_VERSION = 22;
        public const int RETURN_NOT_POSSIBLE_TO_PARSE_REAL_TIME_TASK_XML_DATA = 23;
        public const int RETURN_FAILED_FINDING_DEFINED_UNIT_TEST_TASK_IN_TWINCAT_PROJECT = 24;
        public const int RETURN_TASK_COUNT_NOT_EQUAL_TO_ONE = 25;
        public const int RETURN_TIMEOUT = 26;
        public const int RETURN_INVALID_ADSSTATE = 27;


        public const string XUNIT_RESULT_FILE_NAME = "TcUnit_xUnit_results.xml";
        public const string RT_CONFIG_ROUTE_SETTINGS_SHORTCUT = "TIRR"; // Shortcut for "Real-Time Configuration^Route Settings"
        public const string PLC_CONFIGURATION_SHORTCUT = "TIPC"; // Shortcut for "PLC Configuration"
        public const string REAL_TIME_CONFIGURATION_ADDITIONAL_TASKS = "TIRT"; // Shortcut for "Real-Time Configuration^Additional Tasks" or " Real-Time Configuration" TAB "Additional Tasks"
        public const string LOCAL_AMS_NET_ID = "127.0.0.1.1.1";
    }
}
