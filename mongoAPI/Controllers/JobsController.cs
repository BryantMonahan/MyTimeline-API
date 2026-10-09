using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mongoAPI.Services;

namespace mongoAPI.Controllers
{
    [ApiController]
    [Route("api/job")]
    public class JobsController : ControllerBase
    {
        private readonly MongoDBService _mongoDbService;
        private readonly ILogger<AudioController> _logger;

        public JobsController(S3Service s3Service, MongoDBService mongoDbService, ILogger<AudioController> logger)
        {
            _mongoDbService = mongoDbService;
            _logger = logger;
        }

        [HttpGet("job-status")]
        [Authorize]
        public async Task<IActionResult> GetJobStatus([FromQuery] string jobId)
        {
            try
            {
                IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();

                JobDetailsDto jobDetails = monitoringApi.JobDetails(jobId);

                if (jobDetails == null)
                {
                    return BadRequest("No job with that jobId could be found");
                }

                // Return the latest state name (e.g., Enqueued, Processing, Succeeded, Failed)
                var state = jobDetails.History.LastOrDefault()?.StateName ?? "Unknown";
                if (state == "Unknown")
                {
                    throw new Exception("Job exists with unknown state");
                }
                return Ok(new { state });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something went wrong checking the job status for {jobId}", jobId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong checking the job status");
            }

        }

    }
}