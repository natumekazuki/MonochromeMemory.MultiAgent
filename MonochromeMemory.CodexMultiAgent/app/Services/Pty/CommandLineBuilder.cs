using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodexMultiAgent.App.Services.Pty;

internal static class CommandLineBuilder
{
    internal static string Build(string executable, IReadOnlyList<string> arguments)
    {
        var parts = new List<string> { Quote(executable) };
        if (arguments != null && arguments.Count > 0)
        {
            parts.AddRange(arguments.Select(Quote));
        }
        return string.Join(" ", parts);
    }

    private static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var needsQuotes = value.Any(char.IsWhiteSpace) || value.Contains('"');
        if (!needsQuotes)
        {
            return value;
        }

        var builder = new StringBuilder();
        builder.Append('"');
        var backslashes = 0;
        foreach (var ch in value)
        {
            if (ch == '\\')
            {
                backslashes++;
                continue;
            }

            if (ch == '"')
            {
                builder.Append(new string('\\', backslashes * 2 + 1));
                builder.Append('"');
                backslashes = 0;
                continue;
            }

            if (backslashes > 0)
            {
                builder.Append(new string('\\', backslashes));
                backslashes = 0;
            }

            builder.Append(ch);
        }

        if (backslashes > 0)
        {
            builder.Append(new string('\\', backslashes * 2));
        }

        builder.Append('"');
        return builder.ToString();
    }
}
