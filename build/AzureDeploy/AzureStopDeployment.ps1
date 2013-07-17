#
# this Power Shell script will stop the deployment

$error.clear()

function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

# Validation
if ($args.length -lt 2)
{
   Write-Host "Usage: AzureStopDeployment.ps1 <PublishProfile> <ConnectionsXml> [DeploymentSlot] "
   Write-Host "PublishProfile: Location of the .azurePubxml file"
   Write-Host "ConnectionsXml: Location of the Azure connections XML file"
   Write-Host "DeploymentSlot: Optional deployment slot. Either 'production' or 'staging'"
   Write-Host "                Overrides the slot specified in the .azurePubxml file" 
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

if ($args.length -eq 3)
{
	if ($args[2] -eq "production" -or $args[2] -eq "staging") 
	{
		$deploymentSlot = $args[2]
	}
	else
	{
		Write-Host "Environment must be specified as production or staging"
		exit 880
	}
}
else
{
	$deploymentSlot = $publishProfile.DeploymentSlot
}
Write-host Using Environment slot: $deploymentSlot

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

Get-HostedService $servicename -Certificate $cert -SubscriptionId $sub | 
    Get-Deployment -Slot $deploymentSlot | 
    Set-DeploymentStatus 'suspended' | 
    Get-OperationStatus -WaitToComplete
	
Write-host "Stopped deployment"

if ($error) { exit 888 }
