using System;

public class PasswordResetToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }

    public User User { get; set; } 
}
