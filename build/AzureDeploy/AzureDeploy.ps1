$error.clear()

function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

# Validation
if ($args.length -lt 4)
{
   Write-Host "Usage: AzureDeployNew.ps1 <BuildPath> <PackageName> <PublishProfile> <ConnectionsXml> [DeploymentSlot]"
   Write-Host "BuildPath:      Location of the build output"
   Write-Host "PackageName:    Name of the package to deploy"
   Write-Host "PublishProfile: Location of the .azurePubxml file"
   Write-Host "ConnectionsXml: Location of the Azure connections XML file"
   Write-Host "DeploymentSlot: Optional deployment slot. Either 'production' or 'staging'"
   Write-Host "                Overrides the slot specified in the .azurePubxml file" 
   exit 880
}

if (!(test-path $args[0]))
{
   Write-Host "Invalid build path:" $args[0]
   exit 881
}

if (!(test-path (join-path $args[0] $args[1])))
{
   Write-Host "Invalid package:" $args[1]
   exit 881
}

if (!(test-path $args[2]))
{
   Write-Host "Invalid publish profile:" $args[2]
   exit 881
}

if (!(test-path $args[3]))
{
   Write-Host "Invalid connections file:" $args[3]
   exit 881
}

if ($args[4] -ne $null)
{
  if ($args[4] -eq "production" -or $args[4] -eq "staging") 
  {
    $deploymentSlot = $args[4]
  }
  else
  {
    Write-Host "Environment must be specified as production or staging"
    exit 880
  }
}

# Import WAPPSCmdlets and publish helpers
$WAPPSCmdlets = Join-Path (Get-ScriptDirectory) "WAPPSCmdlets\Microsoft.WindowsAzure.Samples.ManagementTools.PowerShell.dll"
Import-Module -Name $WAPPSCmdlets

$azurePublishHelpers = Join-Path (Get-ScriptDirectory) "AzurePublishHelpers.dll"
Import-Module -Name $azurePublishHelpers

$azurePubXml = New-Object AzurePublishHelpers.AzurePubXmlHelper
$publishProfileFullPath = Resolve-Path $args[2]
$publishProfile = $azurePubXml.GetPublishProfile($publishProfileFullPath)
Write-Host Using PublishProfile: $publishProfile.ConnectionName
$azureConnections = New-Object AzurePublishHelpers.WindowsAzureConnectionsHelper
$azureConnections.ConnectionsFile = Resolve-Path $args[3]

$connection = $azureConnections.GetConnection($publishProfile.ConnectionName)
if ($connection -eq $null)
{
    Write-Host Could not find connection $publishProfile.ConnectionName in $azureConnections.ConnectionsFile - Make sure you have imported it onto the build machine.
    exit 882
}

$sub = $connection.SubscriptionId
$certThumbprint = $connection.CertificateThumbprint

# For NetworkService use LocalMachine, for user accounts use CurrentUser
$certPath = "cert:\LocalMachine\My\" + $certThumbprint
Write-Host Using certificate: $certPath
$cert = get-item $certPath
if ($cert -eq $null)
{
    Write-Host Could not find certificate: $certPath
    exit 882
}

$buildPath = $args[0]
$packagename = $args[1]
$serviceconfig = "ServiceConfiguration." + $publishProfile.ServiceConfiguration + ".cscfg"
$servicename = $publishProfile.HostedServiceName
$storageAccount = $publishProfile.StorageAccountName
$package = join-path $buildPath $packageName
$config = join-path $buildPath $serviceconfig
$buildLabel = $publishProfile.DeploymentLabel
if ($publishProfile.AppendTimestampToDeploymentLabel)
{
    $a = Get-Date
    $buildLabel = $buildLabel + "-" + $a.ToShortDateString() + "-" + $a.ToShortTimeString()
} 
if ($deploymentSlot -eq $null)
{
  $deploymentSlot = $publishProfile.DeploymentSlot
}

Write-Host Suspending previously deployed $servicename
# TODO: Change this to not generate an error if no existing deployment exists
$hostedService = Get-HostedService $servicename -Certificate $cert -SubscriptionId $sub | Get-Deployment -Slot $deploymentSlot
if ($hostedService.Status -ne $null)
{
    $hostedService |
      Set-DeploymentStatus 'Suspended' |
      Get-OperationStatus -WaitToComplete
    $hostedService |
      Remove-Deployment |
      Get-OperationStatus -WaitToComplete
}
 
Get-HostedService $servicename -Certificate $cert -SubscriptionId $sub |
    New-Deployment $deploymentSlot -package $package -configuration $config -label $buildLabel -serviceName $servicename -StorageServiceName $storageAccount |
    Get-OperationStatus -WaitToComplete
 
Get-HostedService $servicename -Certificate $cert -SubscriptionId $sub | 
    Get-Deployment -Slot $deploymentSlot | 
    Set-DeploymentStatus 'Running' | 
    Get-OperationStatus -WaitToComplete
 
$Deployment = Get-HostedService $servicename -Certificate $cert -SubscriptionId $sub | Get-Deployment -Slot $deploymentSlot
Write-host Deployed to $deploymentSlot slot: $Deployment.Url

if ($error) { exit 888 }
