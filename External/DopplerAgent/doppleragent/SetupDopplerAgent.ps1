param(     
    [string] $EntryPoint = "Setup-Agent"
)


# Execution settings
$ErrorActionPreference = "Stop" # Treat all errors as terminating errors.

# Globals
$RootDir = split-path $MyInvocation.MyCommand.Path
$DopplerAgentServiceName = "DopplerInstrumentationService"
$DopplerAgentBinaryName = "Doppler.ScomAdapterService.exe"
$RedButtonStateBinaryName = "Doppler.GacUtility.exe"


function Install-AdapterService
{
    if( Test-Path (Join-Path $env:roleroot "donot.install.agent.service.sem") ) 
    { 
        Log-Message "Configuration is set to do-not install agent service"
        return 
    }            
    
    Cleanup-AdapterService
    $adapterConfigName = $DopplerAgentBinaryName + ".config"
    Update-ConfigWithAgentParameters "$RootDir\$adapterConfigName"
    Setup-AdapterService
}

function Cleanup-AdapterService
{
    $adapterServiceExists = Get-Service | Where-Object {$_.name -eq $DopplerAgentServiceName}    
    if ($adapterServiceExists)
    {    
        $adapterServiceRunning = Get-Service | Where-Object {$_.name -eq $DopplerAgentServiceName -and $_.status -eq "Running"}
        if ($adapterServiceRunning)
        {
            Log-Message "Stopping $DopplerAgentServiceName"
            Stop-Service $DopplerAgentServiceName
        }
        
        Log-Message "Deleting $DopplerAgentServiceName"                
        & sc.exe delete $DopplerAgentServiceName | Out-Null
    }
}

function Update-RedButtonConfig
{
    Copy-Item "$RootDir\$DopplerAgentBinaryName.config" "$RootDir\$RedButtonStateBinaryName.config" -Force
    Update-ConfigWithAgentParameters "$RootDir\$RedButtonStateBinaryName.config"
}

function Update-ConfigWithAgentParameters
{
    param(
        [string] $configName
    )

    $adapterConfigSettings = @{
        "UpdsConnectionString" = "${env:Microsoft.Doppler.UnprocessedDataStore.ConnectionString}";
        "EnvironmentId" = "${env:Microsoft.Doppler.EnvironmentId}";
    }
    
    Log-Message "Updating $configName"
    [xml]$xml = gc $configName
    $i = 0;
    $adapterConfigSettings.Keys | ForEach {
        $xml.configuration.appSettings.add[$i].key = "$_"
        $xml.configuration.appSettings.add[$i].value = $adapterConfigSettings[$_]
        $i++
    }
    
    $xml.Save("$configName")
}

function Setup-AdapterService
{
    Log-Message "Creating $DopplerAgentServiceName"    
    $adapterBinaryPath = Join-Path $RootDir $DopplerAgentBinaryName    
    New-Service $DopplerAgentServiceName $adapterBinaryPath -DisplayName "Doppler Agent Service" -Description "Scom Adapter for Doppler Service" -startupType Automatic | Out-Null               
    
    Log-Message "Starting $DopplerAgentServiceName"
    Start-Service $DopplerAgentServiceName
}

function Get-LogCollectionState
{
    $logCollectionState = (& "$RootDir\$RedButtonStateBinaryName")    
    return $logCollectionState
}

function Install-TraceListenersToGac
{
    [Reflection.Assembly]::LoadWithPartialName("System.EnterpriseServices") | Out-Null
    [System.EnterpriseServices.Internal.Publish] $publish = new-object System.EnterpriseServices.Internal.Publish

    $traceListenersBinaryPath = Join-Path $RootDir "Doppler.TraceListeners.dll"
    Log-Message "Install to GAC: $traceListenersBinaryPath"

    $publish.GacInstall($traceListenersBinaryPath)
}

function Setup-Agent
{
    . "$RootDir\UpdateConfigFiles.ps1"
    Log-Message "Loaded Functions"

    Update-RedButtonConfig

    Install-TraceListenersToGac

    $logCollectionState = Get-LogCollectionState
    Log-Message "LogCollectionState: $logCollectionState"
    
    if ($logCollectionState -eq "On")
    {
        Add-DopplerListenerToConfig
        Add-DopplerHttpModuleToConfig
        
        Install-AdapterService
    }
    else
    {
        Remove-DopplerListenerFromConfig
        Remove-DopplerHttpModuleFromConfig
        
        Install-AdapterService
    }
}

function Get-Version
{
    return [Version] "1.0.0.0"
}

# Jump to the entry point specified in the arguments to this script.
& "$EntryPoint"
