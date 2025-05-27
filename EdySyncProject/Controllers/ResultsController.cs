using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EdySyncProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly EduSyncContext _context;

        public ResultsController(EduSyncContext context)
        {
            _context = context;
        }

        // GET: api/Results
        [HttpGet]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<IEnumerable<ResultDTO>>> GetResults()
        {
            // Get current user's id and role from JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            if (userIdClaim == null || roleClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var role = roleClaim.Value;

            if (role == "Student")
            {
                // Students: Only their own results
                var results = await _context.Results
                    .Where(r => r.UserId == userId)
                    .Select(r => new ResultDTO
                    {
                        ResultId = r.ResultId,
                        AssessmentId = r.AssessmentId,
                        UserId = r.UserId,
                        Score = r.Score,
                        AttemptDate = r.AttemptDate
                    })
                    .ToListAsync();

                return Ok(results);
            }
            else if (role == "Instructor")
            {
                // Instructors: Results of students enrolled in their courses
                var instructorCourseIds = await _context.Courses
                    .Where(c => c.InstructorId == userId)
                    .Select(c => c.CourseId)
                    .ToListAsync();

                var results = await _context.Results
                    .Include(r => r.Assessment)
                    .Where(r => instructorCourseIds.Contains(r.Assessment.CourseId))
                    .Select(r => new ResultDTO
                    {
                        ResultId = r.ResultId,
                        AssessmentId = r.AssessmentId,
                        UserId = r.UserId,
                        Score = r.Score,
                        AttemptDate = r.AttemptDate
                    })
                    .ToListAsync();

                return Ok(results);
            }
            else
            {
                return Forbid();
            }
        }

        // GET: api/Results/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<ResultDTO>> GetResult(Guid id)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

            if (userIdClaim == null || roleClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var role = roleClaim.Value;

            var result = await _context.Results
                .Include(r => r.Assessment)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound();

            if (role == "Student")
            {
                // Student can only see their own result
                if (result.UserId != userId)
                    return Forbid();
            }
            else if (role == "Instructor")
            {
                // Instructor can only see results for their courses
                var isInstructorCourse = await _context.Courses
                    .AnyAsync(c => c.CourseId == result.Assessment.CourseId && c.InstructorId == userId);

                if (!isInstructorCourse)
                    return Forbid();
            }

            var dto = new ResultDTO
            {
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                UserId = result.UserId,
                Score = result.Score,
                AttemptDate = result.AttemptDate
            };

            return Ok(dto);
        }
    }
}
