using System;
using System.Windows.Input;
using SqlFlow.ViewModels;

namespace SqlFlow.Commands;

public class UpdateViewCommand : ICommand
{
    private MainViewModel _viewModel;

    public UpdateViewCommand(MainViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        switch (parameter.ToString())
        {
            case "Configuration":
                _viewModel.SelectedViewModel = new ConfigurationViewModel();
                break;
            case "Variables":
                _viewModel.SelectedViewModel = new VariableViewModel();
                break;
        }
    }

    public event EventHandler? CanExecuteChanged;
}
