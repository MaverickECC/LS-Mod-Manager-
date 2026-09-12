using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyCoop.ModManager.Models;

public sealed class AdvancedSavegameValue : INotifyPropertyChanged
{
    private string _value = "";
    public required string Category { get; init; }
    public required string ObjectName { get; init; }
    public required string PropertyName { get; init; }
    public required string FilePath { get; init; }
    public required string Locator { get; init; }
    public required string ValueType { get; init; }
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public required string OriginalValue { get; init; }
    public string Value { get => _value; set { _value = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsModified)); } }
    public string SourceFile => Path.GetFileName(FilePath);
    public bool IsModified => !string.Equals(Value, OriginalValue, StringComparison.Ordinal);
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
