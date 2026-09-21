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
using System.Runtime.CompilerServices;

namespace mongoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".aiff", ".webm"
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("UserId could not be found in JWT");

                // find where the key and userId match
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, req.ObjectKey)
                & Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId);

                // TODO: set up so there's a probe to the S3 bucket to check length of the audio file
                // set to validated and update the missing fields
                var update = Builders<JournalEntry>.Update
                .Set(j => j.Validated, true)
                .Set(j => j.SizeInBytes, metadata.SizeInBytes)
                .Set(j => j.SecLength, metadata.SecLength)
                .Set(j => j.Title, req.Title)
                .Set(j => j.Description, req.Description ?? "");

                // get the collection and run the update
                var collection = _mongoDbService.GetJournalCollection();
                await collection.UpdateOneAsync(filter, update);
                if (req.Transcribe == true) await Transcribe(req.ObjectKey, userId);
                return Created();
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong confirming file upload", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
            }
        }

        [HttpGet("post-url")]
        [Authorize]
        async public Task<IActionResult> GetPresignedPostS3Url([FromQuery] string fileName)
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

        [HttpGet("get-url")]
        [Authorize]
        async public Task<IActionResult> GetPresignedGetS3Url([FromQuery] string objectKey)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("UserId not found in JWT token");
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId) & Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, objectKey);
                var collection = _mongoDbService.GetJournalCollection();
                var entry = await collection.Find(filter).ToListAsync();
                if (entry.Count == 0)
                {
                    return BadRequest("No object with that key belonging to this user was found");
                }
                var url = _s3Service.GetPresignedUrlGet(objectKey);
                return Ok(url);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet("most-recent-entries")]
        [Authorize]
        async public Task<IActionResult> GetMostRecentJournalEntries([FromQuery] int numOfEntries)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("UserId could not be found in JWT");
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

        [HttpGet("entries")]
        [Authorize]
        async public Task<IActionResult> GetAudioEntries()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("UserId could not be found in JWT");
            var collection = _mongoDbService.GetJournalCollection();
            var filter = Builders<JournalEntry>.Filter.Eq(j => j.Validated, true) & Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId);
            var entries = await collection.Find(filter).SortByDescending(j => j.Uploaded).Project(j => new
            {
                j.Id,
                j.ObjectKey,
                j.Title,
                j.Description,
                j.Uploaded,
                j.Favorite,
                j.SecLength,
                j.WordCount,
                j.SizeInBytes,
                j.Summary,
                j.Transcribed,
                Transcription = (j.Transcription ?? "").Substring(0, 200)
            }).ToListAsync();

            return Ok(entries);
        }

        [HttpPost("transcribe")]
        [Authorize]
        public async Task<IActionResult> TranscribeAudio([FromBody] AudioObjectKey req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var username = User.FindFirstValue(ClaimTypes.Name);
                return await Transcribe(req.ObjectKey, userId);
            }
            catch (Exception e)
            {
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId) & Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, req.ObjectKey);
                var collection = _mongoDbService.GetJournalCollection();
                var update = Builders<JournalEntry>.Update.Set(j => j.Transcribed, TranscriptionStatus.NotTranscribed);
                await collection.UpdateOneAsync(filter, update);
                Console.WriteLine("Something went wrong transcribing an audio file", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
            }
        }

        [HttpDelete("delete-entry")]
        [Authorize]
        public async Task<IActionResult> DeleteEntry([FromHeader(Name = "ObjectKey")] string ObjectKey)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var collection = _mongoDbService.GetJournalCollection();
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId) & Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, ObjectKey);
                var entry = await collection.Find(filter).FirstOrDefaultAsync();
                if (entry == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest, "User has no object with that key");
                }
                // make a request to delete the object from the S3 bucket
                var succeeded = await _s3Service.DeleteObject(ObjectKey);
                if (succeeded == false)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
                }
                await collection.DeleteOneAsync(filter);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong deleting an entry", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
            }
        }

        [HttpPost("favorite")]
        [Authorize]
        public async Task<IActionResult> FlipFavorite([FromBody] JournalEntryId req)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("UserId could not be found JWT token");
                var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId) & Builders<JournalEntry>.Filter.Eq(j => j.Id, req.Id);
                var collection = _mongoDbService.GetJournalCollection();
                var favorite = await collection.Find(filter).FirstOrDefaultAsync();
                if (favorite == null) return BadRequest("No entry with that UserId and JournalEntryId could be found");

                var update = Builders<JournalEntry>.Update.Set(j => j.Favorite, !favorite.Favorite);
                await collection.UpdateOneAsync(filter, update);
                return NoContent();
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong flipping favorite field", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong flipping favorite field");
            }

        }

        private async Task<IActionResult> Transcribe(string ObjectKey, string UserId)
        {
            var filter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, UserId) & Builders<JournalEntry>.Filter.Eq(j => j.ObjectKey, ObjectKey);
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

            var url = _s3Service.GetPresignedUrlGet(ObjectKey);

            var transcript = await _deepgramService.TranscribeFileAsync(url);
            if (transcript.Results.Summary.Result == "success")
            {
                // set as transcribed
                update = Builders<JournalEntry>.Update
                .Set(j => j.Transcribed, TranscriptionStatus.Transcribed)
                .Set(j => j.Transcription, transcript.Results.Channels[0].Alternatives[0].Transcript)
                .Set(j => j.WordCount, transcript.Results.Channels[0].Alternatives[0].Words.Count)
                .Set(j => j.Summary, transcript.Results.Summary.Short);
                await collection.UpdateOneAsync(filter, update);
                var doc = await collection.FindAsync(filter);
                var userCollection = _mongoDbService.GetUserCollection();
                await userCollection.UpdateOneAsync(u => u.Id == UserId, Builders<User>.Update.Inc(u => u.TranscriptionsLeft, -1));
                return Ok(doc.FirstOrDefault());
            }
            else
            {
                throw new Exception("The transcript did not succeed");
            }
        }
    }
}
