public class Assessment
{
    public Guid AssessmentId { get; set; }
    public string Title { get; set; }
    public int MaxScore { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; }
    public ICollection<Result> Results { get; set; }
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

}
