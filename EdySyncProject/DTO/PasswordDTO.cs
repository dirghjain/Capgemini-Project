public class ForgotPasswordDTO
{
    public string Email { get; set; }
}

public class ResetPasswordDTO
{
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
