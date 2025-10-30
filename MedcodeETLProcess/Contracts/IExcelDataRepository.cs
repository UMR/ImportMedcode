using MedcodeETLProcess.Model.ExcelDataMainModel;

namespace MedcodeETLProcess.Contracts
{
    public interface IExcelDataRepository<T>
    {
        List<NewCodeDataModel> ExtractNewCodeData(IFilter filter);
        List<OldCodeDataUpdateModel> ExtractOldCodeData(IFilter filter);
        int GetTotalNewCodeRows(IFilter filter);
        int GetTotalOldCodeRows(IFilter filter);
    }
}
