using ConverterApp.Models;
using ConverterApp.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace ConverterApp
{

    public partial class MainWindow : Window
    {
        private readonly ConversionService _service = new();

        private static readonly Dictionary<string, string[]> AllowedFormats = new()
        {
            [".docx"] = [".pdf", ".txt", ".docx"],
            [".txt"] = [".pdf", ".docx", ".txt"],
            [".pdf"] = [".docx", ".txt", ".pdf"],
            [".jpg"] = [".png", ".bmp", ".gif", ".jpg"],
            [".jpeg"] = [".png", ".bmp", ".gif", ".jpeg"],
            [".png"] = [".jpg", ".bmp", ".gif", ".png"],
            [".bmp"] = [".jpg", ".png", ".gif", ".bmp"],
            [".gif"] = [".jpg", ".png", ".gif", ".bmp"],
            [".mp3"] = [".wav", ".mp3"],
            [".wav"] = [".mp3", ".wav"],
            [".mp4"] = [".mkv", ".mp4", ".mp3"],
            [".mkv"] = [".mp4", ".mkv", ".mp3"],
        };

        private static readonly Dictionary<string, (string preset, int crf)> QualityPresets = new()
        {
            ["Low"] = ("veryfast", 30),
            ["Medium"] = ("medium", 23),
            ["High"] = ("slow", 18),
            ["Very High"] = ("veryslow", 15)
        };

        public MainWindow() => InitializeComponent();

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                InputPathBox.Text = dlg.FileName;
                string ext = Path.GetExtension(dlg.FileName).ToLower();
                FormatBox.Items.Clear();

                if (AllowedFormats.TryGetValue(ext, out var formats))
                {
                    foreach (var format in formats)
                        FormatBox.Items.Add(format);

                    FormatBox.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("Формат не поддерживается.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Показать или скрыть пресеты
                if (new[] { ".mp4", ".avi" }.Contains(ext))
                {
                    QualityComboBox.Items.Clear();
                    foreach (var key in QualityPresets.Keys)
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
                return;
            }

            string output = Path.ChangeExtension(input, outputExt);
            string? quality = null;

            if (QualityComboBox.Visibility == Visibility.Visible && QualityComboBox.SelectedItem is ComboBoxItem item)
                quality = item.Content.ToString();

            try
            {
                // Запуск конвертации в фоновом потоке
                await Task.Run(() =>
                {
                    _service.Convert(new ConversionModel
                    {
                        InputPath = input,
                        OutputPath = output,
                        OutputFormat = outputExt,
                        QualityPreset = quality
                    });
                });

                MessageBox.Show("Конвертация завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string inputExt = Path.GetExtension(InputPathBox.Text).ToLower();
            string? selectedOutputExt = FormatBox.SelectedItem?.ToString()?.ToLower();

            var videoFormats = new[] { ".mp4", ".mkv", ".avi" };

            // Показываем QualityComboBox, если вход — видео и выход не mp3
            if (videoFormats.Contains(inputExt) && selectedOutputExt != ".mp3")
            {
                QualityComboBox.Items.Clear();
                foreach (var key in QualityPresets.Keys)
                    QualityComboBox.Items.Add(key);
                QualityComboBox.SelectedIndex = 1; // Medium
                QualityComboBox.Visibility = Visibility.Visible;
            }
            else
            {
                QualityComboBox.Visibility = Visibility.Collapsed;
            }
        }
    }
}