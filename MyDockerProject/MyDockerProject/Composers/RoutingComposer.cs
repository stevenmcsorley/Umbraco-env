using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.Website.Routing;
using Microsoft.AspNetCore.Routing;
using Umbraco.Cms.Core.Web;

namespace MyDockerProject.Composers;

public class RoutingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Insert our custom content finder at the VERY BEGINNING of the content finder chain
        // This ensures it runs first and prevents Umbraco from trying to find Event/Offer documents
        // We insert it BEFORE ContentFinderByUrlAlias so it can stop the pipeline before URL alias finding runs
        builder.ContentFinders().InsertBefore<Umbraco.Cms.Core.Routing.ContentFinderByUrlAlias, CustomRouteContentFinder>();
    }
}


public class CustomRouteContentFinder : IContentFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var path = request.Uri.GetAbsolutePathDecoded();
        
        // Check if this is one of our custom controller routes
        if (path.StartsWith("/hotels/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Match pattern: /hotels/{hotelSlug}/events/{eventSlug}
            // Match pattern: /hotels/{hotelSlug}/offers/{offerSlug}
            // Match pattern: /hotels/{hotelSlug}/rooms/{roomSlug} (for consistency)
            if (segments.Length >= 4 && 
                (segments[2].Equals("events", StringComparison.OrdinalIgnoreCase) ||
                 segments[2].Equals("offers", StringComparison.OrdinalIgnoreCase) ||
                 segments[2].Equals("rooms", StringComparison.OrdinalIgnoreCase)))
            {
                // CRITICAL: Set published content to null BEFORE returning true
                // This prevents Umbraco from trying to enumerate descendants and find Event/Offer/Room documents
                // which causes the model factory error
                request.SetPublishedContent(null);
                
                // CRITICAL: Also set the template to null to prevent Umbraco from trying to render a template
                request.SetTemplate(null);
                
                // CRITICAL: Set culture to null to prevent culture-specific content finding
                request.SetCulture(null);
                
                // Mark as handled to prevent further content finding
                // This stops the content finder pipeline and prevents ContentFinderByUrlAlias from running
                // Returning true here tells Umbraco "I handled this request, don't try to find content"
                return Task.FromResult(true);
            }
        }
        
        // Return false to let other content finders try
        return Task.FromResult(false);
    }
}

