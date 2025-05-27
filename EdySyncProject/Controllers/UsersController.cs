using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly EduSyncContext _context;

    public UsersController(EduSyncContext context)
    {
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    [Authorize(Roles = "Instructor")] 
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserDTO
            {
                UserId = u.UserId,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role
            }).ToListAsync();

        return Ok(users);
    }

    // GET: api/Users/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Instructor,Student")]
    public async Task<ActionResult<UserDTO>> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        return new UserDTO
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
    }

    // PUT: api/Users/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Instructor")] 
    public async Task<IActionResult> UpdateUser(Guid id, CreateUserDTO dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Role = dto.Role;
        user.PasswordHash = dto.PasswordHash;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
