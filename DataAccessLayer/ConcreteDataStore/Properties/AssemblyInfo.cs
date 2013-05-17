// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ConcreteDataStore")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Emerging Media Group")]
[assembly: AssemblyProduct("ConcreteDataStore")]
[assembly: AssemblyCopyright("Copyright © Emerging Media Group 2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("12d60c67-4d52-4581-a716-551ac973e3ac")]

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

// This assembly should not be part of any interface where CLS compliance would matter
[assembly: CLSCompliant(false)]

// The only assemblies that should be accessing this are tests and Unity containers.
[assembly: InternalsVisibleTo("RuntimeIoc.WebRole")]
[assembly: InternalsVisibleTo("RuntimeIoc.WorkerRole")]
[assembly: InternalsVisibleTo("OAuthSecurity.RuntimeIoc")]
[assembly: InternalsVisibleTo("SimulatedDataStore")]
[assembly: InternalsVisibleTo("ConcreteDataStoreUnitTests")]
[assembly: InternalsVisibleTo("AzureStorageIntegrationTests")]
[assembly: InternalsVisibleTo("RuntimeIocUnitTests")]
[assembly: InternalsVisibleTo("RuntimeIoc.WebRole.IntegrationTests")]
[assembly: InternalsVisibleTo("RuntimeIoc.WorkerRole.IntegrationTests")]
