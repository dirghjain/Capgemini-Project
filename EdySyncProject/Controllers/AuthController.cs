using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EdySyncProject.Services;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly EduSyncContext _context;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public AuthController(EduSyncContext context, IConfiguration configuration, EmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Role) || dto.Role.Trim().ToLower() != "student")
            return BadRequest("You can only register as a Student.");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already in use.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.PasswordHash);

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Role = "Student", 
            PasswordHash = hashedPassword
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        try
        {
            await _emailService.SendWelcomeEmailAsync(dto.Email, dto.Name);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Email send failed: " + ex.Message);
        }
        return Ok("Registration successful as Student!");
    }




    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null)
            return Unauthorized("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return Unauthorized("Wrong Password");

        var token = GenerateJwtToken(user); 
        return Ok(new { token });
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return Ok(); 

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = expiresAt,
            Used = false
        };
        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        var resetLink = $"http://localhost:3000/reset-password?token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);

        return Ok();
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
    {
        var tokenEntry = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == dto.Token &&
                !t.Used &&
                t.ExpiresAt > DateTime.UtcNow);

        if (tokenEntry == null)
            return BadRequest("Invalid or expired reset token.");

        tokenEntry.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        tokenEntry.Used = true;
        await _context.SaveChangesAsync();

        return Ok("Password has been reset successfully.");
    }



    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(10),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}