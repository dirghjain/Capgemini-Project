public class User
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string PasswordHash { get; set; }

    public ICollection<Course> Courses { get; set; }
    public ICollection<Result> Results { get; set; }
}
