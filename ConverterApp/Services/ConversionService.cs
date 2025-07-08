using ConverterApp.Models;
using GemBox.Document;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace ConverterApp.Services
{
    public class ConversionService
    {
        private readonly AppConfig _config;

        public ConversionService(AppConfig config)
        {
            _config = config;
        }

        public void Convert(ConversionModel model)
        {
            string ext = Path.GetExtension(model.InputPath).ToLower();

            string inputExt = Path.GetExtension(model.InputPath).ToLower();
            string outputExt = Path.GetExtension(model.OutputPath).ToLower();

            if (!_config.AllowedFormats.ContainsKey(ext))
                throw new NotSupportedException("Неподдерживаемый формат");

            switch (true)
            {
                case bool _ when _config.VideoFormats.Contains(inputExt) || _config.AudioFormats.Contains(inputExt):
                    ConvertMedia(model);
                    break;

                case bool _ when IsSupported(inputExt):
                    ConvertDocument(model);
                    break;

                default:
                    ConvertImage(model);
                    break;
            }
        }

        private void ConvertDocument(ConversionModel model)
        {
            ExecuteWithExceptionHandling(() =>
            {
                ComponentInfo.SetLicense("FREE-LIMITED-KEY");

                string inputExt = Path.GetExtension(model.InputPath).ToLower();
                string outputExt = Path.GetExtension(model.OutputPath).ToLower();

                if (!IsSupported(inputExt) || !IsSupported(outputExt))
                    throw new NotSupportedException("Формат не поддерживается");

                var document = DocumentModel.Load(model.InputPath);

                switch (outputExt)
                {
                    case ".pdf":
                        document.Save(model.OutputPath, SaveOptions.PdfDefault);
                        break;

                    case ".docx":
                        document.Save(model.OutputPath, SaveOptions.DocxDefault);
                        break;

                    case ".txt":
                        document.Save(model.OutputPath, SaveOptions.TxtDefault);
                        break;

                    default:
                        throw new NotSupportedException("Неподдерживаемый формат вывода");
                }
            });
        }

        private bool IsSupported(string ext) => ext switch
        {
            ".docx" or ".doc" or ".txt" or ".pdf" => true,
            _ => false
        };

        private void ConvertImage(ConversionModel model)
        {
            ExecuteWithExceptionHandling(() =>
            {
                 using var image = System.Drawing.Image.FromFile(model.InputPath);
                image.Save(model.OutputPath, GetImageFormat(model.OutputFormat));
            });
        }

        private ImageFormat GetImageFormat(string ext) => ext.ToLower() switch
        {
            ".png" => ImageFormat.Png,
            ".bmp" => ImageFormat.Bmp,
            ".jpeg" or ".jpg" => ImageFormat.Jpeg,
            ".gif" => ImageFormat.Gif,
            _ => throw new NotSupportedException("Неподдерживаемое преобразование изображения")
        };

        public void ConvertMedia(ConversionModel model)
        {
            ExecuteWithExceptionHandling(() =>
            {
                var inputExt = Path.GetExtension(model.InputPath).ToLower();
                var outputExt = Path.GetExtension(model.OutputPath).ToLower();

                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
                var process = new Process();
                process.StartInfo.FileName = ffmpegPath;

                string args;
                switch (true)
                {
                    case var _ when (_config.VideoFormats.Contains(inputExt) || _config.AudioFormats.Contains(inputExt)) && outputExt == ".mp3":
                        args = $"-y -i \"{model.InputPath}\" -vn -acodec libmp3lame -q:a 2 \"{model.OutputPath}\"";
                        break;

                    case var _ when _config.VideoFormats.Contains(inputExt) && _config.VideoFormats.Contains(outputExt):
                        var quality = _config.QualityPresets.TryGetValue(model.QualityPreset ?? "Medium", out var preset)
                            ? preset
                            : _config.QualityPresets["Medium"];
                        args = $"-y -i \"{model.InputPath}\" -c:v libx264 -preset {quality.Preset} -crf {quality.Crf} \"{model.OutputPath}\"";
                        break;

                    case var _ when _config.AudioFormats.Contains(inputExt) && _config.AudioFormats.Contains(outputExt):
                        args = $"-y -i \"{model.InputPath}\" \"{model.OutputPath}\"";
                        break;

                    default:
                        throw new NotSupportedException("Неподдерживаемое преобразование мультимедиа");
                }

                process.StartInfo.Arguments = args;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            });
        }
        private void ExecuteWithExceptionHandling(Action conversionAction)
        {
            try
            {
                conversionAction();
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Файл повреждён или имеет неподдерживаемый формат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при конвертации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}
