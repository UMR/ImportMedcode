using MedcodeETLProcess.Contracts;

namespace MedcodeETLProcess.Implementation
{
    internal class Filter : IFilter
    {
        public int MinNumber { get; set; }
        public int MaxNumber { get; set; }
        public int BatchSize { get; set; }
        public string CodeType { get; set; }
        public byte[] MedcodeNewFile { get; set; }
        public byte[] MedcodeOldFile { get; set; }
        public string ProcessingPhase { get; set; }
        public HashSet<string> ProcessedNewCodes { get; set; } = [];
        public string CodeVersion { get; set; }
        public string RequestId { get; set; }
    }
}
