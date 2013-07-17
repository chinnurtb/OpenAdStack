#
# this Power Shell script will swap the staging and production deployments

$error.clear()

function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

# Validation
if ($args.length -ne 2)
{
   Write-Host "Usage: AzureSwap.ps1 <PublishProfile> <ConnectionsXml>"
   Write-Host "PublishProfile: Location of the .azurePubxml file"
   Write-Host "ConnectionsXml: Location of the Azure connections XML file"
   exit 880
}

if (!(test-path $args[0]))
{
   Write-Host "Invalid publish profile:" $args[0]
   exit 881
}

if (!(test-path $args[1]))
{
   Write-Host "Invalid connections file:" $args[1]
   exit 881
}

# Import WAPPSCmdlets and publish helpers
$WAPPSCmdlets = Join-Path (Get-ScriptDirectory) "WAPPSCmdlets\Microsoft.WindowsAzure.Samples.ManagementTools.PowerShell.dll"
Import-Module -Name $WAPPSCmdlets

$azurePublishHelpers = Join-Path (Get-ScriptDirectory) "AzurePublishHelpers.dll"
Import-Module -Name $azurePublishHelpers

$azurePubXml = New-Object AzurePublishHelpers.AzurePubXmlHelper
$publishProfileFullPath = Resolve-Path $args[0]
$publishProfile = $azurePubXml.GetPublishProfile($publishProfileFullPath)
Write-Host Using PublishProfile: $publishProfile.ConnectionName
$azureConnections = New-Object AzurePublishHelpers.WindowsAzureConnectionsHelper
$azureConnections.ConnectionsFile = Resolve-Path $args[1]

$connection = $azureConnections.GetConnection($publishProfile.ConnectionName)
if ($connection -eq $null)
{
    Write-Host Could not find connection $publishProfile.ConnectionName in $azureConnections.ConnectionsFile - Make sure you have imported it onto the build machine.
    exit 882
}

$sub = $connection.SubscriptionId
$certThumbprint = $connection.CertificateThumbprint

# For NetworkService use LocalMachine, for user accounts use CurrentUser
$certPath = "cert:\LocalMachine\MY\" + $certThumbprint
Write-Host Using certificate: $certPath
$cert = get-item $certPath
$servicename = $publishProfile.HostedServiceName
Write-Host Using Service Name: $servicename

#Swap deployment from staging to deployment
get-deployment staging -subscriptionid $sub -certificate $cert -serviceName $servicename |
Move-Deployment |
Get-OperationStatus –WaitToComplete

Write-host "Swapped staging to deployment"

if ($error) { exit 888 }
