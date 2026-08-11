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
        private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".aiff"
        };

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
            if (!AllowedAudioExtensions.Contains(Path.GetExtension(file.FileName)))
            {
                return BadRequest("File must be an audio file");
            }
            Console.WriteLine(file.FileName);
            Console.WriteLine(file.ToString());
            return Created();
        }

        [HttpGet("url")]
        async public Task<IActionResult> GetPresignedS3Url([FromQuery] string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (!AllowedAudioExtensions.Contains(extension))
            {
                return BadRequest("File must be an audio file");
            }
            var url = _s3Service.GetPresignedUrl("bmoney", extension);
            return Ok(url);
        }
    }
}
