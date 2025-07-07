using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

using ConverterApp.Models;
using System.Windows;

namespace ConverterApp.Services
{
    public static class ConfigService
    {
        private const string ConfigPath = "config.json";

        public static AppConfig LoadConfig()
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ??
                       throw new InvalidOperationException("Invalid configuration file");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}
