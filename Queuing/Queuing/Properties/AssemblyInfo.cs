//-----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Queuing")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Rare Crowds Inc")]
[assembly: AssemblyProduct("Queuing")]
[assembly: AssemblyCopyright("Copyright © Rare Crowds Inc 2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1cb156bc-d2f2-4222-b5f1-473ee3cf747c")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: InternalsVisibleTo("RuntimeIoc.WebRole")]
[assembly: InternalsVisibleTo("RuntimeIoc.WorkerRole")]
[assembly: InternalsVisibleTo("RuntimeIoc.WebRole.IntegrationTests")]
[assembly: InternalsVisibleTo("RuntimeIoc.WorkerRole.IntegrationTests")]
[assembly: InternalsVisibleTo("AzureQueueUnitTests")]
[assembly: InternalsVisibleTo("QueuingUnitTests")]
[assembly: InternalsVisibleTo("ScheduledActivityUnitTests")]