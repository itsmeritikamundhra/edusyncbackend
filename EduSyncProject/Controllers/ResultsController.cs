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
using EduSyncProject.Helpers;
using System.Text.Json;

namespace EduSyncProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Base authorization for all endpoints
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EventHubService _eventHubService;

        public ResultsController(AppDbContext context, EventHubService eventHubService)
        {
            _context = context;
            _eventHubService = eventHubService;
        }

        // GET: api/Results
        [HttpGet]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<IEnumerable<ResultReadDTO>>> GetResults()
        {
            var results = await _context.Results
                .Include(r => r.Assessment)
                .Include(r => r.User)
                .ToListAsync();

            var resultDtos = results.Select(result => new ResultReadDTO
            {
                Id = result.ResultId.ToString(),
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                AssessmentId = result.AssessmentId.ToString(),
                AssessmentTitle = result.Assessment.Title,
                UserId = result.UserId.ToString(),
                UserName = result.User.Name,
                MaxScore = result.Assessment.MaxScore
            });

            return Ok(resultDtos);
        }

        // GET: api/Results/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<ResultReadDTO>> GetResult(Guid id)
        {
            var result = await _context.Results
                .Include(r => r.Assessment)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound();

            // If student, can only view their own results
            if (User.IsInRole("Student"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (result.UserId.ToString() != userId)
                {
                    return Forbid();
                }
            }

            var resultDto = new ResultReadDTO
            {
                Id = result.ResultId.ToString(),
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                AssessmentId = result.AssessmentId.ToString(),
                AssessmentTitle = result.Assessment.Title,
                UserId = result.UserId.ToString(),
                UserName = result.User.Name,
                MaxScore = result.Assessment.MaxScore
            };

            return resultDto;
        }

        // PUT: api/Results/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> PutResult(Guid id, ResultUpdateDTO resultDto)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            result.Score = resultDto.Score;
            result.AttemptDate = resultDto.AttemptDate;

            try
            {
                await _context.SaveChangesAsync();
                // Send event to Event Hub (now as JSON)
                var json = JsonSerializer.Serialize(new {
                    EventType = "ResultUpdated",
                    ResultId = id,
                    Score = result.Score,
                    AttemptDate = result.AttemptDate,
                    Timestamp = DateTime.UtcNow
                });
                await _eventHubService.SendMessageAsync(json);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResultExists(id))
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

        // POST: api/Results
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ResultReadDTO>> PostResult(ResultCreateDTO resultDto)
        {
            // Validate that the assessment exists
            var assessment = await _context.Assessments.FindAsync(resultDto.AssessmentId);
            if (assessment == null)
            {
                return BadRequest("Invalid Assessment ID");
            }

            // Get current user ID from claims
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return BadRequest("User ID not found in token");
            }

            // Convert string to Guid
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return BadRequest("Invalid user ID format");
            }

            // Ensure the result is being created for the authenticated student
            if (resultDto.UserId != userGuid)
            {
                return BadRequest("Students can only submit their own results");
            }

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var result = new Result
            {
                Score = resultDto.Score,
                AttemptDate = resultDto.AttemptDate,
                AssessmentId = resultDto.AssessmentId,
                UserId = userGuid
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            // Send event to Event Hub (now as JSON)
            var json = JsonSerializer.Serialize(new {
                EventType = "ResultCreated",
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                AssessmentTitle = assessment.Title,
                UserId = result.UserId,
                UserEmail = user.Email,
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                Timestamp = DateTime.UtcNow
            });
            await _eventHubService.SendMessageAsync(json);

            var readDto = new ResultReadDTO
            {
                Id = result.ResultId.ToString(),
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                AssessmentId = result.AssessmentId.ToString(),
                AssessmentTitle = assessment.Title,
                UserId = result.UserId.ToString(),
                UserName = user.Name,
                MaxScore = assessment.MaxScore
            };

            return CreatedAtAction(nameof(GetResult), new { id = result.ResultId }, readDto);
        }

        // DELETE: api/Results/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteResult(Guid id)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            // Send event to Event Hub (now as JSON)
            var json = JsonSerializer.Serialize(new {
                EventType = "ResultDeleted",
                ResultId = id,
                Timestamp = DateTime.UtcNow
            });
            await _eventHubService.SendMessageAsync(json);

            return NoContent();
        }

        private bool ResultExists(Guid id)
        {
            return _context.Results.Any(e => e.ResultId == id);
        }
    }
}
