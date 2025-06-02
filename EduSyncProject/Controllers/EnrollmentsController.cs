using EduSyncProject.Data;
using EduSyncProject.DTO;
using EduSyncProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EduSyncProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrollmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Enrollments
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<IEnumerable<EnrollmentReadDTO>>> GetEnrolledCourses()
        {
            var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (studentId == null)
            {
                return BadRequest("Student ID not found in token");
            }

            var userGuid = Guid.Parse(studentId);

            var enrolledCourses = await _context.Users
                .Include(u => u.EnrolledCourses)
                    .ThenInclude(c => c.Instructor)
                .Where(u => u.UserId == userGuid)
                .SelectMany(u => u.EnrolledCourses)
                .Select(course => new EnrollmentReadDTO
                {
                    CourseId = course.CourseId.ToString(),
                    CourseTitle = course.Title,
                    Description = course.Description,
                    InstructorId = course.InstructorId.ToString(),
                    InstructorName = course.Instructor.Name,
                    MediaUrl = course.MediaUrl
                })
                .ToListAsync();

            return Ok(enrolledCourses);
        }

        // POST: api/Enrollments/student
        [HttpPost("student")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult> StudentEnroll(StudentEnrollmentDTO enrollmentDto)
        {
            var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (studentId == null)
            {
                return BadRequest("Student ID not found in token");
            }

            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.CourseId == enrollmentDto.CourseId);

            if (course == null)
            {
                return NotFound("Course not found");
            }

            var userGuid = Guid.Parse(studentId);

            // Check if already enrolled
            if (course.Students.Any(s => s.UserId == userGuid))
            {
                return BadRequest("You are already enrolled in this course");
            }

            var student = await _context.Users.FindAsync(userGuid);
            if (student == null)
            {
                return NotFound("Student not found");
            }

            course.Students.Add(student);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Enrollment successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while enrolling in the course", error = ex.Message });
            }
        }

        // POST: api/Enrollments/instructor
        [HttpPost("instructor")]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult> InstructorEnroll(InstructorEnrollmentDTO enrollmentDto)
        {
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (instructorId == null)
            {
                return BadRequest("Instructor ID not found in token");
            }

            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.CourseId == enrollmentDto.CourseId);

            if (course == null)
            {
                return NotFound("Course not found");
            }

            // Verify that the instructor owns the course
            if (course.InstructorId.ToString() != instructorId)
            {
                return StatusCode(403, new { message = "Instructors can only enroll students in their own courses" });
            }

            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == enrollmentDto.StudentId && u.Role == "Student");

            if (student == null)
            {
                return NotFound("Student not found");
            }

            // Check if already enrolled
            if (course.Students.Any(s => s.UserId == student.UserId))
            {
                return BadRequest("Student is already enrolled in this course");
            }

            course.Students.Add(student);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Enrollment successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while enrolling in the course", error = ex.Message });
            }
        }

        // DELETE: api/Enrollments/student/{courseId}
        [HttpDelete("student/{courseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentUnenroll(Guid courseId)
        {
            var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (studentId == null)
            {
                return BadRequest("Student ID not found in token");
            }

            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound("Course not found");
            }

            var userGuid = Guid.Parse(studentId);
            var student = course.Students.FirstOrDefault(s => s.UserId == userGuid);

            if (student == null)
            {
                return NotFound("You are not enrolled in this course");
            }

            course.Students.Remove(student);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Successfully unenrolled from the course" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while unenrolling from the course", error = ex.Message });
            }
        }

        // DELETE: api/Enrollments/instructor/{courseId}
        [HttpDelete("instructor/{courseId}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> InstructorUnenroll(Guid courseId, [FromQuery] Guid studentId)
        {
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (instructorId == null)
            {
                return BadRequest("Instructor ID not found in token");
            }

            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound("Course not found");
            }

            // Verify that the instructor owns the course
            if (course.InstructorId.ToString() != instructorId)
            {
                return StatusCode(403, new { message = "Instructors can only remove students from their own courses" });
            }

            var student = course.Students.FirstOrDefault(s => s.UserId == studentId);
            if (student == null)
            {
                return NotFound("Student not found in the course");
            }

            course.Students.Remove(student);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Successfully unenrolled student from the course" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while unenrolling from the course", error = ex.Message });
            }
        }

        // GET: api/Enrollments/course/{courseId}/students
        [HttpGet("course/{courseId}/students")]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<IEnumerable<UserReadDTO>>> GetEnrolledStudents(Guid courseId)
        {
            // Get the course with its students
            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.CourseId == courseId);

            if (course == null)
            {
                return NotFound("Course not found");
            }

            // Verify that the instructor owns the course
            var instructorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (course.InstructorId.ToString() != instructorId)
            {
                return StatusCode(403, new { message = "Instructors can only view students in their own courses" });
            }

            var enrolledStudents = course.Students.Select(student => new UserReadDTO
            {
                Id = student.UserId.ToString(),
                Name = student.Name,
                Email = student.Email,
                Role = student.Role
            });

            return Ok(enrolledStudents);
        }
    }
} 