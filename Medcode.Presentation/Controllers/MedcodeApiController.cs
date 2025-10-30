using Medcode.Presentation.Model;
using Microsoft.AspNetCore.Mvc;
using MedcodeETLProcess.Services;

namespace Medcode.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedcodeApiController : ControllerBase
    {
        private readonly ETLService _etlService;
        private readonly ILogger<MedcodeApiController> _logger;

        public MedcodeApiController(ETLService etlService, ILogger<MedcodeApiController> logger)
        {
            _etlService = etlService;
            _logger = logger;
        }


        [HttpPost("ExecuteETLBackground")]
        public async Task<IActionResult> ExecuteETLBackground([FromForm] Filter filter)
        {
            try
            {
                if (filter == null)
                {
                    return BadRequest("Invalid filter");
                }

                if (filter.MedcodeNewFile == null)
                {
                    return BadRequest("MedcodeNewFile is required.");
                }

                if (string.IsNullOrEmpty(filter.CodeType))
                {
                    return BadRequest("CodeType is required.");
                }

                if (string.IsNullOrEmpty(filter.CodeVersion))
                {
                    return BadRequest("CodeVersion is required.");
                }

                if (!filter.MinNumber.HasValue || filter.MinNumber < 1)
                {
                    filter.MinNumber = 1;
                }

                if (!filter.BatchSize.HasValue || filter.BatchSize <= 0)
                {
                    filter.BatchSize = 1000;
                }

                _logger.LogInformation($"Received ETL background request: CodeType={filter.CodeType}, CodeVersion={filter.CodeVersion}, BatchSize={filter.BatchSize}");
                byte[] newFileBytes = null;
                byte[] oldFileBytes = null;

                using (var ms = new MemoryStream())
                {
                    await filter.MedcodeNewFile.CopyToAsync(ms);
                    newFileBytes = ms.ToArray();
                }

                if (filter.MedcodeOldFile != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        await filter.MedcodeOldFile.CopyToAsync(ms);
                        oldFileBytes = ms.ToArray();
                    }
                }

                var etlRequest = new ETLRequest
                {
                    CodeType = filter.CodeType,
                    CodeVersion = filter.CodeVersion,
                    MinNumber = filter.MinNumber.Value,
                    BatchSize = filter.BatchSize.Value,
                    MedcodeNewFile = newFileBytes,
                    MedcodeOldFile = oldFileBytes
                };

                var requestId = _etlService.StartBackgroundETL(etlRequest);

                return Accepted(new { requestId });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ExecuteETLBackground: {ex.Message}", ex);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while starting the ETL background request",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}