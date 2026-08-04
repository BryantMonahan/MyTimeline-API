using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using mongoAPI.Models;
using mongoAPI.Services;

namespace mongoAPI.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    public class PlaylistController: ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public PlaylistController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        //[HttpGet]
        //public async Task<List<Playlist>> GetAllPlaylists()
        //{
        //    return await _mongoDBService.GetAllPlaylists();
        //}

        //[HttpPost]
        //public async Task<IActionResult> CreatePlaylist([FromBody] Playlist playlist)
        //{
        //    try
        //    {
        //        await _mongoDBService.CreatePlaylist(playlist);
        //        return CreatedAtAction(nameof(GetAllPlaylists), new { id = playlist.Id }, playlist);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return BadRequest(e.Message);
        //    }
     
        //}

        //// this endpoint is just for ADDING a movie to a playlist
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdatePlaylist(string id, [FromBody] string movieId)
        //{
        //    try
        //    {
        //        await _mongoDBService.AddPlaylist(id, movieId);
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return BadRequest(e.Message);
        //    }
        //}

        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeletePlaylist(string id)
        //{
        //    try
        //    {
        //        await _mongoDBService.DeletePlaylist(id);
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return BadRequest();
        //    }
        //}

    }
}
