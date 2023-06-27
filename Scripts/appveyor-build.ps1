$project = ".\SystemMonitor\SystemMonitor.sln"
$releaseConfiguration = "Release"
$framework = "net7.0"

Write-Host "Building $env:APPVEYOR_BUILD_VERSION" -ForegroundColor magenta

Write-Host "Installing build dependencies..." -ForegroundColor green
choco install -y innosetup
Install-Product node ''

Write-Host "Checking versions..." -ForegroundColor green
Write-Host "Dotnet" -ForegroundColor cyan
dotnet --version
Write-Host "Tar" -ForegroundColor cyan
tar --version

Write-Host "Restoring packages..." -ForegroundColor green
dotnet restore $project
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

Write-Host "Building..." -ForegroundColor green
dotnet build $project -c $releaseConfiguration
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

# publish specific profiles
Write-Host "Publishing for each environment..." -ForegroundColor green

$buildEnv = "win10-x64"
Write-Host "Publishing for $buildEnv..." -ForegroundColor cyan
dotnet publish $project -r $buildEnv -c $releaseConfiguration --self-contained
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

$buildEnv = "linux-x64"
Write-Host "Publishing for $buildEnv..." -ForegroundColor cyan
dotnet publish $project -r $buildEnv -c $releaseConfiguration --self-contained
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

$buildEnv = "linux-arm64"
Write-Host "Publishing for $buildEnv..." -ForegroundColor cyan
dotnet publish $project -r $buildEnv -c $releaseConfiguration --self-contained
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

$buildEnv = "osx.10.12-x64"
Write-Host "Publishing for $buildEnv..." -ForegroundColor cyan
dotnet publish $project -r $buildEnv -c $releaseConfiguration --self-contained
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

# build installers
Write-Host "Building Installers" -ForegroundColor green
Set-Location -Path .\SystemMonitor\Installer
(Get-Content .\DesktopInstaller.iss).replace('MyAppVersion "0.0"', 'MyAppVersion "' + $env:APPVEYOR_BUILD_VERSION + '"') | Set-Content .\DesktopInstaller.iss
(Get-Content .\MonitoringServiceInstaller.iss).replace('MyAppVersion "0.0"', 'MyAppVersion "' + $env:APPVEYOR_BUILD_VERSION + '"') | Set-Content .\MonitoringServiceInstaller.iss
(Get-Content .\AgentServiceInstaller.iss).replace('MyAppVersion "0.0"', 'MyAppVersion "' + $env:APPVEYOR_BUILD_VERSION + '"') | Set-Content .\AgentServiceInstaller.iss
Write-Host "Building installer using the following script:" -ForegroundColor cyan
.\build-installer.cmd
if ($LastExitCode -ne 0) { exit $LASTEXITCODE }

# package artifacts
Write-Host "Creating artifact archives..." -ForegroundColor green
Set-Location -Path ..\..\

# (note) windows doesn't need this, it has it's own installer that won't be archived
tar -czf SystemMonitor-Desktop_linux-x64.targz -C .\SystemMonitor\Ui\SystemMonitor.Desktop\bin\$($releaseConfiguration)\$framework\linux-x64\publish .
tar -czf SystemMonitor-Desktop_linux-arm64.targz -C .\SystemMonitor\Ui\SystemMonitor.Desktop\bin\$($releaseConfiguration)\$framework\linux-arm64\publish .
tar -czf SystemMonitor-Desktop_osx.10.12-x64.targz -C .\SystemMonitor\Ui\SystemMonitor.Desktop\bin\$($releaseConfiguration)\$framework\osx.10.12-x64\publish .

tar -czf SystemMonitor-Agent_linux-x64.targz -C .\SystemMonitor\Service\SystemMonitor.Agent\bin\$($releaseConfiguration)\$framework\linux-x64\publish .
tar -czf SystemMonitor-Agent_linux-arm64.targz -C .\SystemMonitor\Service\SystemMonitor.Agent\bin\$($releaseConfiguration)\$framework\linux-arm64\publish .
tar -czf SystemMonitor-Agent_osx.10.12-x64.targz -C .\SystemMonitor\Service\SystemMonitor.Agent\bin\$($releaseConfiguration)\$framework\osx.10.12-x64\publish .

tar -czf SystemMonitor-MonitoringService_linux-x64.targz -C .\SystemMonitor\Service\SystemMonitor.MonitoringService\bin\$($releaseConfiguration)\$framework\linux-x64\publish .
tar -czf SystemMonitor-MonitoringService_linux-arm64.targz -C .\SystemMonitor\Service\SystemMonitor.MonitoringService\bin\$($releaseConfiguration)\$framework\linux-arm64\publish .
tar -czf SystemMonitor-MonitoringService_osx.10.12-x64.targz -C .\SystemMonitor\Service\SystemMonitor.MonitoringService\bin\$($releaseConfiguration)\$framework\osx.10.12-x64\publish .

Write-Host "Uploading Artifacts" -ForegroundColor green
Get-ChildItem .\SystemMonitor\Installer\*.exe | % { Push-AppveyorArtifact $_.FullName }

# rename these artifacts to include the build version number
Get-ChildItem .\*.targz -recurse | % { Push-AppveyorArtifact $_.FullName -FileName "$($_.Basename)-$env:APPVEYOR_BUILD_VERSION.tar.gz" }