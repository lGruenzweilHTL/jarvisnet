using System.Reflection;
using AssistantCore.Workers;

namespace AssistantCore.Tools;

public class ToolCollector
{
    private bool isInitialized = false;
    private ToolData[] toolMethods = [];
    private List<Assembly> assemblies = [];

    private void GetToolMethods()
    {
        var tools = assemblies.SelectMany(assembly => assembly.GetTypes())
            .SelectMany(t => t.GetMethods())
            .Select(m => new {Method = m, Attribute = m.GetCustomAttribute<LlmToolAttribute>()})
            .Where(t => t.Attribute != null)
            .ToArray();
        
        toolMethods = tools
            .Select(t => new ToolData
            {
                Method = t.Method,
                Attribute = t.Attribute!,
                Params = t.Method.GetParameters()
                    .Select(p => new ToolData.ParamData
                    {
                        Info = p,
                        Attribute = p.GetCustomAttribute<LlmToolParamAttribute>() ?? 
                                    throw new InvalidOperationException("Every tool parameter must have a LlmToolParamAttribute.")
                    })
                    .ToArray()
            })
            .ToArray();
        
        isInitialized = true;
    }

    public ToolData[] GetTools(bool refresh = false)
    {
        if (!isInitialized || refresh)
            GetToolMethods();

        return toolMethods;
    }
    public ToolData[] GetToolsBySpeciality(LlmSpeciality speciality, bool refresh = false)
    {
        if (!isInitialized || refresh) 
            GetToolMethods();

        return toolMethods
            .Where(t => t.Attribute.Speciality.HasFlag(speciality))
            .ToArray();
    }
    
    public void AddAssembly(Assembly assembly)
    {
        assemblies.Add(assembly);
    }
    public void RemoveAssembly(Assembly assembly)
    {
        assemblies.Remove(assembly);
    }
    public void SetAssemblies(List<Assembly> assemblyList)
    {
        assemblies = assemblyList;
    }
}