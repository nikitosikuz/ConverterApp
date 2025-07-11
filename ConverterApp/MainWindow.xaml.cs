﻿using ConverterApp.Models;
using ConverterApp.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ConverterApp
{
    public partial class MainWindow : Window
    {
        private readonly AppConfig _config;

        private readonly ConversionService _service;

        private readonly EventLog _eventLog;

        public MainWindow()
        {
            InitializeComponent();
            _config = ConfigService.LoadConfig();
            _service = new ConversionService(_config);

            _eventLog = new EventLog("Application");
            if (!EventLog.SourceExists("ConverterApp"))
                EventLog.CreateEventSource("ConverterApp", "Application");

            _eventLog.Source = "ConverterApp";
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                _eventLog.WriteEntry($"Пользователь выбрал файл: {dlg.FileName}", EventLogEntryType.Information);
                InputPathBox.Text = dlg.FileName;
                string ext = Path.GetExtension(dlg.FileName).ToLower();
                FormatBox.Items.Clear();

                if (_config.AllowedFormats.TryGetValue(ext, out var formats))
                {
                    foreach (var format in formats)
                        FormatBox.Items.Add(format);

                    FormatBox.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Формат не поддерживается.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _eventLog.WriteEntry($"Формат не поддерживается: {ext}", EventLogEntryType.Warning);
                    return;
                }

                // Показать или скрыть пресеты
                if (_config.VideoFormats.Contains(ext))
                {
                    QualityComboBox.Items.Clear();
                    foreach (var key in _config.QualityPresets.Keys)
                        QualityComboBox.Items.Add(key);
                    QualityComboBox.SelectedIndex = 1; // Default: Medium
                    QualityComboBox.Visibility = Visibility.Visible;
                }
                else
                {
                    QualityComboBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            string input = InputPathBox.Text;
            string outputExt = FormatBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(outputExt))
            {
                MessageBox.Show("Выберите файл и формат.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                _eventLog.WriteEntry("Попытка запуска конвертации без указания пути или формата.", EventLogEntryType.Warning);
                return;
            }

            string output = Path.ChangeExtension(input, outputExt);
            string? quality = null;

            if (QualityComboBox.Visibility == Visibility.Visible && QualityComboBox.SelectedItem is string selectedQuality)
            {
                quality = selectedQuality;
            }
            _eventLog.WriteEntry($"Запуск конвертации: {input} → {output}", EventLogEntryType.Information);
            try
            {
                await Task.Run(() =>
                {
                    var model = new ConversionModel
                    {
                        InputPath = input,
                        OutputPath = output,
                        OutputFormat = outputExt
                    };

                    if (quality != null && _config.QualityPresets.TryGetValue(quality, out var preset))
                    {
                        model.QualityPreset = preset.Preset;
                        model.Crf = preset.Crf;
                    }

                    _service.Convert(model);
                });

                MessageBox.Show("Конвертация завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                _eventLog.WriteEntry($"Успешная конвертация: '{input}' → '{output}'", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _eventLog.WriteEntry($"Ошибка при конвертации: {ex}", EventLogEntryType.Error);
            }
        }

        private void FormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string inputExt = Path.GetExtension(InputPathBox.Text).ToLower();
            string? selectedOutputExt = FormatBox.SelectedItem?.ToString()?.ToLower();
            _eventLog.WriteEntry($"Выбран выходной формат: {selectedOutputExt}", EventLogEntryType.Information);
            // Показываем QualityComboBox, если вход — видео и выход не mp3
            if (_config.VideoFormats.Contains(inputExt) && selectedOutputExt != ".mp3")
            {
                QualityComboBox.Items.Clear();
                foreach (var key in _config.QualityPresets.Keys)
                    QualityComboBox.Items.Add(key);
                QualityComboBox.SelectedIndex = 1; // Medium
                QualityComboBox.Visibility = Visibility.Visible;
            }
            else
            {
                QualityComboBox.Visibility = Visibility.Collapsed;
            }
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _eventLog.WriteEntry("Приложение закрыто пользователем.", EventLogEntryType.Information);
            this.Close();
        }
    }
}