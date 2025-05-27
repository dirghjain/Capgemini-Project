using Microsoft.EntityFrameworkCore;

public class EduSyncContext : DbContext
{
    public EduSyncContext(DbContextOptions<EduSyncContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Question> Questions { get; set; }



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

        modelBuilder.Entity<Assessment>()
            .HasMany(a => a.Results)
            .WithOne(r => r.Assessment)
            .HasForeignKey(r => r.AssessmentId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Results)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(t => t.User)
            .WithMany() 
            .HasForeignKey(t => t.UserId);
    }
}
