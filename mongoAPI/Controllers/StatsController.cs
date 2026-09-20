using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mongoAPI.Models;
using mongoAPI.Services;
using mongoAPI.Types;
using MongoDB.Driver;

namespace mongoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly MongoDBService _mondoDBService;

        public StatsController(MongoDBService mongoDBService)
        {
            _mondoDBService = mongoDBService;
        }

        [HttpGet("past-seven-days")]
        [Authorize]
        public async Task<IActionResult> GetDaysUploadedThisWeek([FromQuery] UploadedDaysRequest req)
        {
            // determine the user's time
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine(userId);
            var startTime = DateTime.UtcNow.AddMinutes(-1 * req.MinutesPastMidnight!.Value);
            var endTime = startTime.AddDays(1);
            List<bool> daysUsed = new List<bool>();
            var collection = _mondoDBService.GetJournalCollection();
            for (int i = 1; i <= 7; i++)
            {
                // create the filter that checks for a entry in a 24hr period
                var filter = Builders<JournalEntry>.Filter.Gte(j => j.Uploaded, startTime.ToUniversalTime()) &
                Builders<JournalEntry>.Filter.Lte(j => j.Uploaded, endTime.ToUniversalTime()) & Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId);

                // check for entry and see if it exists
                var entry = await collection.Find(filter).Limit(1).ToListAsync();
                daysUsed.Add(entry.Count != 0);

                // check the previous 24hr period
                startTime = startTime.AddDays(-1);
                endTime = endTime.AddDays(-1);
            }
            return Ok(daysUsed);
        }

        [HttpGet("stats-bar")]
        [Authorize]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("No userId found in JWT");
                var collection = _mondoDBService.GetJournalCollection();
                var generalFilter = Builders<JournalEntry>.Filter.Eq(j => j.UserId, userId) & Builders<JournalEntry>.Filter.Eq(j => j.Validated, true);
                var totalEntries = await collection.CountDocumentsAsync(generalFilter);
                var totalAggregated = await collection.Aggregate().Match(generalFilter).Group(t => t.UserId,
                g => new
                {
                    SecondTotal = g.Sum(j => j.SecLength),
                    TranscribedTotal = g.Sum(j => j.Transcribed == TranscriptionStatus.Transcribed ? 1 : 0),
                    BytesUsed = g.Sum(j => j.SizeInBytes),
                    WordsTotal = g.Sum(j => j.WordCount),
                    LongestEntrySeconds = g.Max(j => j.SecLength)
                }).FirstOrDefaultAsync();
                return Ok(new
                {
                    SecondTotal = totalAggregated?.SecondTotal ?? 0,
                    TranscribedTotal = totalAggregated?.TranscribedTotal ?? 0,
                    BytesUsed = totalAggregated?.BytesUsed ?? 0,
                    WordsTotal = totalAggregated?.WordsTotal ?? 0,
                    LongestEntrySeconds = totalAggregated?.LongestEntrySeconds ?? 0,
                    totalEntries
                });
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong getting data for stats row", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong getting data for stats row");
            }
        }
        [HttpGet("transcriptions-left")]
        [Authorize]
        public async Task<IActionResult> GetTranscriptionsLeft()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("No userId found in JWT");
                var collection = _mondoDBService.GetUserCollection();
                var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
                var profile = await collection.Find(filter).FirstOrDefaultAsync() ?? throw new Exception("No user found in db with given userId");
                if (profile.CoolUser == true)
                {
                    return Ok(new { TranscriptionsLeft = -1 });
                }
                else
                {
                    return Ok(new { profile.TranscriptionsLeft });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting transcriptions left", e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong getting the number of transcriptions left");
            }
        }
    }

}