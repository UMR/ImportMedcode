namespace MedcodeETLProcess.Contracts
{
    public interface IFilter
    {
        int MinNumber { get; set; }
        int MaxNumber { get; set; }
        int BatchSize { get; set; }
        string CodeType { get; set; }
        byte[] MedcodeNewFile { get; set; }
        byte[] MedcodeOldFile { get; set; }
        string ProcessingPhase { get; set; }
        HashSet<string> ProcessedNewCodes { get; set; }
        string CodeVersion { get; set; }
        string RequestId { get; set; }
    }
}
