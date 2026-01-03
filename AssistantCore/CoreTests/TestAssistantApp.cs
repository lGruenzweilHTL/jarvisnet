using AssistantCore.Workers;
using CoreTests.Satellite.MockWorkers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class TestAssistantApp
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISttWorker>();
            services.RemoveAll<IRoutingWorker>();
            services.RemoveAll<ILlmWorker>();
            services.RemoveAll<ITtsWorker>();

            services.AddSingleton<ISttWorker, FakeSttWorker>();
            services.AddSingleton<IRoutingWorker, FakeRoutingWorker>();
            services.AddSingleton<ILlmWorker, FakeGeneralLlmWorker>();
            services.AddSingleton<ITtsWorker, FakeTtsWorker>();
        });
    }
}