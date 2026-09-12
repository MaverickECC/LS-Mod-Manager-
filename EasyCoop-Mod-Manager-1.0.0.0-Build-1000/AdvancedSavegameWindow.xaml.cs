using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EasyCoop.ModManager.Models;
using EasyCoop.ModManager.Services;

namespace EasyCoop.ModManager;

public partial class AdvancedSavegameWindow : Window
{
    private readonly GameInstallation _game;
    private readonly SavegameInfo _savegame;
    private readonly AdvancedSavegameService _service;
    private readonly ObservableCollection<AdvancedSavegameValue> _values = new();
    private readonly ICollectionView _view;
    public AdvancedSavegameWindow(GameInstallation game, SavegameInfo savegame, SavegameService savegameService)
    {
        InitializeComponent(); _game = game; _savegame = savegame; DataContext = game;
        _service = new AdvancedSavegameService(savegameService);
        _view = CollectionViewSource.GetDefaultView(_values); _view.Filter = Filter;
        ValuesGrid.ItemsSource = _view; CategoryBox.SelectedIndex = 0;
        Subtitle.Text = $"{game.ShortName} · {savegame.SlotName} · {savegame.DisplayName}";
        LoadValues();
    }
    private void LoadValues() { _values.Clear(); foreach (var item in _service.Scan(_savegame)) _values.Add(item); StatusText.Text = $"{_values.Count} bearbeitbare Werte erkannt."; }
    private bool Filter(object obj) { if (obj is not AdvancedSavegameValue item) return false; var category=(CategoryBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Alle"; var search=SearchBox.Text.Trim(); return (category=="Alle" || item.Category==category) && (search.Length==0 || item.ObjectName.Contains(search,StringComparison.OrdinalIgnoreCase) || item.PropertyName.Contains(search,StringComparison.OrdinalIgnoreCase)); }
    private void Category_Changed(object sender, SelectionChangedEventArgs e) => _view?.Refresh();
    private void Search_Changed(object sender, TextChangedEventArgs e) => _view?.Refresh();
    private void ValuesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => Dispatcher.BeginInvoke(() => StatusText.Text = $"{_values.Count(x => x.IsModified)} nicht gespeicherte Änderungen.");
    private void Reset_Click(object sender, RoutedEventArgs e) { LoadValues(); _view.Refresh(); }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ValuesGrid.CommitEdit(DataGridEditingUnit.Cell, true); ValuesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var changed=_values.Where(x=>x.IsModified).ToList(); if (changed.Count==0) { MessageBox.Show(this,"Es wurden keine Werte geändert.","EasyCoop",MessageBoxButton.OK,MessageBoxImage.Information); return; }
        if (MessageBox.Show(this,$"{changed.Count} Änderungen speichern?\n\nVorher wird automatisch ein vollständiges Backup erstellt.","Erweiterten Spielstand speichern",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
        try { _service.Save(_game.ShortName,_savegame,_values.ToList()); LoadValues(); _view.Refresh(); StatusText.Text=$"{changed.Count} Änderungen wurden gespeichert."; }
        catch(Exception ex) { AppLogger.Error(ex,"Erweiterter Spielstand-Editor"); StatusText.Text=ex.Message; MessageBox.Show(this,ex.Message,"EasyCoop",MessageBoxButton.OK,MessageBoxImage.Error); }
    }
}
