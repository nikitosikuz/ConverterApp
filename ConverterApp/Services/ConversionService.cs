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
            var videoFormats = new[] { ".mp4", ".mkv" };
            var audioFormats = new[] { ".mp3", ".wav" };

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            var process = new Process();
            process.StartInfo.FileName = "ffmpeg";

            string args;
            if ((videoFormats.Contains(inputExt) || audioFormats.Contains(inputExt)) && outputExt == ".mp3")
            {
                args = $"-y -i \"{model.InputPath}\" -vn -acodec libmp3lame -q:a 2 \"{model.OutputPath}\"";
            }
            else if (videoFormats.Contains(inputExt) && videoFormats.Contains(outputExt))
            {
                var qualityPresets = new Dictionary<string, (string preset, int crf)>
                {
                    ["Low"] = ("veryfast", 30),
                    ["Medium"] = ("medium", 23),
                    ["High"] = ("slow", 18),
                    ["Very High"] = ("veryslow", 15)
                };

                var quality = qualityPresets.ContainsKey(model.QualityPreset ?? "")
                    ? qualityPresets[model.QualityPreset!]
                    : qualityPresets["Medium"];

                args = $"-y -i \"{model.InputPath}\" -c:v libx264 -preset {quality.preset} -crf {quality.crf} \"{model.OutputPath}\"";
            }
            else
            {
                args = $"-y -i \"{model.InputPath}\" \"{model.OutputPath}\"";
            }

            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            // Чтение вывода — чтобы не зависал
            process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }
}
