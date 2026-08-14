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
using System.Numerics;
using MongoDB.Bson;

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
        private readonly DeepgramService _deepgramService;

        public AudioController(S3Service s3Service, MongoDBService mongoDbService, DeepgramService deepgramService)
        {
            _s3Service = s3Service;
            _mongoDbService = mongoDbService;
            _deepgramService = deepgramService;
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
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (username == null) { throw new Exception("Username in JWT was null"); }
            var url = _s3Service.GetPresignedUrlPut(username, extension);
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

        [HttpGet("most-recent-entries")]
        [Authorize]
        async public Task<IActionResult> GetMostRecentJournalEntries([FromQuery] int numOfEntries)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var collection = _mongoDbService.GetJournalCollection();
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.Validated, true)
                    & Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId);
                var sort = Builders<JournalEntry>.Sort.Descending(j => j.Uploaded);
                var entries = await collection.Find(filter).Sort(sort).Limit(numOfEntries).ToListAsync();
                return Ok(entries);
            }
            catch (System.Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
            }
        }

        [HttpPost("transcribe")]
        [Authorize]
        public async Task<IActionResult> TranscribeAudio([FromBody] TranscribeAudioRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId) & Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, req.ObjectKey);
            try
            {
                var username = User.FindFirstValue(ClaimTypes.Name);
                var collection = _mongoDbService.GetJournalCollection();
                var entry = await collection.Find(filter).ToListAsync();
                if (entry.Count == 0)
                {
                    return BadRequest("No object with that key belonging to this user was found");
                }
                else if (entry.First().Transcribed != TranscriptionStatus.NotTranscribed)
                {
                    return BadRequest("File has already been transcribed");
                }
                // set entry to transcribing
                var update = Builders<JournalEntry>.Update.Set(j => j.Transcribed, TranscriptionStatus.Transcribing);
                await collection.UpdateOneAsync(filter, update);

                var url = _s3Service.GetPresignedUrlGet(req.ObjectKey);

                var transcript = await _deepgramService.TranscribeFileAsync(url);
                if (transcript.Results.Summary.Result == "success")
                {
                    // set as transcribed
                    update = Builders<JournalEntry>.Update
                    .Set(j => j.Transcribed, TranscriptionStatus.Transcribed)
                    .Set(j => j.Transcription, transcript.Results.Channels[0].Alternatives[0].Transcript)
                    .Set(j => j.Summary, transcript.Results.Summary.Short);
                    await collection.UpdateOneAsync(filter, update);
                    var doc = await collection.FindAsync(filter);
                    return Ok(doc.FirstOrDefault());
                }
                else
                {
                    throw new Exception("The transcript did not succeed");
                }

            }
            catch (Exception e)
            {
                var collection = _mongoDbService.GetJournalCollection();
                var update = Builders<JournalEntry>.Update.Set(j => j.Transcribed, TranscriptionStatus.NotTranscribed);
                await collection.UpdateOneAsync(filter, update);
                Console.WriteLine("Something went wrong transcribing an audio file", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
            }
        }
    }
}
