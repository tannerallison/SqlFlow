using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using SqlFlow.Manager;
using SqlFlow.Views;

namespace SqlFlow.ViewModels;

public class ScriptFolderListViewModel : BaseViewModel
{
    private ObservableCollection<ScriptFolder> _scriptFolders;
    private ScriptFolder _selectedScriptFolder;

    public ObservableCollection<ScriptFolder> ScriptFolders
    {
        get => _scriptFolders;
        set => SetField(ref _scriptFolders, value);
    }

    public ScriptFolderListViewModel()
    {
        EditScriptFolderCommand = new RelayCommand<ScriptFolder>(EditScriptFolder);
        MoveScriptFolderUpCommand = new RelayCommand<ScriptFolder>(MoveScriptFolderUp);
        MoveScriptFolderDownCommand = new RelayCommand<ScriptFolder>(MoveScriptFolderDown);

        // Initialize the script folders
        ScriptFolders = new ObservableCollection<ScriptFolder>
        {
            new ScriptFolder("Path1", SearchOption.AllDirectories, "00|00|00"),
            new ScriptFolder("Path2", SearchOption.TopDirectoryOnly, "00|00|00"),
            new ScriptFolder("Path3", SearchOption.AllDirectories, "00|00|00")
        };
    }

    public static Brush BrushFromColor(string color)
    {
        return new SolidColorBrush(Utility.GetColorFromString(color));
    }

    public ScriptFolder SelectedScriptFolder
    {
        get => _selectedScriptFolder;
        set => SetField(ref _selectedScriptFolder, value);
    }

    public ICommand EditScriptFolderCommand { get; }
    public ICommand MoveScriptFolderUpCommand { get; }
    public ICommand MoveScriptFolderDownCommand { get; }

    private void MoveScriptFolderUp(ScriptFolder? scriptFolder)
    {
        ShiftFolder(scriptFolder, -1);
    }

    private void MoveScriptFolderDown(ScriptFolder? scriptFolder)
    {
        ShiftFolder(scriptFolder, 1);
    }

    private void ShiftFolder(ScriptFolder? scriptFolder, int shift)
    {
        if (scriptFolder is null)
            return;
        int i = ScriptFolders.IndexOf(scriptFolder);
        if (i + shift < 0 || i + shift > ScriptFolders.Count - 1)
            return;
        ScriptFolders.RemoveAt(i);
        ScriptFolders.Insert(i + shift, scriptFolder);
        SelectedScriptFolder = scriptFolder;
        OnPropertyChanged(nameof(ScriptFolder));
    }

    private void EditScriptFolder(ScriptFolder? scriptFolder)
    {
        if (scriptFolder is null)
            return;
        EditScriptFolderWindow editWindow = new EditScriptFolderWindow();
        var editViewModel = new EditScriptFolderViewModel(scriptFolder);
        editWindow.DataContext = editViewModel;
        editWindow.ShowDialog();
        OnPropertyChanged(nameof(ScriptFolder));
    }
}
