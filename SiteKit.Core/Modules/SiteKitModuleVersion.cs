using System.Text.RegularExpressions;

namespace SiteKit.Core.Modules;

/// <summary>Small dependency-free SemVer and range evaluator for module packages.</summary>
public static class SiteKitModuleVersion
{
    private static readonly Regex VersionPattern = new("^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z.-]+))?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsValid(string? value) => value is not null && VersionPattern.IsMatch(value);

    public static int Compare(string left, string right)
    {
        var a = Parse(left); var b = Parse(right);
        var core = a.Core.CompareTo(b.Core);
        if (core != 0) return core;
        if (a.Prerelease is null) return b.Prerelease is null ? 0 : 1;
        if (b.Prerelease is null) return -1;
        var count = Math.Min(a.Prerelease.Length, b.Prerelease.Length);
        for (var index = 0; index < count; index++)
        {
            var leftId = a.Prerelease[index]; var rightId = b.Prerelease[index];
            var leftNumeric = int.TryParse(leftId, out var leftNumber);
            var rightNumeric = int.TryParse(rightId, out var rightNumber);
            var comparison = leftNumeric && rightNumeric ? leftNumber.CompareTo(rightNumber)
                : leftNumeric ? -1 : rightNumeric ? 1
                : string.Compare(leftId, rightId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
        }
        return a.Prerelease.Length.CompareTo(b.Prerelease.Length);
    }

    public static bool IsValidRange(string? range)
    {
        if (range == "*") return true;
        if (string.IsNullOrWhiteSpace(range) || range.Length > 160) return false;
        var tokens = range.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return false;
        foreach (var token in tokens)
        {
            var version = token.StartsWith(">=", StringComparison.Ordinal) || token.StartsWith("<=", StringComparison.Ordinal)
                ? token[2..]
                : token.StartsWith('>') || token.StartsWith('<') || token.StartsWith('^') || token.StartsWith('~') ? token[1..] : token;
            if (!IsValid(version)) return false;
        }
        return true;
    }

    public static bool Satisfies(string version, string range)
    {
        if (!IsValid(version) || !IsValidRange(range)) return false;
        if (range == "*") return true;
        foreach (var token in range.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var op = token.StartsWith(">=", StringComparison.Ordinal) || token.StartsWith("<=", StringComparison.Ordinal) ? token[..2]
                : token.StartsWith('>') || token.StartsWith('<') || token.StartsWith('^') || token.StartsWith('~') ? token[..1] : "=";
            var required = op == "=" ? token : token[op.Length..];
            var comparison = Compare(version, required);
            var parsed = Parse(required).Core;
            var satisfies = op switch
            {
                "=" => comparison == 0,
                ">" => comparison > 0,
                ">=" => comparison >= 0,
                "<" => comparison < 0,
                "<=" => comparison <= 0,
                "^" => comparison >= 0 && Compare(version, UpperCaret(parsed)) < 0,
                "~" => comparison >= 0 && Compare(version, $"{parsed.Major}.{parsed.Minor + 1}.0") < 0,
                _ => false
            };
            if (!satisfies) return false;
        }
        return true;
    }

    private static string UpperCaret(Version version) => version.Major > 0
        ? $"{version.Major + 1}.0.0"
        : version.Minor > 0 ? $"0.{version.Minor + 1}.0" : $"0.0.{version.Build + 1}";

    private static (Version Core, string[]? Prerelease) Parse(string value)
    {
        if (!IsValid(value)) throw new ArgumentException("A valid semantic version is required.", nameof(value));
        var parts = value.Split('-', 2);
        return (Version.Parse(parts[0]), parts.Length == 2 ? parts[1].Split('.') : null);
    }
}
