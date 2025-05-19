public class AssessmentDTO
{
    public Guid AssessmentId { get; set; }
    public string Title { get; set; }
    public string Questions { get; set; } 
    public int MaxScore { get; set; }
    public Guid CourseId { get; set; }
}
public class CreateAssessmentDTO
{
    public string Title { get; set; }
    public string Questions { get; set; } 
    public int MaxScore { get; set; }
    public Guid CourseId { get; set; }
}
