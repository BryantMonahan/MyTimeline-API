using Microsoft.AspNetCore.Mvc;
using mongoAPI.Services;
using mongoAPI.Models;
using BCrypt.Net;
using MongoDB.Driver;
using mongoAPI.Types;
using MongoDB.Bson.Serialization.Serializers;


namespace mongoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        private readonly TokenService _tokenService;

        public AuthController(MongoDBService mongoDBService, TokenService tokenService)
        {
            _mongoDBService = mongoDBService;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> AttemptLogin([FromBody] LoginRequest req)
        {
            var collection = _mongoDBService.GetUserCollection();
            var user = await collection.Find<User>(u => u.username == req.Username).FirstOrDefaultAsync<User>();
            if (user == null)
            {
                return Unauthorized("Incorrect username or password");
            }
            if (BCrypt.Net.BCrypt.EnhancedVerify(req.Password, user.hashedPassword))
            {
                string token = _tokenService.CreateToken(user);
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddHours(4),
                };
                Response.Cookies.Append("token", token, options);
                return Ok();
            }
            else
            {
                return Unauthorized("Incorrect username or password");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAccount([FromBody] RegisterRequest req)
        {
            var collection = _mongoDBService.GetUserCollection();
            var user = await collection.Find<User>(u => u.username == req.Username || u.email == req.Email).FirstOrDefaultAsync();
            if (user == null)
            {
                await collection.InsertOneAsync(new Models.User
                {
                    username = req.Username,
                    hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(req.Password, 12),
                    email = req.Email
                });
                return CreatedAtAction(nameof(AttemptLogin), null, new { username = req.Username, email = req.Email });
            }
            else
            {
                if (user.username == req.Username)
                {
                    return Conflict("That username is already taken");
                }
                else
                {
                    return Conflict("That email is already taken");
                }
            }
        }
    }
}
