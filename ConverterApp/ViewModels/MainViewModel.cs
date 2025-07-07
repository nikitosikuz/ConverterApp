using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using ConverterApp.Models;
using ConverterApp.Services;
using System.IO;

namespace ConverterApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ConversionService _service;
        private string _inputPath;
        private string _outputFormat;

        public string InputPath
        {
            get => _inputPath;
            set { _inputPath = value; OnPropertyChanged(); }
        }

        public string OutputFormat
        {
            get => _outputFormat;
            set { _outputFormat = value; OnPropertyChanged(); }
        }

        public ICommand BrowseCommand => new RelayCommand(_ =>
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true) InputPath = dlg.FileName;
        });

        public ICommand ConvertCommand => new RelayCommand(_ =>
        {
            var outputPath = Path.ChangeExtension(InputPath, OutputFormat);
            _service.Convert(new ConversionModel
            {
                InputPath = InputPath,
                OutputPath = outputPath,
                OutputFormat = OutputFormat
            });
        });

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;

        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
    }
}
