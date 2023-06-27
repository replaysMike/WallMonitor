$project = ".\WallMonitor\WallMonitor.sln"
$releaseConfiguration = "Release"
$framework = "net7.0"

Write-Host "Building $env:APPVEYOR_BUILD_VERSION" -ForegroundColor magenta

Write-Host "Installing build dependencies..." -ForegroundColor green
choco install -y innosetup

Write-Host "Checking versions..." -ForegroundColor green
Write-Host "Dotnet" -ForegroundColor cyan
dotnet --version
Write-Host "Tar" -ForegroundColor cyan
tar --version

Write-Host "Restoring packages..." -ForegroundColor green
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet restore -s https://api.nuget.org/v3/index.json $project
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
Set-Location -Path .\WallMonitor\Installer
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
tar -czf WallMonitor-Desktop_linux-x64.targz -C .\WallMonitor\Ui\WallMonitor.Desktop\bin\$($releaseConfiguration)\$framework\linux-x64\publish .
tar -czf WallMonitor-Desktop_linux-arm64.targz -C .\WallMonitor\Ui\WallMonitor.Desktop\bin\$($releaseConfiguration)\$framework\linux-arm64\publish .
tar -czf WallMonitor-Desktop_osx.10.12-x64.targz -C .\WallMonitor\Ui\WallMonitor.Desktop\bin\$($releaseConfiguration)\$framework\osx.10.12-x64\publish .

tar -czf WallMonitor-Agent_linux-x64.targz -C .\WallMonitor\Service\WallMonitor.Agent\bin\$($releaseConfiguration)\$framework\linux-x64\publish .
tar -czf WallMonitor-Agent_linux-arm64.targz -C .\WallMonitor\Service\WallMonitor.Agent\bin\$($releaseConfiguration)\$framework\linux-arm64\publish .
tar -czf WallMonitor-Agent_osx.10.12-x64.targz -C .\WallMonitor\Service\WallMonitor.Agent\bin\$($releaseConfiguration)\$framework\osx.10.12-x64\publish .

tar -czf WallMonitor-MonitoringService_linux-x64.targz -C .\WallMonitor\Service\WallMonitor.MonitoringService\bin\$($releaseConfiguration)\$framework\linux-x64\publish .
tar -czf WallMonitor-MonitoringService_linux-arm64.targz -C .\WallMonitor\Service\WallMonitor.MonitoringService\bin\$($releaseConfiguration)\$framework\linux-arm64\publish .
tar -czf WallMonitor-MonitoringService_osx.10.12-x64.targz -C .\WallMonitor\Service\WallMonitor.MonitoringService\bin\$($releaseConfiguration)\$framework\osx.10.12-x64\publish .

Write-Host "Uploading Artifacts" -ForegroundColor green
Get-ChildItem .\WallMonitor\Installer\*.exe | % { Push-AppveyorArtifact $_.FullName }

# rename these artifacts to include the build version number
Get-ChildItem .\*.targz -recurse | % { Push-AppveyorArtifact $_.FullName -FileName "$($_.Basename)-$env:APPVEYOR_BUILD_VERSION.tar.gz" }