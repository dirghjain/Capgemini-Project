public class Course
{
    public Guid CourseId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string MediaUrl { get; set; }

    public Guid InstructorId { get; set; }
    public User Instructor { get; set; }

    public ICollection<Assessment> Assessments { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; }
}
