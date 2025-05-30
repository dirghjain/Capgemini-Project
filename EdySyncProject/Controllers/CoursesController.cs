using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EdySyncProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly EduSyncContext _context;

        public CoursesController(EduSyncContext context)
        {
            _context = context;
        }

        // GET: api/Courses
        [HttpGet]
        [Authorize(Roles = "Instructor,Student")] 
        public async Task<ActionResult<IEnumerable<CourseDTO>>> GetCourses()
        {
            var courses = await _context.Courses
                .Select(course => new CourseDTO
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    MediaUrl = course.MediaUrl,
                    InstructorId = course.InstructorId
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET: api/Courses/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Instructor,Student")]
        public async Task<ActionResult<CourseDTO>> GetCourse(Guid id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            var courseDto = new CourseDTO
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                MediaUrl = course.MediaUrl,
                InstructorId = course.InstructorId
            };

            return Ok(courseDto);
        }
        [HttpPost("{courseId}/enroll")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(Guid courseId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            // Prevent duplicate enrollment
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
            if (alreadyEnrolled)
                return BadRequest("You are already enrolled in this course.");

            var enrollment = new Enrollment
            {
                EnrollmentId = Guid.NewGuid(),
                UserId = userId,
                CourseId = courseId
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok("Enrolled successfully!");
        }

        [HttpGet("{courseId}/enrollment-status")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CheckEnrollmentStatus(Guid courseId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);

            return Ok(new { enrolled = isEnrolled });
        }
        // GET: api/Courses/enrolled
        [HttpGet("enrolled")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<IEnumerable<CourseDTO>>> GetEnrolledCourses()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c =>
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            // Find all course IDs this student is enrolled in
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();

            // Get course details for those courses
            var courses = await _context.Courses
                .Where(c => enrolledCourseIds.Contains(c.CourseId))
                .Select(course => new CourseDTO
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    MediaUrl = course.MediaUrl,
                    InstructorId = course.InstructorId
                })
                .ToListAsync();

            return Ok(courses);
        }


        // POST: api/Courses
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<CourseDTO>> PostCourse([FromBody] CreateCourseDTO dto)
        {
            var instructor = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.InstructorId && u.Role == "Instructor");
            if (instructor == null)
                return BadRequest("InstructorId is invalid.");

            var course = new Course
            {
                CourseId = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                MediaUrl = dto.MediaUrl,
                InstructorId = dto.InstructorId
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var result = new CourseDTO
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                MediaUrl = course.MediaUrl,
                InstructorId = course.InstructorId
            };

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, result);
        }

        // PUT: api/Courses/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> PutCourse(Guid id, [FromBody] CreateCourseDTO dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return NotFound();

            if (course.InstructorId != dto.InstructorId)
                return Forbid();

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.MediaUrl = dto.MediaUrl;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            // Load all related data
            var course = await _context.Courses
                .Include(c => c.Assessments)
                    .ThenInclude(a => a.Results)
                .Include(c => c.Assessments)
                    .ThenInclude(a => a.Questions)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound();

            foreach (var assessment in course.Assessments ?? new List<Assessment>())
            {
                if (assessment.Results != null && assessment.Results.Any())
                    _context.Results.RemoveRange(assessment.Results);

                if (assessment.Questions != null && assessment.Questions.Any())
                    _context.Questions.RemoveRange(assessment.Questions);
            }

            // Remove all related assessments
            if (course.Assessments != null && course.Assessments.Any())
                _context.Assessments.RemoveRange(course.Assessments);

            // Remove all related enrollments
            if (course.Enrollments != null && course.Enrollments.Any())
                _context.Enrollments.RemoveRange(course.Enrollments);

            // Finally, remove the course itself
            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();

            return NoContent();
        }


        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}
