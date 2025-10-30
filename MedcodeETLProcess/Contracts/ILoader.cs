namespace MedcodeETLProcess.Contracts
{
    public interface ILoader<T>
    {
        Task<int> LoadData(List<T> transformedData, string codeVersion);
    }
}
