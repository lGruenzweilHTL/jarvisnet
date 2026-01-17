using AssistantCore.Chat;
using AssistantCore.Tools.Dto;

namespace AssistantCore.Workers.Dto.Impl;

public record LlmRequest(string RequestId, LlmInput Input, LlmConfig Config, LlmContext Context)
    : WorkerRequest<LlmInput, LlmConfig, LlmContext>(RequestId, Input, Config, Context);

public record LlmInput(string Prompt, ToolDto[] Tools, ChatContext ChatContext);
public record LlmConfig(int MaxTokens, float Temperature);
public record LlmContext(string Location);