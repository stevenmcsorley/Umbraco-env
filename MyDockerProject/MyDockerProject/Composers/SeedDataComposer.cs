using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using System.Threading;

namespace MyDockerProject.Composers;

public class SeedDataComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Components().Append<SeedDataComponent>();
        builder.Services.AddScoped<Services.SeedDataService>();
    }
}

public class SeedDataComponent : Umbraco.Cms.Core.Composing.IAsyncComponent
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentService _contentService;

    public SeedDataComponent(IContentTypeService contentTypeService, IContentService contentService)
    {
        _contentTypeService = contentTypeService;
        _contentService = contentService;
    }

    public async Task InitializeAsync(bool isAfterRuntime, CancellationToken cancellationToken)
    {
        // Wait a bit for Umbraco to be ready
        await Task.Delay(5000, cancellationToken);
        var seedService = new Services.SeedDataService(_contentService, _contentTypeService);
        seedService.SeedExampleHotel();
    }

    public Task TerminateAsync(bool isBeforeRuntime, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Terminate()
    {
    }
}

