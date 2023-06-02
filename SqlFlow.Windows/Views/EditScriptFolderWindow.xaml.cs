using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Ookii.Dialogs.Wpf;
using SqlFlow.Manager;
using SqlFlow.ViewModels;

namespace SqlFlow.Views;

public partial class EditScriptFolderWindow : Window
{
    public EditScriptFolderWindow()
    {
        InitializeComponent();
        SearchOptionBox.ItemsSource = Enum.GetValues(typeof(SearchOption));
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog();
        if (dialog.ShowDialog() ?? false)
        {
            // Set the selected directory path to the Path property
            ((EditScriptFolderViewModel)DataContext).Path = dialog.SelectedPath;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Perform any validation or additional logic before saving
        // For simplicity, this example assumes the data is valid

        // Close the window and return DialogResult.OK
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Close the window without saving and return DialogResult.Cancel
        DialogResult = false;
    }

    private void ClrPcker_Background_SelectedColorChanged(object sender,
        RoutedPropertyChangedEventArgs<Color?> routedPropertyChangedEventArgs)
    {
        ((EditScriptFolderViewModel)DataContext).Color = ClrPckerBackground.SelectedColor ?? Colors.Black;
    }
}
