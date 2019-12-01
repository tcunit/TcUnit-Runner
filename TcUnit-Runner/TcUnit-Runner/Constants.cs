/* 
The MIT License(MIT)

Copyright(c) 2019 Jakob Sagatowski

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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

        public const string RT_CONFIG_ROUTE_SETTINGS_SHORTCUT = "TIRR"; // Shortcut for "Real-Time Configuration^Route Settings"
        public const string PLC_CONFIGURATION_SHORTCUT = "TIPC"; // Shortcut for "PLC Configuration"
    }
}
