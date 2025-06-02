using EduSyncProject.Data;
using EduSyncProject.DTO;
using EduSyncProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EduSyncProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Base authorization for all endpoints
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<IEnumerable<UserReadDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Where(user => user.Role == "Student") // Only get users with Student role
                .ToListAsync();
            var userDtos = users.Select(user => new UserReadDTO
            {
                Id = user.UserId.ToString(),
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
            return Ok(userDtos);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<UserReadDTO>> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // If student, can only view their own profile
            if (User.IsInRole("Student"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (user.UserId.ToString() != userId)
                {
                    return Forbid();
                }
            }

            var userDto = new UserReadDTO
            {
                Id = user.UserId.ToString(),
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

            return userDto;
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<IActionResult> PutUser(Guid id, UserUpdateDTO userDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // If student, can only update their own profile
            if (User.IsInRole("Student"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (user.UserId.ToString() != userId)
                {
                    return Forbid();
                }

                // Students cannot change their role
                if (user.Role != userDto.Role)
                {
                    return BadRequest("Students cannot change their role");
                }
            }
            // If instructor, cannot update other instructor's profile
            else if (User.IsInRole("Instructor"))
            {
                if (user.Role.ToLower() == "instructor")
                {
                    var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (user.UserId.ToString() != instructorId)
                    {
                        return StatusCode(403, new { message = "Instructors cannot modify other instructors' accounts" });
                    }
                }

                // Prevent changing student to instructor
                if (user.Role.ToLower() == "student" && userDto.Role.ToLower() == "instructor")
                {
                    return StatusCode(403, new { message = "Cannot change a student's role to instructor" });
                }
            }

            user.Name = userDto.Name;
            user.Email = userDto.Email;
            user.Role = userDto.Role;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<UserReadDTO>> PostUser(UserCreateDTO userDto)
        {
            // Validate that instructors can only create student accounts
            if (string.IsNullOrEmpty(userDto.Role) || userDto.Role.ToLower() != "student")
            {
                return StatusCode(403, new { message = "Instructors can only create student accounts. Creating instructor accounts is not allowed." });
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var user = new User
            {
                Name = userDto.Name,
                Email = userDto.Email,
                Role = userDto.Role,  // Use the provided role (we've already validated it's "student")
                Courses = new List<Course>(),
                Results = new List<Result>()
            };

            var hasher = new PasswordHasher<object>();
            user.PasswordHash = hasher.HashPassword(null, userDto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var readDto = new UserReadDTO
            {
                Id = user.UserId.ToString(),
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, readDto);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Courses)
                    .ThenInclude(c => c.Assessments)
                        .ThenInclude(a => a.Results)
                .Include(u => u.Results)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent deletion of instructor accounts
            if (user.Role.ToLower() == "instructor")
            {
                return StatusCode(403, new { message = "Instructors cannot delete other instructor accounts" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. First delete all results from assessments in user's courses
                foreach (var course in user.Courses)
                {
                    foreach (var assessment in course.Assessments)
                    {
                        _context.Results.RemoveRange(assessment.Results);
                    }
                }

                // 2. Delete all direct results linked to the user
                _context.Results.RemoveRange(user.Results);

                // 3. Delete all assessments in user's courses
                foreach (var course in user.Courses)
                {
                    _context.Assessments.RemoveRange(course.Assessments);
                }

                // 4. Delete all courses linked to the user
                _context.Courses.RemoveRange(user.Courses);

                // 5. Finally delete the user
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error deleting user: {ex.Message}");
            }
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
