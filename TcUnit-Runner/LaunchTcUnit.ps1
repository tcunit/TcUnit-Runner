param ($vsSolutionPath,
       $tcUnitTaskName,
       $tcUnitAmsNetId,
       $tcUnitForcedTcVersion,
       $tcUnitTimeout,
       $testResultPath)

$FailureExitValue = 1
$SuccessExitValue = 0
$TcUnitRunnerInstallDir = "C:\Program Files (x86)\TcUnit-Runner"

$tcUnitTaskName = $tcUnitTaskName
$tcUnitAmsNetId = $tcUnitAmsNetId
$tcUnitForcedTcVersion = $tcUnitForcedTcVersion
$tcUnitTimeout = $tcUnitTimeout
$vsSolutionPath = $vsSolutionPath

if (-Not $vsSolutionPath) {
    Write-Output "Path to visual studio solution must be given"
    exit $FailureExitValue
}

$tcUnitRunnerExeCompletePath = $TcUnitRunnerInstallDir + "\TcUnit-Runner.exe"

if (-Not ( Test-Path -Path $tcUnitRunnerExeCompletePath)) {
    Write-Output "Could not find path for TcUnit-Runner.exe"
    exit $FailureExitValue
}

# Create parameter call to TcUnit-Runner
$TcUnitRunnerParams="--VisualStudioSolutionFilePath $($vsSolutionPath)"

if ($tcUnitTaskName) {
    Write-Output "taskName was provided, using: $($tcUnitTaskName)"
    $TcUnitRunnerParams = $TcUnitRunnerParams + " --TcUnitTaskName=$($tcUnitTaskName)"
}
else {
    Write-Output "Task name of the TcUnit task not provided. Assuming only one task in TwinCAT solution"
}

if ($tcUnitAmsNetId) {
    Write-Output "An AmsNetId has been provided, using: $($tcUnitAmsNetId)"
    $TcUnitRunnerParams = $TcUnitRunnerParams + " --AmsNetId=$($tcUnitAmsNetId)"
}
else {
    Write-Output "AmsNetId to run TwinCAT/TcUnit is not provided. Assuming TwinCAT/TcUnit will run locally '127.0.0.1.1.1'"
}

if ($tcUnitForcedTcVersion) {
    Write-Output "A TwinCAT version has been provided, using: $($tcUnitForcedTcVersion)"
    $TcUnitRunnerParams = $TcUnitRunnerParams + " --TcVersion=$($tcUnitForcedTcVersion)"
}
else {
    Write-Output "No TwinCAT version provided. Assuming latest TwinCAT version should be used"
}

if ($tcUnitTimeout) {
    Write-Output "Timeout has been provided, using [min]: $($tcUnitTimeout)"
    $TcUnitRunnerParams = $TcUnitRunnerParams + " --Timeout=$($tcUnitTimeout)"
}
else {
    Write-Output "Timeout not provided"
}

if ($testResultPath) {$TcUnitRunnerParams += " -r $($testResultPath)"}

Write-Output "Starting $($tcUnitRunnerExeCompletePath) with arguments: $($TcUnitRunnerParams)"
$tcUnitRunnerProcess = Start-Process -FilePath $tcUnitRunnerExeCompletePath -ArgumentList $TcUnitRunnerParams -NoNewWindow -Wait -PassThru
Write-Output $tcUnitRunnerProcess.exitCode
if ($tcUnitRunnerProcess) {
    if ($tcUnitRunnerProcess.exitCode -ne 0) {exit 1}
    else {exit 0}
}
else {exit 1}