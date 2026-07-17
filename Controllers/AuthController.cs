using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Data;
using QuickParkAPI.DTOs;
using QuickParkAPI.Models;
using QuickParkAPI.Services;

namespace QuickParkAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwt;

        public AuthController(AppDbContext db, IJwtService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Invalid request data" });

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { error = "Name, email, and password are required" });

            if (dto.Password.Length < 6)
                return BadRequest(new { error = "Password must be at least 6 characters long" });

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (existing != null)
                return BadRequest(new { error = "User already exists with this email address" });

            var role = dto.Role ?? "user";
            var isOwner = role == "owner";
            var verified = !isOwner; // owners need admin approval

            var user = new User
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.ToLower().Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Role = role,
                Verified = verified,
                IsActive = true,
                Address = isOwner ? (dto.Address ?? string.Empty) : string.Empty,
                GovtId = isOwner ? (dto.GovtId ?? string.Empty) : string.Empty,
                GovtIdType = isOwner ? (dto.GovtIdType ?? string.Empty) : string.Empty,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return StatusCode(201, new AuthResponseDto
            {
                Success = true,
                Token = token,
                User = MappingHelper.ToUserResponse(user),
                PendingApproval = isOwner && !verified
            });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { error = "Email and password are required" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (user == null)
                return StatusCode(401, new { error = "Invalid email or password" });

            if (!user.IsActive)
                return StatusCode(403, new { error = "Your account has been deactivated. Please contact support." });

            if (user.Role == "owner" && !user.Verified)
                return StatusCode(403, new
                {
                    error = "Your owner account is pending admin approval. Please wait for verification before logging in.",
                    pendingApproval = true
                });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return StatusCode(401, new { error = "Invalid email or password" });

            var token = _jwt.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                User = MappingHelper.ToUserResponse(user)
            });
        }

        // GET /api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetUserId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound(new { error = "User not found" });

            return Ok(new { success = true, user = MappingHelper.ToUserResponse(user) });
        }

        private int GetUserId() => ClaimsHelper.GetUserId(User);
    }
}
