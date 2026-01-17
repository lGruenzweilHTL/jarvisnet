namespace AssistantCore.Workers.Dto.Impl;

public record RoutingRequest(string RequestId, RoutingInput Input, RoutingConfig Config, RoutingContext Context) : 
    WorkerRequest<RoutingInput, RoutingConfig, RoutingContext>(RequestId, Input, Config, Context);
public record RoutingInput(string Text);
public record RoutingConfig(string[] AllowedSpecialities);
public record RoutingContext(string Location);