using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mongoAPI.Services;
using mongoAPI.Types;

namespace mongoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly S3Service _s3Service;

        public AudioController(S3Service s3Service)
        {
            _s3Service = s3Service;
        }


        [HttpPost("upload")]
        async public Task<IActionResult> UploadAudioFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }
            Console.WriteLine(file.FileName);
            Console.WriteLine(file.ToString());
            return Created();
        }

        [HttpPost("url")]
        async public Task<IActionResult> GetPresignedS3Url([FromBody] PresignedUrlRequest req)
        {
            var url = _s3Service.GetPresignedUrl("bmoney", req.ContentType);
            return Ok(url);
        }
    }
}
