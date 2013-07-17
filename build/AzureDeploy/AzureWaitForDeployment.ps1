#
# this Power Shell script will wait until a deployment is in the desired state

$error.clear()

function Get-ScriptDirectory
{
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $Invocation.MyCommand.Path
}

# Validation
if ($args.length -lt 3)
{
   Write-Host "Usage: AzureWaitForDeployment.ps1 <PublishProfile> <ConnectionsXml> <DesiredState> {MaxWait} {DeploymentSlot}"
   Write-Host "PublishProfile: Location of the .azurePubxml file"
   Write-Host "ConnectionsXml: Location of the Azure connections XML file"
   Write-Host "DesiredState:   Desired deployment state to wait for"
   Write-Host "MaxWaitSeconds: Maximum time to wait for deployment to reach the desired state"
   Write-Host "DeploymentSlot: Either 'production' or 'staging'"
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

# TODO: Check for valid status value
$desiredStatus = $args[2]

# TODO: Check for valid number
if ($args[2] -ne $null)
{
	$maxWait = $args[3]
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
$certPath = "cert:\LocalMachine\My\" + $certThumbprint
Write-Host Using certificate: $certPath
$cert = get-item $certPath

Write-Host Getting service information for $publishProfile.HostedServiceName
$service = Get-HostedService $publishProfile.HostedServiceName -Certificate $cert -SubscriptionId $sub

if ($deploymentSlot -eq $null)
{
	$deploymentSlot = $publishProfile.DeploymentSlot
}

Write-Host Waiting for all role instances of $service.ServiceName to be $desiredStatus

$start = Get-Date
while(1)
{
	$deployment = $service | Get-Deployment -slot $deploymentSlot
	
  # Has the service been deployed yet?
  if ($deployment -eq $null)
  {
    Write-Host Deployment not found for $service.ServiceName
    break;
  }

  # Are the role instances in the desired status?
  $rolesNotAtDesiredStatus = 0
  foreach ($roleInstance in $deployment.RoleInstanceList)
  {
    if ($roleInstance.InstanceStatus -ne $desiredStatus)
    {
      $rolesNotAtDesiredStatus += 1
    }
  }

  if ($rolesNotAtDesiredStatus -eq 0)
  {
    Write-Host All role instances of $deployment.ServiceName are $desiredStatus
    break
  }

  # One or more roles was not in the desired status. Wait before checking again.
  $waiting = (((Get-Date) - $start).TotalSeconds)
  if ($maxWait -ne $null -and $waiting -gt $maxWait)
  {
    Write-Host Giving up on $service.ServiceName after $waiting seconds.
    break
  }
  else
  {
    Write-Host (((Get-Date) - $start).TotalSeconds) Waiting for $rolesNotAtDesiredStatus "/"($deployment.RoleInstanceList).Count role instances of $service.ServiceName to be $desiredStatus"..."
    Start-Sleep -s 10
  }
}
