public class Enrollment
{
    public Guid EnrollmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public User User { get; set; }
    public Course Course { get; set; }
}
