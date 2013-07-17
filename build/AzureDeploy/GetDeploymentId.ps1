#
# this Power Shell script will swap the staging and production deployments

$error.clear()

function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

# Validation
if ($args.length -ne 3)
{
   Write-Host "Usage: GetDeploymentId.ps1 <PublishProfile> <ConnectionsXml> <DeploymentSlot>"
   Write-Host "PublishProfile: Location of the .azurePubxml file"
   Write-Host "ConnectionsXml: Location of the Azure connections XML file"
   Write-Host "DeploymentSlot: Either 'production' or 'staging'"
   exit 880
}

if (test-path $args[0])
{
	$publishProfilePath = $args[0]
}
else
{
   Write-Host "Invalid publish profile:" $args[0]
   exit 881
}

if (test-path $args[1])
{
  $connectionsFile = $args[1]
}
else
{
   Write-Host "Invalid connections file:" $args[1]
   exit 881
}

if ($args[2] -eq "production" -or $args[2] -eq "staging") 
{
	$slot = $args[2]
}
else
{
	Write-Host "Deployment slot must be specified as production or staging"
	exit 880
}

# Import WAPPSCmdlets and publish helpers
$WAPPSCmdlets = Join-Path (Get-ScriptDirectory) "WAPPSCmdlets\Microsoft.WindowsAzure.Samples.ManagementTools.PowerShell.dll"
Import-Module -Name $WAPPSCmdlets

$azurePublishHelpers = Join-Path (Get-ScriptDirectory) "AzurePublishHelpers.dll"
Import-Module -Name $azurePublishHelpers

$azurePubXml = New-Object AzurePublishHelpers.AzurePubXmlHelper
$publishProfileFullPath = Resolve-Path $publishProfilePath
$publishProfile = $azurePubXml.GetPublishProfile($publishProfileFullPath)
$azureConnections = New-Object AzurePublishHelpers.WindowsAzureConnectionsHelper
$azureConnections.ConnectionsFile = Resolve-Path $connectionsFile

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
$cert = get-item $certPath
$servicename = $publishProfile.HostedServiceName

# Get the deployment
$deployment = get-deployment $slot -subscriptionid $sub -certificate $cert -serviceName $servicename
if ($error)
{
	Write-host No deployments found for slot $slot of service $servicename
}
else
{
	Write-host DeploymentId: $deployment.DeploymentId
}
