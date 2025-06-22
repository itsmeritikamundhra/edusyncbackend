using EduSyncProject.Data;
using EduSyncProject.DTO;
using EduSyncProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EduSyncProject.Helpers.BlobService _blobService;

        public CoursesController(AppDbContext context, EduSyncProject.Helpers.BlobService blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        // GET: api/Courses
        [HttpGet]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<IEnumerable<CourseReadDTO>>> GetCourses()
        {
            // Get all courses for both students and instructors
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .ToListAsync();

            var courseDtos = courses.Select(course => new CourseReadDTO
            {
                Id = course.CourseId.ToString(),
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId.ToString(),
                InstructorName = course.Instructor.Name,
                MediaUrl = course.MediaUrl
            });

            return Ok(courseDtos);
        }

       [HttpGet("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<CourseReadDTO>> GetCourse(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            var courseDto = new CourseReadDTO
            {
                Id = course.CourseId.ToString(),
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId.ToString(),
                InstructorName = course.Instructor.Name,
                MediaUrl = course.MediaUrl
            };

            return Ok(courseDto);
        }

        // PUT: api/Courses/5
        
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> PutCourse(Guid id, CourseUpdateDTO courseDto)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound("Course not found");
            }

            // Verify that the instructor is updating their own course
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            // Additional validation to ensure the instructor exists and has correct role
            var instructor = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId.ToString() == instructorId && u.Role == "Instructor");
            
            if (instructor == null)
            {
                return StatusCode(403, new { message = "Only users with Instructor role can update courses" });
            }

            if (course.InstructorId != instructor.UserId)
            {
                return StatusCode(403, new { 
                    message = "Instructors can only update their own courses",
                    courseInstructorId = course.InstructorId.ToString(),
                    yourInstructorId = instructorId,
                    courseTitle = course.Title
                });
            }

            course.Title = courseDto.Title;
            course.Description = courseDto.Description;
            course.MediaUrl = courseDto.MediaUrl;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
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

        // POST: api/Courses
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<CourseReadDTO>> PostCourse(CourseCreateDTO courseDto)
        {
            // Get current instructor's ID from claims
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (instructorId == null)
            {
                return BadRequest("Instructor ID not found in token");
            }

            // Convert string to Guid
            if (!Guid.TryParse(instructorId, out Guid instructorGuid))
            {
                return BadRequest("Invalid instructor ID format");
            }

            // Verify that the user is actually an instructor
            var instructor = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == instructorGuid && u.Role == "Instructor");
            
            if (instructor == null)
            {
                return StatusCode(403, new { message = "Only users with Instructor role can create courses" });
            }

            var course = new Course
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                InstructorId = instructorGuid,  // Always use the authenticated instructor's ID
                MediaUrl = courseDto.MediaUrl,
                Assessments = new List<Assessment>()
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var readDto = new CourseReadDTO
            {
                Id = course.CourseId.ToString(),
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId.ToString(),
                InstructorName = instructor.Name,
                MediaUrl = course.MediaUrl
            };

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, readDto);
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Assessments)
                    .ThenInclude(a => a.Results)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            // Verify that the instructor is deleting their own course
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (course.InstructorId.ToString() != instructorId)
            {
                return StatusCode(403, new { 
                    message = "Instructors can only delete their own courses",
                    courseInstructorId = course.InstructorId.ToString(),
                    yourInstructorId = instructorId,
                    courseTitle = course.Title
                });
            }
            // Delete the associated blob/file if MediaUrl exists
            if (!string.IsNullOrEmpty(course.MediaUrl))
            {
                try
                {
                    var uri = new Uri(course.MediaUrl);
                    string fileName = Uri.UnescapeDataString(uri.Segments.Last());
                    Console.WriteLine($"Trying to delete file: '{fileName}' from container: '{_blobService.GetType().GetProperty("_containerName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_blobService)}'");
                    await _blobService.DeleteFileAsync(fileName);
                }
                catch (Exception ex)
                {
                    // Log the error, but do not block course deletion
                    Console.WriteLine($"Failed to delete blob: {ex}");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete all results associated with the course's assessments
                foreach (var assessment in course.Assessments)
                {
                    _context.Results.RemoveRange(assessment.Results);
                }

                // Delete all assessments
                _context.Assessments.RemoveRange(course.Assessments);

                // Delete the course
                _context.Courses.Remove(course);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}
