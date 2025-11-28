using System.Text;
using System.Text.RegularExpressions;

namespace MyDockerProject.Helpers;

public static class UrlHelper
{
    /// <summary>
    /// Converts a string to a URL-friendly slug
    /// </summary>
    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Convert to lowercase
        string slug = input.ToLowerInvariant();

        // Replace spaces and special characters with hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", " ").Trim();
        slug = Regex.Replace(slug, @"\s", "-");
        
        // Remove multiple consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");

        return slug;
    }
}

