Azure Deployment via PowerShell
-------------------------------

Based on the Azure Build & Deploy Sample by Tom Hollander (http://blogs.msdn.com/tomholl)

The AzureDeploy.ps1 script and its accompanying utilities are used by buildall.cmd to
deploy to Azure as part of the automated build process.

To set up your environment to deploy from buildall.cmd

1. Go to https://windows.azure.com/download/publishprofile.aspx to download a publish profile 
   for your Windows Azure subscription.
2. Run ImportPublishSettings.exe to import the subscription details and certificate onto the build server
   If you're running the build as NETWORK SERVICE or similar non-user account:
    a) Make sure you specify LocalMachine as the certificate store and a custom Windows Azure Connections file
	b) Use the Certificates MMC snap-in to grant access to the certificate private key to the build account

To set up your Windows Azure project ready for the build:

1. Open Visual Studio and open/create a new Windows Azure Project for your application
2. Right-click on the Windows Azure Project (.ccproj) in Solution Explorer and choose Publish
3. Configure all of the publish details for your project (specify the hosted service name, deployment 
   slot, etc) and save this to an .azurePubXml file in the project. Cancel out of the dialog
   (i.e. you do not need to publish to Windows Azure from Visual Studio).
5. Add the project to build\projectsToDeploy.txt
4. Check your solution (including the Profile directory and projectsToDeploy.txt edit) into source control.
