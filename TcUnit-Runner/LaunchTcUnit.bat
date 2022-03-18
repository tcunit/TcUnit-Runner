@echo off
rem -----------------------------------------------------------------------------
rem Batch script for launching TcUnit-Runner
rem For documentation see: www.tcunit.org
rem -----------------------------------------------------------------------------
rem Instructions:
rem Set the below variable TCUNIT_RUNNER_INSTALL_DIRECTORY to where TcUnit-Runner was
rem installed. This should normally be left at default and not changed
rem (default C:\Program Files (x86)\TcUnit-Runner)
SET TCUNIT_RUNNER_INSTALL_DIRECTORY=C:\Users\JEB\Source\Repos\TcUnit-Runner_Bulkje\TcUnit-Runner\bin\Debug

rem The parameters that can be supplied are:
rem -t [OPTIONAL] The name of the task running TcUnit defined under "Tasks".
rem    If this is not provided, it's assumed that only one task exists in the TwinCAT project.
rem -a [OPTIONAL] The AMS NetId of the device of where the project and TcUnit should run.
rem    If this is not provided, the local AMS NetId is assumed (127.0.0.1.1.1)
rem -w [OPTIONAL] The version of TwinCAT to be used. If this is not provided, then the following rules apply:
rem    - If TwinCAT project is pinned, go with this version, otherwise...
rem    - Go with latest installed version of TwinCAT
rem -u [OPTIONAL] Timeout the process with an error after X minutes. If no timeout is provided,
rem               the process might run indefinitely in case of error
SET TCUNIT_TASK_NAME=
SET TCUNIT_AMSNETID=
SET TCUNIT_TCVERSION_TO_USE=
SET TCUNIT_TIMEOUT=

CALL :Process_Parameters %1, %2, %3, %4, %5, %6, %7, %8

rem Create parameter call to TcUnit-Runner
SET TCUNIT_RUNNER_PARAMETERS=
IF DEFINED TCUNIT_TASK_NAME (
    SET TCUNIT_RUNNER_PARAMETERS=%TCUNIT_RUNNER_PARAMETERS% --TcUnitTaskName=%TCUNIT_TASK_NAME%
)
IF DEFINED TCUNIT_AMSNETID (
    SET TCUNIT_RUNNER_PARAMETERS=%TCUNIT_RUNNER_PARAMETERS% --AmsNetId=%TCUNIT_AMSNETID%
)
IF DEFINED TCUNIT_TCVERSION_TO_USE (
    SET TCUNIT_RUNNER_PARAMETERS=%TCUNIT_RUNNER_PARAMETERS% --TcVersion=%TCUNIT_TCVERSION_TO_USE%
)
IF DEFINED TCUNIT_TIMEOUT (
    SET TCUNIT_RUNNER_PARAMETERS=%TCUNIT_RUNNER_PARAMETERS% --Timeout=%TCUNIT_TIMEOUT%
)

SET TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH=%TCUNIT_RUNNER_INSTALL_DIRECTORY%\TcUnit-Runner.exe

rem Check that the TcUnit-Runner executable exists
IF NOT EXIST "%TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH%" (
    echo The configured search path "%TCUNIT_RUNNER_INSTALL_DIRECTORY%" for TcUnit-Runner does not exist!
    GOTO Exit
)

IF NOT DEFINED TCUNIT_TASK_NAME (
    echo Task name of the TcUnit task not provided. Assuming only one task in TwinCAT solution
) ELSE (
    echo A TcUnit task name has been provided, using: %TCUNIT_TASK_NAME%
)

IF NOT DEFINED TCUNIT_AMSNETID (
    echo AmsNetId to run TwinCAT/TcUnit is not provided. Assuming TwinCAT/TcUnit will run locally '127.0.0.1.1.1'
) ELSE (
    echo An AmsNetId has been provided, using: %TCUNIT_AMSNETID%
)

IF NOT DEFINED TCUNIT_TCVERSION_TO_USE (
    echo A TwinCAT version is not provided. Assuming latest TwinCAT version should be used
) ELSE (
    echo A TwinCAT version has been provided, using: %TCUNIT_TCVERSION_TO_USE%
)

