﻿using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ModPlus_Revit")]
[assembly: AssemblyConfiguration("")]
#if R2017
[assembly: AssemblyDescription("2017")]
#elif R2018
[assembly: AssemblyDescription("2018")]
#elif R2019
[assembly: AssemblyDescription("2019")]
#elif R2020
[assembly: AssemblyDescription("2020")]
#elif R2021
[assembly: AssemblyDescription("2021")]
#endif
[assembly: AssemblyCompany("modplus.org")]
[assembly: AssemblyProduct("ModPlus")]
[assembly: AssemblyCopyright("Copyright ©  ModPlus")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("5.2.4.0")]
[assembly: AssemblyFileVersion("5.2.4.0")]