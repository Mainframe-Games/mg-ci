using System.CommandLine;
using System.Xml.Linq;

namespace MG_CLI;

public class CsprojVersioning : Command
{
    private readonly Argument<string> _projectPath = new("path")
    {
        HelpName = "Path to the .csproj"
    };
    
    private readonly Argument<string> _propertyName = new("propertyName")
    {
        HelpName = "The XML tag to update with the version number. Default is 'AssemblyVersion'.",
        DefaultValueFactory = _ => "AssemblyVersion"
    };
    
    public CsprojVersioning() : base("csproj-versioning", "Commands related to versioning and git tags")
    {
        Add(_projectPath);
        Add(_propertyName);
        SetAction(Run);
    }

    private int Run(ParseResult result)
    {
        try
        {
            var projectPath = result.GetRequiredValue(_projectPath);
            var propertyName = result.GetRequiredValue(_propertyName);

            var doc = XDocument.Load(projectPath);
            var propGroup = doc.Root?.Element("PropertyGroup");

            var currentVersion = propGroup?.Element(propertyName)?.Value!;

            var split = currentVersion.Split('.');
            var major = int.Parse(split[0]);
            var minor = int.Parse(split[1]);
            var build = int.Parse(split[2]);
            var newVersion = $"{major}.{minor}.{build + 1}";

            propGroup?.SetElementValue(propertyName, newVersion);
            doc.Save(projectPath);
            Log.Success($"Updated {propertyName} in .csproj to {newVersion}");
        }
        catch (Exception e)
        {
            Log.PrintError(e.ToString());
            return e.HResult;
        }
    
        return 0;
    }
}