IF NOT DEFINED TCUNIT_TIMEOUT (
    echo Timeout not provided.
) ELSE (
    echo Timeout has been provided, using [min]: %TCUNIT_TIMEOUT%
)


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
"%TCUNIT_RUNNER_EXECUTABLE_COMPLETE_PATH%" --VisualStudioSolutionFilePath=%VISUAL_STUDIO_SOLUTION_PATH% %TCUNIT_RUNNER_PARAMETERS%

rem %errorlevel% is a system wide environment variable that is set upon execution of a program
echo Exit code is %errorlevel%

EXIT /B %errorlevel%

rem This function process the parameters 
:Process_Parameters

rem First parameter
IF "%~1" == "-t" (
    SET TCUNIT_TASK_NAME=%2
)
IF "%~1" == "-T" (
    SET TCUNIT_TASK_NAME=%2
)
IF "%~1" == "-a" (
    SET TCUNIT_AMSNETID=%2
)
IF "%~1" == "-A" (
    SET TCUNIT_AMSNETID=%2
)
IF "%~1" == "-w" (
    SET TCUNIT_TCVERSION_TO_USE=%2
)
IF "%~1" == "-W" (
    SET TCUNIT_TCVERSION_TO_USE=%2
)
IF "%~1" == "-u" (
    SET TCUNIT_TIMEOUT=%2
)
IF "%~1" == "-U" (
    SET TCUNIT_TIMEOUT=%2
)

rem Second parameter
IF "%~3" == "-t" (
    SET TCUNIT_TASK_NAME=%4
)
IF "%~3" == "-T" (
    SET TCUNIT_TASK_NAME=%4
)
IF "%~3" == "-a" (
    SET TCUNIT_AMSNETID=%4
)
IF "%~3" == "-A" (
    SET TCUNIT_AMSNETID=%4
)
IF "%~3" == "-w" (
    SET TCUNIT_TCVERSION_TO_USE=%4
)
IF "%~3" == "-W" (
    SET TCUNIT_TCVERSION_TO_USE=%4
)
IF "%~3" == "-u" (
    SET TCUNIT_TIMEOUT=%4
)
IF "%~3" == "-U" (
    SET TCUNIT_TIMEOUT=%4
)

rem Third parameter
IF "%~5" == "-t" (
    SET TCUNIT_TASK_NAME=%6
)
IF "%~5" == "-T" (
    SET TCUNIT_TASK_NAME=%6
)
IF "%~5" == "-a" (
    SET TCUNIT_AMSNETID=%6
)
IF "%~5" == "-A" (
    SET TCUNIT_AMSNETID=%6
)
IF "%~5" == "-w" (
    SET TCUNIT_TCVERSION_TO_USE=%6
)
IF "%~5" == "-W" (
    SET TCUNIT_TCVERSION_TO_USE=%6
)
IF "%~5" == "-u" (
    SET TCUNIT_TIMEOUT=%6
)
IF "%~5" == "-U" (
    SET TCUNIT_TIMEOUT=%6
)

rem Fourth parameter
IF "%~7" == "-t" (
    SET TCUNIT_TASK_NAME=%8
)
IF "%~7" == "-T" (
    SET TCUNIT_TASK_NAME=%8
)
IF "%~7" == "-a" (
    SET TCUNIT_AMSNETID=%8
)
IF "%~7" == "-A" (
    SET TCUNIT_AMSNETID=%8
)
IF "%~7" == "-w" (
    SET TCUNIT_TCVERSION_TO_USE=%8
)
IF "%~7" == "-W" (
    SET TCUNIT_TCVERSION_TO_USE=%8
)
IF "%~7" == "-u" (
    SET TCUNIT_TIMEOUT=%8
)
IF "%~7" == "-U" (
    SET TCUNIT_TIMEOUT=%8
)


GOTO:EOF

:Exit
echo Failed!
EXIT /B 1