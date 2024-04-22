using System.Text.RegularExpressions;
using MassTransit;

namespace VUta.Transport;

public class VUtaEndpointNameFormatter : KebabCaseEndpointNameFormatter
{
    public VUtaEndpointNameFormatter(string prefix, bool includeNamespace)
        : base(prefix, includeNamespace)
    {
    }

    public new static IEndpointNameFormatter Instance { get; } = new VUtaEndpointNameFormatter("", true);

    public override string SanitizeName(string name)
    {
        var a = base.SanitizeName(name).Replace("vuta-", "vuta:");
        ;
        return a;
    }
}

public partial class VUtaEntityNameFormatter : IEntityNameFormatter
{
    public string FormatEntityName<T>()
    {
        return "vuta:" + Pattern()
            .Replace(typeof(T).Name, m => $"-{m.Value}")
            .ToLowerInvariant();
    }

    [GeneratedRegex("(?<=[a-z0-9])[A-Z]", RegexOptions.Compiled)]
    private static partial Regex Pattern();
}