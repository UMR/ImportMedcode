namespace MedcodeETLProcess.Contracts
{
    public interface ITransformer<TInput, TOutput>
    {
        Task<List<TOutput>> TransformData(List<TInput> rawData, IFilter filter);
    }
}
