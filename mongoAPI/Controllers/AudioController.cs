using System.Reflection.Metadata;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mongoAPI.Services;
using mongoAPI.Types;
using mongoAPI.Models;
using System.Security.Claims;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;

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
        private readonly MongoDBService _mongoDbService;

        public AudioController(S3Service s3Service, MongoDBService mongoDbService)
        {
            _s3Service = s3Service;
            _mongoDbService = mongoDbService;
        }

        [HttpPost("confirm-upload")]
        [Authorize]
        async public Task<IActionResult> CheckObjectUploaded([FromBody] CheckUploadRequest req)
        {
            var metadata = await _s3Service.GetObjectMetadata(req.ObjectKey);
            if (metadata == null)
            {
                return BadRequest("Object with provided key could not be found");
            }

            try
            {
                // get userId of account calling endpoint
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // find where the key and userId match
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, req.ObjectKey)
                & Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId);

                // TODO: set up so there's a probe to the S3 bucket to check length of the audio file
                // set to validated and update the missing fields
                var update = Builders<JournalEntry>.Update
                .Set(j => j.Validated, true)
                .Set(j => j.SizeInBytes, metadata.ContentLength);

                // get the collection and run the update
                var collection = _mongoDbService.GetJournalCollection();
                await collection.UpdateOneAsync(filter, update);
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong confirming file upload", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
            }
        }

        [HttpGet("url")]
        [Authorize]
        async public Task<IActionResult> GetPresignedS3Url([FromQuery] string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (!AllowedAudioExtensions.Contains(extension))
            {
                return BadRequest("File must be an audio file");
            }
            var url = _s3Service.GetPresignedUrl("bmoney", extension);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var collection = _mongoDbService.
            GetJournalCollection();
            // TODO: Check that the given name does not already exist and if it does then add a number to indicate that
            try
            {
                await collection.InsertOneAsync(new JournalEntry { ObjectKey = url.Key, FileName = fileName, UserId = userId, Uploaded = DateTime.UtcNow, Name = fileName });
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong creating the journal entry in mongo", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");

            }
            return Ok(url);
        }
    }
}
