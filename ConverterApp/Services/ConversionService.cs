using ConverterApp.Models;
using GemBox.Document;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

            if (!_config.AllowedFormats.ContainsKey(ext))
                throw new NotSupportedException("Unsupported format");

            if (ext == ".docx" || ext == ".txt" || ext == ".pdf")
                ConvertDocument(model);
            else if (ext == ".jpg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".jpeg")
                ConvertImage(model);
            else if (ext == ".mp4" || ext == ".mkv" || ext == ".mp3" || ext == ".wav")
                ConvertMedia(model);
            else
                throw new NotSupportedException("Unsupported format");
        }

        private void ConvertDocument(ConversionModel model)
        {
            ComponentInfo.SetLicense("FREE-LIMITED-KEY");

            string inputExt = Path.GetExtension(model.InputPath).ToLower();
            string outputExt = Path.GetExtension(model.OutputPath).ToLower();

            if (!IsSupported(inputExt) || !IsSupported(outputExt))
                throw new NotSupportedException("Формат не поддерживается");

            var document = DocumentModel.Load(model.InputPath);

            if (outputExt == ".pdf")
                document.Save(model.OutputPath, SaveOptions.PdfDefault);
            else if (outputExt == ".docx")
                document.Save(model.OutputPath, SaveOptions.DocxDefault);
            else if (outputExt == ".txt")
                document.Save(model.OutputPath, SaveOptions.TxtDefault);
            else
                throw new NotSupportedException("Неподдерживаемый формат вывода");
        }

        private bool IsSupported(string ext) => ext switch
        {
            ".docx" or ".doc" or ".txt" or ".pdf" => true,
            _ => false
        };

        private void ConvertImage(ConversionModel model)
        {
            using var image = System.Drawing.Image.FromFile(model.InputPath);
            image.Save(model.OutputPath, GetImageFormat(model.OutputFormat));
        }

        private ImageFormat GetImageFormat(string ext) => ext.ToLower() switch
        {
            ".png" => ImageFormat.Png,
            ".bmp" => ImageFormat.Bmp,
            ".jpeg" or ".jpg" => ImageFormat.Jpeg,
            ".gif" => ImageFormat.Gif,
            _ => throw new NotSupportedException("Unsupported image format")
        };

        public void ConvertMedia(ConversionModel model)
        {
            var inputExt = Path.GetExtension(model.InputPath).ToLower();
            var outputExt = Path.GetExtension(model.OutputPath).ToLower();

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            var process = new Process();
            process.StartInfo.FileName = ffmpegPath;

            string args;
            if ((_config.VideoFormats.Contains(inputExt) || _config.AudioFormats.Contains(inputExt)) && outputExt == ".mp3")
            {
                args = $"-y -i \"{model.InputPath}\" -vn -acodec libmp3lame -q:a 2 \"{model.OutputPath}\"";
            }
            else if (_config.VideoFormats.Contains(inputExt) && _config.VideoFormats.Contains(outputExt))
            {
                var quality = _config.QualityPresets.TryGetValue(model.QualityPreset ?? "Medium", out var preset)
                    ? preset
                    : _config.QualityPresets["Medium"];

                args = $"-y -i \"{model.InputPath}\" -c:v libx264 -preset {quality.Preset} -crf {quality.Crf} \"{model.OutputPath}\"";
            }
            else if (_config.AudioFormats.Contains(inputExt) && _config.AudioFormats.Contains(outputExt))
            {
                args = $"-y -i \"{model.InputPath}\" \"{model.OutputPath}\"";
            }
            else
            {
                throw new NotSupportedException("Unsupported media conversion");
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
        }
    }
}
