using MedcodeETLProcess.Model.MedcodeModel;

namespace MedcodeETLProcess.Contracts
{
    public interface IMedcodeRepository<TEntity>
    {
        Task<int> AddOrUpdateMedcodeData(List<MedcodeDataMainModel> medcodeData, string CodeVersion);
        Task<MedicalCode> GetMedcodeByCode(string code);
        Task<bool> MedcodeExists(string code);
        Task<HashSet<string>> GetExistingCodes(List<string> codes);
    }
}
