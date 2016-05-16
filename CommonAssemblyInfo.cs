using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


[assembly: AssemblyCompany("d3 Inc.")]
[assembly: AssemblyCopyright("created by yoq © 2016, BSD open source license")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision 

[assembly: AssemblyVersion("0.2.5")]

static class VersionHelper
{
    static string _version = null, _copyright = null;

    public static string GetVersion()
    {
        if (_version == null)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Version vers = asm.GetName().Version;
            string versionString = vers.Major + "." + vers.Minor + "." + vers.Build;

            string confStr = (asm.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false)[0] as AssemblyConfigurationAttribute).Configuration;
            _version = versionString + (confStr == "" ? "" : " " + confStr);
        }

        return _version;
    }

    public static string GetCopyright()
    {
        if (_copyright == null)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            _copyright = (asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute).Copyright;
        }

        return _copyright;
    }
}