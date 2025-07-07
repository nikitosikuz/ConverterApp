using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConverterApp.Models
{
    public class AppConfig
    {
        public Dictionary<string, string[]> AllowedFormats { get; set; }
        public Dictionary<string, QualityPreset> QualityPresets { get; set; }
        public string[] VideoFormats { get; set; }
    }

    public class QualityPreset
    {
        public string Preset { get; set; }
        public int Crf { get; set; }
    }
}
