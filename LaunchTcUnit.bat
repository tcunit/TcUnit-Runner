@echo off

rem The MIT License(MIT)

rem Copyright(c) 2020 Jakob Sagatowski

rem Permission is hereby granted, free of charge, to any person obtaining a copy of
rem this software and associated documentation files (the "Software"), to deal in
rem the Software without restriction, including without limitation the rights to
rem use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
rem the Software, and to permit persons to whom the Software is furnished to do so,
rem subject to the following conditions:

rem The above copyright notice and this permission notice shall be included in all
rem copies or substantial portions of the Software.

rem THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
rem IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
rem FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
rem COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
rem IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
rem CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

rem -----------------------------------------------------------------------------
rem USAGE:
rem Set the below variable TCUNIT_RUNNER_INSTALL_DIRECTORY to where TcUnit-Runner was
rem installed. This should normally be left at default and not changed
rem (default C:\Program Files (x86)\TcUnit-Runner)
SET TCUNIT_RUNNER_INSTALL_DIRECTORY=C:\Program Files (x86)\TcUnit-Runner

rem The parameters that need to be supplied are
rem %1 - The name of unit test task
SET TCUNIT_TASK_NAME=%1


set TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH=%TCUNIT_RUNNER_INSTALL_DIRECTORY%\TcUnit-Runner.exe

rem Check that the TcUnit-Runner executable exists
IF NOT EXIST "%TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH%" (
    echo The configured search path "%TCUNIT_RUNNER_INSTALL_DIRECTORY%" for TcUnit-Runner does not exist!
    GOTO Exit
)

rem Error handling of providing TcUnit task name
IF NOT DEFINED TCUNIT_TASK_NAME (
    echo Task name of the TcUnit task not provided! Assuming only one task in TwinCAT solution
) ELSE (
    echo A TcUnit task name has been provided, using: %TCUNIT_TASK_NAME%
)

rem Enter Jenkins workspace (stored in environment variable %WORKSPACE%)
cd %WORKSPACE%

rem Find the visual studio solution file.
FOR /r %%i IN (*.sln) DO (
    SET VISUAL_STUDIO_SOLUTION_PATH="%%i"
)

rem Error handling of finding the files.
IF NOT DEFINED VISUAL_STUDIO_SOLUTION_PATH (
    echo Visual studio solution file path does not exist!
    GOTO Exit
) ELSE (
    echo VISUAL_STUDIO_SOLUTION_PATH found!
    echo The filepath to the visual studio solution file is: %VISUAL_STUDIO_SOLUTION_PATH%
)


rem Call TcUnit-Runner
IF NOT DEFINED TCUNIT_TASK_NAME (
    "%TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH%" --VisualStudioSolutionFilePath=%VISUAL_STUDIO_SOLUTION_PATH%
) ELSE (
    "%TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH%" --VisualStudioSolutionFilePath=%VISUAL_STUDIO_SOLUTION_PATH% --TcUnitTaskName=%TCUNIT_TASK_NAME%
)

rem %errorlevel% is a system wide environment variable that is set upon execution of a program
echo Exit code is %errorlevel%

EXIT /B %errorlevel%

:Exit
echo Failed!
EXIT /B 1