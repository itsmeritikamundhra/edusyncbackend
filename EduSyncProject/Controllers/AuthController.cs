using EduSyncProject.Data;
using EduSyncProject.DTO;
using EduSyncProject.DTO.Auths;
using EduSyncProject.Models;
using EduSyncProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;




namespace EduSyncProject.Controllers
{
    [EnableCors("AllowReactApp")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(AppDbContext context, JwtTokenGenerator tokenGenerator, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already exists.");

            var hasher = new PasswordHasher<object>();

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password)
                

            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            var hasher = new PasswordHasher<object>();
            var result = hasher.VerifyHashedPassword(null, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid email or password.");

            var token = _tokenGenerator.GenerateToken(user.UserId.ToString(), user.Email, user.Role);
            return Ok(new
            {
                token = token,
                role = user.Role  // Add this line to include the role in the response
            });
        }
    }
}
