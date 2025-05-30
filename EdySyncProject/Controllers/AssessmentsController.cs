using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdySyncProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssessmentsController : ControllerBase
    {
        private readonly EduSyncContext _context;

        public AssessmentsController(EduSyncContext context)
        {
            _context = context;
        }

        // GET: api/Assessments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssessmentDTO>>> GetAssessments()
        {
            var assessments = await _context.Assessments
                .Include(a => a.Questions)
                .Select(a => new AssessmentDTO
                {
                    AssessmentId = a.AssessmentId,
                    Title = a.Title,
                    MaxScore = a.MaxScore,
                    CourseId = a.CourseId,
                    Questions = a.Questions.Select(q => new QuestionDTO
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD
                    }).ToList()
                })
                .ToListAsync();

            return Ok(assessments);
        }

        // GET: api/Assessments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AssessmentDTO>> GetAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
                return NotFound();

            var dto = new AssessmentDTO
            {
                AssessmentId = assessment.AssessmentId,
                Title = assessment.Title,
                MaxScore = assessment.MaxScore,
                CourseId = assessment.CourseId,
                Questions = assessment.Questions.Select(q => new QuestionDTO
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<AssessmentDTO>> PostAssessment([FromBody] CreateAssessmentDTO dto)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == dto.CourseId);
            if (!courseExists)
                return BadRequest("CourseId is invalid or does not exist.");

            if (dto.Questions == null || !dto.Questions.Any())
                return BadRequest("At least one question is required.");

            var assessment = new Assessment
            {
                AssessmentId = Guid.NewGuid(),
                Title = dto.Title,
                MaxScore = dto.MaxScore,
                CourseId = dto.CourseId,
                Questions = dto.Questions.Select(q => new Question
                {
                    QuestionId = Guid.NewGuid(),
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption
                }).ToList()
            };

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            var result = new AssessmentDTO
            {
                AssessmentId = assessment.AssessmentId,
                Title = assessment.Title,
                MaxScore = assessment.MaxScore,
                CourseId = assessment.CourseId,
                Questions = assessment.Questions.Select(q => new QuestionDTO
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD
                }).ToList()
            };

            return CreatedAtAction(nameof(GetAssessment), new { id = assessment.AssessmentId }, result);
        }

        [HttpGet("{id}/attempt-status")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAttemptStatus(Guid id)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var alreadyAttempted = await _context.Results.AnyAsync(r => r.AssessmentId == id && r.UserId == userId);
            return Ok(new { attempted = alreadyAttempted });
        }




        [HttpPost("{id}/attempt")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AttemptAssessment(Guid id, [FromBody] AttemptDTO dto)
        {
            if (dto == null || dto.Answers == null)
                return BadRequest("Answers are required.");

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            // Check if result already exists for this assessment and user
            var alreadyAttempted = await _context.Results.AnyAsync(r => r.AssessmentId == id && r.UserId == userId);
            if (alreadyAttempted)
                return BadRequest("You have already attempted this assessment.");

            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
                return NotFound("Assessment not found.");

            int score = 0;
            foreach (var question in assessment.Questions)
            {
                if (dto.Answers.TryGetValue(question.QuestionId, out var selectedOption))
                {
                    if (string.Equals(selectedOption, question.CorrectOption, StringComparison.OrdinalIgnoreCase))
                    {
                        score++;
                    }
                }
            }

            var result = new Result
            {
                ResultId = Guid.NewGuid(),
                AssessmentId = id,
                UserId = userId,
                Score = score,
                AttemptDate = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return Ok(new { score, resultId = result.ResultId });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                .Include(a => a.Results)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
                return NotFound();

            // Remove all related results
            if (assessment.Results != null && assessment.Results.Any())
                _context.Results.RemoveRange(assessment.Results);

            // Remove all related questions
            if (assessment.Questions != null && assessment.Questions.Any())
                _context.Questions.RemoveRange(assessment.Questions);

            // Remove the assessment itself
            _context.Assessments.Remove(assessment);

            await _context.SaveChangesAsync();

            return NoContent();
        }


    }
}
