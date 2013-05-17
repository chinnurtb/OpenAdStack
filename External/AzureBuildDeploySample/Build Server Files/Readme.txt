Azure Build & Deploy Sample
---------------------------
by Tom Hollander - http://blogs.msdn.com/tomholl

This sample contains scripts and tools that will help you set up automatic 
build and deployment of Windows Azure project on a build server.

To set up your build server:

1. Install Windows Azure SDK 1.6 (http://www.microsoft.com/windowsazure/sdk)
2. Install Windows Azure Platform PowerShell Cmdlets (http://wappowershell.codeplex.com)
3. Compile the code in this sample (ImportPublishSettings.exe and AzurePublishHelpers.dll)
4. Copy both files to C:\Build on your build server
5. Install AzurePublishHelpers.dll as a PowerShell cmdlet by copying to:
      C:\Windows\System32\WindowsPowerShell\v1.0\Modules\AzurePublishHelpers
	  (or C:\Windows\SysWOW64\WindowsPowerShell\v1.0\Modules\AzurePublishHelpers if you
	   are running a 32-bit build on a 64-bit machine)
6. Copy AzureDeploy.ps1 to "C:\Build" (see comments in the file about changes if your 
   build runs as NETWORK SERVICE)
7. Copy AzureDeploy.targets to 
     C:\Program Files\MSBuild\Microsoft\VisualStudio\v10.0\Windows Azure Tools\1.6\ImportAfter (32-bit OS)
     or C:\Program Files (x86)\MSBuild\Microsoft\VisualStudio\v10.0\Windows Azure Tools\1.6\ImportAfter (64-bit OS)
8. Go to https://windows.azure.com/download/publishprofile.aspx to download a publish profile 
   for your Windows Azure subscription.
9. Run ImportPublishSettings.exe to import the subscription details and certificate onto the build server
   If you're running the build as NETWORK SERVICE or similar non-user account:
    a) Make sure you specify LocalMachine as the certificate store and a custom Windows Azure Connections file
	b) Use the Certificates MMC snap-in to grant access to the certificate private key to the build account

To set up your Windows Azure project ready for the build:

1. Open Visual Studio and open/create a new Windows Azure Project for your application
2. Right-click on the Windows Azure Project (.ccproj) in Solution Explorer and choose Publish
3. Configure all of the publish details for your project (specify the hosted service name, deployment 
   slot, etc) and save this to an .azurePubXml file in the project. Cancel out of the dialog
   (i.e. you do not need to publish to Windows Azure from Visual Studio).
4. Check your solution into TFS source control.

To set up your build definition in TFS:

1. Open Visual Studio, connect to TFS via Team Explorer and set up a new Build Definition
2. Configure the build definition details as appropriate for your project
3. On the Process tab, Expand the Advanced group, and enter the following under "MSBuild Arguments":
     /p:AzurePublishProfile="myServiceProduction.azurePubxml"
   (or whatever you called your publish profile name)
4. Save the build, and kick it off! 