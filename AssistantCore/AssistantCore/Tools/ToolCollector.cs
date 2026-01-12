using System.Reflection;
using AssistantCore.Workers;

namespace AssistantCore.Tools;

public static class ToolCollector
{
    private static bool isInitialized = false;
    private static ToolData[] toolMethods;

    private static void GetToolMethods()
    {
        var tools = Assembly.GetExecutingAssembly()
            .GetTypes()
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

    public static ToolData[] GetTools()
    {
        if (!isInitialized) 
            GetToolMethods();

        return toolMethods;
    }
    public static ToolData[] GetToolsBySpeciality(LlmSpeciality speciality)
    {
        if (!isInitialized) 
            GetToolMethods();

        return toolMethods
            .Where(t => t.Attribute.Speciality.HasFlag(speciality))
            .ToArray();
    }
}