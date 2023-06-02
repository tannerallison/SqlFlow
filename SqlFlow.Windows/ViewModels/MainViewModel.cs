using System.Windows.Input;
using SqlFlow.Commands;
using SqlFlow.Manager;

namespace SqlFlow.ViewModels;

public class MainViewModel : BaseViewModel
{
    private Project _model = new Project();
    private BaseViewModel _selectedViewModel = new ConfigurationViewModel();

    public MainViewModel()
    {
        UpdateViewCommand = new UpdateViewCommand(this);
    }

    public Project Model
    {
        get => _model;
        set => _model = value;
    }

    public BaseViewModel SelectedViewModel
    {
        get => _selectedViewModel;
        set => SetField(ref _selectedViewModel, value);
    }

    public ICommand UpdateViewCommand { get; set; }
}
