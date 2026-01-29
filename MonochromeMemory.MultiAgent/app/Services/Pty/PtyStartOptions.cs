using System.Collections.Generic;

namespace CodexMultiAgent.App.Services.Pty;

public sealed class PtyStartOptions
{
    public string Executable { get; init; } = string.Empty;
    public IReadOnlyList<string> Arguments { get; init; } = new List<string>();
    public string WorkingDirectory { get; init; } = ".";
    public int Columns { get; init; } = 120;
    public int Rows { get; init; } = 30;
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();
}
