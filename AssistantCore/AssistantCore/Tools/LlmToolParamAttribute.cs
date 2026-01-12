namespace AssistantCore.Tools;

public class LlmToolParamAttribute : Attribute
{
    public string ParamName { get; }
    public string Description { get; }
    public bool Required { get; }
    public LlmToolParamAttribute(string paramName, string description, bool required = false)
    {
        ParamName = paramName;
        Description = description;
        Required = required;
    }
}