

Development Environment Setup
NOTE: We have currently only tested with these dependencies, running on Window 7. 
Visual Studio Express and Windows 8 should work, but there may be some dependency changes.


Install Development Tools and SDKs
Visual Studio 2010 Pro
http://msdn.microsoft.com/en-us/vstudio/default.aspx or get Shared drive for downloads.

When creating new projects, VisualStudio automatically uses the registered organization of your computer for the company value in AssemblyInfo.cs. 
Please makes sure if you create new assemblyInfo.cs files you change the values to reflect the Open Source project


Visual Studio 2010 SP1
http://www.microsoft.com/download/en/details.aspx?id=23691

SQL Server 2008 R2 Express
https://www.microsoft.com/betaexperience/pd/SQLEXPDBMT64/enus/

Windows SDK
http://www.microsoft.com/download/en/details.aspx?id=8279

Windows Identity Foundation
http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=17331 (Foundation)

http://www.microsoft.com/download/en/details.aspx?id=4451 (SDK) Windows Azure SDK 7.1.1485.0 Version is important!

http://www.windowsazure.com/en-us/develop/downloads/

https://www.microsoft.com/web/handlers/webpi.ashx?command=getinstallerredirect&appid=vwdorvs11azurepack_1_7_1

FxCop Integrator for Visual Studio 2010
	Open VisualStudio
	Launch the Extension Manager
	Tools -> Extension Manager
	Select the Online Gallery
	Search for "fxcop integrator"
	Click to install and then restart VisualStudio Set the path to FxCopCmd Tools -> Options...
	Select FxCop Integrator
	Set FxCopCmd Path to
	{Your_OAS_Workspace}\trunk\build\Microsoft Fxcop 10.0

Install IIS
Note: Even if IIS is already installed and enabled you need to enable IIS6 Compatibility Control Panel -> Programs -> Turn Windows features on or off -> Internet Information Services -> Web Management Tools -> IIS 6 Management Compatibility Enable IIS Metabase and IIS 6 configuration compatibility If IIS is not installed at all, do the following (and then make sure IIS 6 compat is enabled per the steps above):
•	Run appwiz.cpl by typing it into the Start Menu
•	Click 'Turn Windows features on or off'
•	Expand 'Internet Information Services'
•	Check everything under 'World Wide Web Services' and 'Web Management Tools'
•	Click 'Okay'



Builds
After dependencies are installed
Open Env.cmd to open a build environment
Test to see if everything builds by typing BuildDebug
