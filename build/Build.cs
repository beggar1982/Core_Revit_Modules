using ModPlusNuke;
using Nuke.Common.Execution;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : RevitModuleBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);
}
