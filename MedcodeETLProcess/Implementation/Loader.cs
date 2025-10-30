using MedcodeETLProcess.Contracts;
using MedcodeETLProcess.Model.MedcodeModel;

namespace MedcodeETLProcess.Implementation
{
    public class Loader(IMedcodeRepository<MedcodeDataMainModel> medcodeRepository) : ILoader<MedcodeDataMainModel>
    {
        private readonly IMedcodeRepository<MedcodeDataMainModel> _medcodeRepository = medcodeRepository;

        public async Task<int> LoadData(List<MedcodeDataMainModel> transformedData, string codeVersion)
        {
            try
            {
                var result = await _medcodeRepository.AddOrUpdateMedcodeData(transformedData, codeVersion);

                Console.WriteLine($"Successfully loaded {result} records.");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                throw;
            }
        }
    }
}
