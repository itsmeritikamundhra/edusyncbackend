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
    public class AssessmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssessmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Assessments
        [HttpGet]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<IEnumerable<AssessmentReadDTO>>> GetAssessments()
        {
            try
            {
                if (User.IsInRole("Instructor"))
                {
                    var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (instructorId == null)
                    {
                        return BadRequest("Instructor ID not found in token");
                    }

                    // Get all assessments from courses owned by this instructor
                    var instructorAssessments = await _context.Assessments
                        .Include(a => a.Course)
                        .Where(a => a.Course.InstructorId.ToString() == instructorId)
                        .Select(assessment => new AssessmentReadDTO
                        {
                            Id = assessment.AssessmentId.ToString(),
                            Title = assessment.Title,
                            Questions = assessment.Questions,
                            MaxScore = assessment.MaxScore,
                            CourseId = assessment.CourseId.ToString(),
                            CourseTitle = assessment.Course.Title
                        })
                        .ToListAsync();

                    return Ok(instructorAssessments);
                }
                else if (User.IsInRole("Student"))
                {
                    var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (studentId == null)
                    {
                        return BadRequest("User ID not found in token");
                    }

                    // Get assessments from courses where the student is enrolled
                    var studentAssessments = await _context.Users
                        .Where(u => u.UserId.ToString() == studentId)
                        .SelectMany(u => u.EnrolledCourses)
                        .SelectMany(c => c.Assessments)
                        .Include(a => a.Course)  // Ensure Course is included for CourseTitle
                        .Select(assessment => new AssessmentReadDTO
                        {
                            Id = assessment.AssessmentId.ToString(),
                            Title = assessment.Title,
                            Questions = assessment.Questions,
                            MaxScore = assessment.MaxScore,
                            CourseId = assessment.CourseId.ToString(),
                            CourseTitle = assessment.Course.Title
                        })
                        .ToListAsync();

                    return Ok(studentAssessments);
                }

                return BadRequest("Invalid user role");
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving assessments" });
            }
        }

        // GET: api/Assessments/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<AssessmentReadDTO>> GetAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            // If student, verify they are enrolled in the course
            if (User.IsInRole("Student"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return BadRequest("User ID not found in token");
                }

                var userGuid = Guid.Parse(userId);
                var isEnrolled = await _context.Users
                    .Include(u => u.Courses)
                    .Where(u => u.UserId == userGuid)
                    .SelectMany(u => u.Courses)
                    .AnyAsync(c => c.CourseId == assessment.CourseId);

                if (!isEnrolled)
                {
                    return StatusCode(403, new { message = "You must be enrolled in the course to view this assessment" });
                }
            }

            var assessmentDto = new AssessmentReadDTO
            {
                Id = assessment.AssessmentId.ToString(),
                Title = assessment.Title,
                Questions = assessment.Questions,
                MaxScore = assessment.MaxScore,
                CourseId = assessment.CourseId.ToString(),
                CourseTitle = assessment.Course.Title
            };

            return assessmentDto;
        }

        // PUT: api/Assessments/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> PutAssessment(Guid id, AssessmentUpdateDTO assessmentDto)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            // Verify that the instructor owns the course containing this assessment
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (assessment.Course.InstructorId.ToString() != instructorId)
            {
                return Forbid("Instructors can only update assessments in their own courses");
            }

            assessment.Title = assessmentDto.Title;
            assessment.Questions = assessmentDto.Questions;
            assessment.MaxScore = assessmentDto.MaxScore;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssessmentExists(id))
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

        // POST: api/Assessments
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<AssessmentReadDTO>> PostAssessment(AssessmentCreateDTO assessmentDto)
        {
            // Get instructor ID from claims
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (instructorId == null)
            {
                return BadRequest(new { message = "Instructor ID not found in token" });
            }

            // Verify that the course exists
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == assessmentDto.CourseId);

            if (course == null)
            {
                return BadRequest(new { message = $"Course with ID {assessmentDto.CourseId} not found" });
            }

            // Debug information about the course and instructor
            if (course.InstructorId.ToString() != instructorId)
            {
                return StatusCode(403, new { 
                    message = "Instructors can only create assessments in their own courses",
                    courseInstructorId = course.InstructorId.ToString(),
                    yourInstructorId = instructorId,
                    courseTitle = course.Title
                });
            }

            var assessment = new Assessment
            {
                Title = assessmentDto.Title,
                Questions = assessmentDto.Questions,
                MaxScore = assessmentDto.MaxScore,
                CourseId = assessmentDto.CourseId,
                Results = new List<Result>()
            };

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            var readDto = new AssessmentReadDTO
            {
                Id = assessment.AssessmentId.ToString(),
                Title = assessment.Title,
                Questions = assessment.Questions,
                MaxScore = assessment.MaxScore,
                CourseId = assessment.CourseId.ToString(),
                CourseTitle = course.Title
            };

            return CreatedAtAction(nameof(GetAssessment), new { id = assessment.AssessmentId }, readDto);
        }

        // DELETE: api/Assessments/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Results)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            // Verify that the instructor owns the course containing this assessment
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (assessment.Course.InstructorId.ToString() != instructorId)
            {
                return StatusCode(403, new { 
                    message = "Instructors can only delete assessments in their own courses",
                    courseInstructorId = assessment.Course.InstructorId.ToString(),
                    yourInstructorId = instructorId,
                    courseTitle = assessment.Course.Title,
                    assessmentTitle = assessment.Title
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete all results for this assessment
                _context.Results.RemoveRange(assessment.Results);

                // Delete the assessment
                _context.Assessments.Remove(assessment);

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

        private bool AssessmentExists(Guid id)
        {
            return _context.Assessments.Any(e => e.AssessmentId == id);
        }
    }
}
