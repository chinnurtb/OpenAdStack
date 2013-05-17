$ErrorActionPreference = "Stop" # Treat all errors as terminating errors.

function TryV1
{
    param
    (
        [ScriptBlock] $Command = $(throw "The parameter -Command is required."),
        [ScriptBlock] $Catch   = { throw $_ },
        [ScriptBlock] $Finally = { }
    )
    
    & {
        $local:ErrorActionPreference = "SilentlyContinue"
        
        trap
        {
            trap
            {
                & {
                    trap { throw $_ }
                    & $Finally
                }
                
                throw $_
            }
            
            $_ | & { & $Catch }
        }
        
        & $Command
    }
 
    & {
        trap { throw $_ }
        & $Finally
    }
}

$rootDir = split-path $MyInvocation.MyCommand.Path

$installerScriptSource = "https://doppler.blob.core.windows.net/updates/agentbinaries/SetupDopplerAgent.ps1"
$installerScript = Join-Path $rootDir "SetupDopplerAgent.ps1"
$installerScriptUpdated = Join-Path $rootDir "SetupDopplerAgent_updated.ps1"

TryV1 {
    # Download the script if it exists.
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($installerScriptSource, $installerScriptUpdated)
    Write-Output "Downloaded script from $installerScriptSource to $installerScriptUpdated"
    
    # Ensure we are able to run the updates script.
    & "$installerScriptUpdated" "Get-Version"

    # Overwrite the out-of-date script and jump to it.    
    Move-Item "$installerScriptUpdated" "$installerScript" -Force    
    Write-Output "Replaced local version with downloaded version: $installerScriptUpdated --> $installerScript"    
    
} -Catch {
    Write-Output "Error Encountered: $_" 
    Write-Output "The local script:$installerScript will be run"   
}

Powershell.exe -Command "& '$installerScript' Setup-Agent"
