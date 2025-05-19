using Microsoft.EntityFrameworkCore;

public class EduSyncContext : DbContext
{
    public EduSyncContext(DbContextOptions<EduSyncContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Courses)
            .WithOne(c => c.Instructor)
            .HasForeignKey(c => c.InstructorId);

        modelBuilder.Entity<Course>()
            .HasMany(c => c.Assessments)
            .WithOne(a => a.Course)
            .HasForeignKey(a => a.CourseId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Results)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);
    }
}
