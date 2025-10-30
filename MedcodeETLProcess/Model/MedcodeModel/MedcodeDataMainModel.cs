namespace MedcodeETLProcess.Model.MedcodeModel
{
    public class MedcodeDataMainModel
    {
        public MedicalCode MedicalCode { get; set; }
        public MedicalCodesAction MedicalCodesAction { get; set; }
        public MedicalCodeHistory MedicalCodeHistory { get; set; }
        public MedcodeActionHistory MedicalCodeActionHistory { get; set; }
        public MedcodepediaHistoryDBMedcode MedcodepediaHistoryDBMedcode { get; set; }
    }
}
