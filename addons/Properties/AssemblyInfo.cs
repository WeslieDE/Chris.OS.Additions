using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Addins;

[assembly: AssemblyTitle("Chris.OS.Additions")]
[assembly: AssemblyDescription("Some additions for OpenSimulator. More Script functions, toolsets and settings.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://sahrea.de")]
[assembly: AssemblyProduct("ChrisOSAdditions")]
[assembly: AssemblyCopyright("Copyright (c) Sahrea.de 2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion(OpenSim.VersionInfo.AssemblyVersionNumber)]
[assembly: Addin("Chris.OS.Additions", OpenSim.VersionInfo.VersionNumber)]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]