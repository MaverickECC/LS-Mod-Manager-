namespace EasyCoop.ModManager.Models;

public sealed record ModIssue(ModIssueSeverity Severity, string Message);

public enum ModIssueSeverity
{
    Information,
    Warning,
    Error
}
