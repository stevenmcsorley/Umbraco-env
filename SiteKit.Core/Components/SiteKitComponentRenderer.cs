using System.Net;
using System.Text;
using System.Text.Json;

namespace SiteKit.Core.Components;

/// <summary>Renders validated component definitions using a fixed primitive set; it never evaluates component code.</summary>
public static class SiteKitComponentRenderer
{
    public static string Render(SiteKitComponentDefinition definition, JsonElement data, string? variant = null, string? placementClass = null, SiteKitComponentAppearance? appearance = null)
    {
        var errors = SiteKitComponentValidator.ValidateDefinition(definition);
        errors = errors.Concat(SiteKitComponentValidator.ValidateData(definition, data)).ToArray();
        if (errors.Count > 0) throw new ArgumentException("Invalid component or data: " + string.Join(" ", errors));
        if (variant is not null && !definition.Variants.Any(x => x.Alias == variant))
            throw new ArgumentException("Unknown component variant.", nameof(variant));
        if (placementClass is { Length: > 120 } || (placementClass is not null && !System.Text.RegularExpressions.Regex.IsMatch(placementClass, "^[a-zA-Z0-9 _-]{0,120}$")))
            throw new ArgumentException("Placement style class is invalid.", nameof(placementClass));
        if (appearance is not null && JsonSiteKitPlacementRegistry.ValidateAppearance(appearance).Count > 0)
            throw new ArgumentException("Placement appearance contains an invalid style value.", nameof(appearance));
        var builder = new StringBuilder();
        var variantClass = definition.Variants.FirstOrDefault(x => x.Alias == variant)?.StyleClass;
        builder.Append("<div class=\"sitekit-component");
        if (!string.IsNullOrWhiteSpace(variantClass)) builder.Append(' ').Append(WebUtility.HtmlEncode(variantClass));
        if (!string.IsNullOrWhiteSpace(placementClass)) builder.Append(' ').Append(WebUtility.HtmlEncode(placementClass));
        builder.Append('"');
        var style = BuildStyle(appearance);
        if (style.Length > 0) builder.Append(" style=\"").Append(WebUtility.HtmlEncode(style)).Append('"');
        builder.Append('>');
        RenderNode(builder, definition.Layout, data, new Dictionary<string, JsonElement>(StringComparer.Ordinal));
        builder.Append("</div>");
        return builder.ToString();
    }

    private static void RenderNode(StringBuilder output, SiteKitLayoutNode node, JsonElement data, Dictionary<string, JsonElement> scopes)
    {
        var tag = node.Type switch
        {
            "section" => "section", "container" => "div", "stack" => "div", "grid" => "div", "card" => "article",
            "heading" => "h" + node.HeadingLevel, "text" => "p", "button" => "a", "image" => "img",
            "field" => "div", "list" => "ul", "repeat" => "div", "accordion" => "details", _ => throw new InvalidOperationException("Unsupported primitive.")
        };
        var classes = new List<string> { "sitekit-" + node.Type };
        if (!string.IsNullOrWhiteSpace(node.StyleClass)) classes.Add(node.StyleClass);
        output.Append('<').Append(tag).Append(" class=\"").Append(WebUtility.HtmlEncode(string.Join(' ', classes))).Append('"');
        if (node.Type == "grid") output.Append(" style=\"--sitekit-columns:").Append(node.Columns).Append("\"");

        var value = Resolve(node.Bind, data, scopes);
        if (node.Type == "accordion")
        {
            output.Append("><summary>");
            if (value is { ValueKind: JsonValueKind.String }) output.Append(WebUtility.HtmlEncode(value.Value.GetString()));
            else if (!string.IsNullOrEmpty(node.Text)) output.Append(WebUtility.HtmlEncode(node.Text));
            output.Append("</summary>");
            foreach (var child in node.Children) RenderNode(output, child, data, scopes);
            output.Append("</details>");
            return;
        }
        if (node.Type == "image" && value?.ValueKind == JsonValueKind.String && SiteKitComponentValidator.IsSafeMediaUrl(value.Value.GetString()))
        {
            output.Append(" src=\"").Append(WebUtility.HtmlEncode(value.Value.GetString())).Append("\" alt=\"\" loading=\"lazy\" />");
            return;
        }
        if (node.Type == "button" && value is { ValueKind: JsonValueKind.Object } link && link.TryGetProperty("url", out var url) &&
            url.ValueKind == JsonValueKind.String && SiteKitComponentValidator.IsSafeLink(url.GetString()))
            output.Append(" href=\"").Append(WebUtility.HtmlEncode(url.GetString())).Append('"');
        output.Append('>');

        if (node.Type == "repeat" && value is { ValueKind: JsonValueKind.Array } items)
        {
            foreach (var item in items.EnumerateArray())
            {
                var nestedScopes = new Dictionary<string, JsonElement>(scopes, StringComparer.Ordinal) { [node.ItemAlias!] = item };
                foreach (var child in node.Children) RenderNode(output, child, data, nestedScopes);
            }
        }
        else if (value is not null && node.Type is "text" or "heading" or "button" or "field")
        {
            if (value.Value.ValueKind == JsonValueKind.Object && node.Type == "button" && value.Value.TryGetProperty("label", out var label))
                output.Append(WebUtility.HtmlEncode(label.ToString()));
            else if (value.Value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                output.Append(WebUtility.HtmlEncode(value.Value.ToString()));
        }
        else if (!string.IsNullOrEmpty(node.Text) && node.Type is "text" or "heading" or "button")
            output.Append(WebUtility.HtmlEncode(node.Text));

        foreach (var child in node.Children) RenderNode(output, child, data, scopes);
        output.Append("</").Append(tag).Append('>');
    }

    private static JsonElement? Resolve(string? path, JsonElement root, Dictionary<string, JsonElement> scopes)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;
        var index = 0;
        JsonElement current;
        if (scopes.TryGetValue(parts[0], out current)) index++;
        else current = root;
        for (; index < parts.Length; index++)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(parts[index], out current)) return null;
        }
        return current;
    }

    private static string BuildStyle(SiteKitComponentAppearance? appearance)
    {
        if (appearance is null) return string.Empty;
        var declarations = new List<string>();
        if (appearance.Color is not null) declarations.Add("color:" + appearance.Color);
        if (appearance.BackgroundColor is not null) declarations.Add("background-color:" + appearance.BackgroundColor);
        if (appearance.BorderColor is not null) declarations.Add("border-color:" + appearance.BorderColor);
        if (appearance.BorderRadius is not null) declarations.Add("border-radius:" + appearance.BorderRadius);
        if (appearance.Padding is not null) declarations.Add("padding:" + appearance.Padding);
        return string.Join(';', declarations);
    }
}
