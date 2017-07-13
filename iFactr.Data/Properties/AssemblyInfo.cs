using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

#if WindowsCE || PocketPC || PCL
#else
[assembly: System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, Execution = true)]
#endif

[assembly: AssemblyTitle("iFactr.Data")]
[assembly: AssemblyDescription("The iFactr RESTful Data Stack client")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Zebra Technologies Corporation")]
[assembly: AssemblyProduct("iFactr Data")]
[assembly: AssemblyCopyright("Copyright © 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]



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
[assembly: AssemblyVersion("4.0.0.42")]
[assembly: AssemblyInformationalVersion("Branch - master (Hash: Working Tree)")]
#if !NETCF
  [assembly: AssemblyFileVersion("4.0.0.42")]
#endif

// For signed assemblies you will need the assembly's public key
// To acquire the public key of a signed assembly you will need the "sn.exe" tool that ships with Visual Studio. 
// From the "Visual Studio Command Prompt": 
// sn -Tp c:\MyExample\SomeLibrary.Test.dll
