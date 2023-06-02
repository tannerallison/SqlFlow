using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SqlFlow.Manager;
using SqlFlow.ViewModels;

namespace SqlFlow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            MessageBoxResult result = SaveSettings();

            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.None)
                e.Cancel = true;
        }

        private Project _project;

        private const string SaveFileFilter = "JSON documents (.json)|*.json";


        private MessageBoxResult SaveSettings()
        {
            if (!_project.IsDirty)
                return MessageBoxResult.No;

            MessageBoxResult result = MessageBox.Show("Would you like to save your work before completing this action?",
                "Save Work?", MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            switch (result)
            {
                case MessageBoxResult.No:
                case MessageBoxResult.None:
                case MessageBoxResult.Cancel:
                    break;
                default:
                    SaveFile();
                    break;
            }

            return result;
        }

        private void SaveFile()
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".conv",
                FileName = _project.DirectoryPath,
                Filter = SaveFileFilter
            };

            if (dialog.FileName == " ")
                dialog.FileName = _project.Name;

            if (dialog.ShowDialog() == true)
            {
                _project.DirectoryPath = Path.GetDirectoryName(dialog.FileName);
                File.WriteAllText(dialog.FileName, Project.Serialize(_project));
                _project.IsDirty = false;
            }
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            Key realKey = (e.Key == Key.System
                ? e.SystemKey
                : e.Key);

            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return;

            switch (realKey)
            {
                case Key.N:
                    MenuItemNew_OnClick(sender, e);
                    e.Handled = true;
                    break;
                case Key.O:
                    MenuItemOpen_OnClick(sender, e);
                    e.Handled = true;
                    break;
                case Key.S:
                    MenuItemSave_OnClick(sender, e);
                    e.Handled = true;
                    break;
            }
        }

        private void MenuItemNew_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = SaveSettings();

            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.None)
                return;

            _project = new Project();
        }

        private void MenuItemOpen_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult results = SaveSettings();
            if (results == MessageBoxResult.Cancel || results == MessageBoxResult.None)
                return;
            OpenFile();
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog { DefaultExt = ".conv", Filter = SaveFileFilter };
            if (dialog.ShowDialog() ?? false)
                OpenMigration(dialog.FileName);
        }

        private void OpenMigration(string filePath)
        {
            if (File.Exists(filePath))
            {
                _project = Project.Deserialize(File.ReadAllText(filePath));
                _project.DirectoryPath = Path.GetDirectoryName(filePath) ?? "";
                _project.PopulateFromScriptFolders();
                Title =_project.Name;
                // if (_currentView != null)
                //     _currentView.RaiseEvent(new RoutedEventArgs(LoadedEvent));
            }
        }

        private void MenuItemSave_OnClick(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

    }
}
