
public class LoginDTO
{
    public string Email { get; set; }
    public string Password { get; set; }
}
public class CreateUserDTO
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string PasswordHash { get; set; } 
}
