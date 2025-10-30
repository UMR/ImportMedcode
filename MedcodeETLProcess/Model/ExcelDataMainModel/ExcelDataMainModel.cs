namespace MedcodeETLProcess.Model.ExcelDataMainModel
{
    public class ExcelDataMainModel
    {
        public List<NewCodeDataModel> NewCodeDataModels { get; set; }
        public List<OldCodeDataUpdateModel> OldCodeDataModels { get; set; }
        public string ProcessingPhase { get; set; }
    }
}
