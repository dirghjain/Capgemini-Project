public class Question
{
    public Guid QuestionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string QuestionText { get; set; }
    public string OptionA { get; set; }
    public string OptionB { get; set; }
    public string OptionC { get; set; }
    public string OptionD { get; set; }
    public string CorrectOption { get; set; } 
    public Assessment Assessment { get; set; }
}
