using System.Collections.Generic;
using System.Collections.ObjectModel;
using SqlFlow.Manager;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SqlFlow.Views;

namespace SqlFlow.ViewModels;

public class ConfigurationViewModel : BaseViewModel
{
    private ObservableCollection<ScriptFolder> _scriptFolders;

    public ObservableCollection<ScriptFolder> ScriptFolders
    {
        get => _scriptFolders;
        set => SetField(ref _scriptFolders, value);
    }

    public ConfigurationViewModel()
    {
    }
}
