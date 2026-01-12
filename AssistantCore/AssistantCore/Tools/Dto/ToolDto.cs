using System.Text.Json.Serialization;
using AssistantCore.Workers;

namespace AssistantCore.Tools.Dto;

public struct ToolDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("parameters")]
    public ToolParamDto[] Parameters { get; set; }
    
    [JsonPropertyName("speciality")]
    public LlmSpeciality Speciality { get; set; }
}