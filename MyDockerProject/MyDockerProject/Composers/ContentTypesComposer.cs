using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using System.Threading;

namespace MyDockerProject.Composers;

public class ContentTypesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Components().Append<ContentTypesComponent>();
    }
}

public class ContentTypesComponent : Umbraco.Cms.Core.Composing.IAsyncComponent
{
    public Task InitializeAsync(bool isAfterRuntime, CancellationToken cancellationToken)
    {
        // Content types should be created manually in Umbraco backoffice
        // This composer is kept for future programmatic creation if needed
        return Task.CompletedTask;
    }

    public Task TerminateAsync(bool isBeforeRuntime, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Terminate()
    {
    }

}

