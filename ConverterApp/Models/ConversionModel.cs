using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConverterApp.Models
{
    public class ConversionModel
    {
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string OutputFormat { get; set; } = "";
        public string? QualityPreset { get; set; }
        public int Crf { get; set; }
    }
}
