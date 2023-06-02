using System.Windows.Media;
using System.IO;
using System.Linq;
using SqlFlow.Manager;

namespace SqlFlow.ViewModels;

public class EditScriptFolderViewModel : BaseViewModel
{
    private readonly ScriptFolder _scriptFolder;

    public EditScriptFolderViewModel(ScriptFolder scriptFolder)
    {
        _scriptFolder = scriptFolder;
    }

    public string Path
    {
        get => _scriptFolder.Path;
        set
        {
            _scriptFolder.Path = value;
            OnPropertyChanged();
        }
    }

    public SearchOption SearchOption
    {
        get => _scriptFolder.SearchOption;
        set
        {
            _scriptFolder.SearchOption = value;
            OnPropertyChanged();
        }
    }

    public Color Color
    {
        get => Utility.GetColorFromString(_scriptFolder.Color);
        set
        {
            _scriptFolder.Color = $"{value.R}|{value.G}|{value.B}";
            OnPropertyChanged();
        }
    }
}
