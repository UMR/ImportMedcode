namespace Medcode.Presentation.Model
{
    public class Filter
    {
        public int? MinNumber { get; set; }
        public int? BatchSize { get; set; }
        public string CodeType { get; set; }
        public IFormFile MedcodeNewFile { get; set; }
        public IFormFile MedcodeOldFile { get; set; }
        public string CodeVersion { get; set; }
    }
}