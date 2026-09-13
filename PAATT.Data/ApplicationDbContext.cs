using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PAATT.Data.Entities;

namespace PAATT.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<TimesheetReview> TimesheetReviews => Set<TimesheetReview>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureUsers(builder);
        ConfigureClients(builder);
        ConfigureProjects(builder);
        ConfigureAssignments(builder);
        ConfigureTimesheets(builder);
        ConfigureEntries(builder);
        ConfigureReviews(builder);
    }

    private static void ConfigureUsers(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().Property(user => user.Name).HasMaxLength(150).IsRequired();
        builder.Entity<ApplicationUser>().HasIndex(user => user.Name);
    }

    private static void ConfigureClients(ModelBuilder builder)
    {
        builder.Entity<Client>(entity =>
        {
            entity.Property(client => client.Name).HasMaxLength(200).IsRequired();
            entity.Property(client => client.ContactName).HasMaxLength(150);
            entity.Property(client => client.ContactEmail).HasMaxLength(256);
            entity.HasIndex(client => client.Name).IsUnique();
        });
    }

    private static void ConfigureProjects(ModelBuilder builder)
    {
        builder.Entity<Project>(entity =>
        {
            entity.Property(project => project.Name).HasMaxLength(200).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(2000);
            entity.Property(project => project.BudgetedHours).HasPrecision(10, 2);
            entity.HasIndex(project => new { project.ClientId, project.Name }).IsUnique();
            entity.HasOne(project => project.Client).WithMany(client => client.Projects)
                .HasForeignKey(project => project.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(project => project.ProjectManager).WithMany(user => user.ManagedProjects)
                .HasForeignKey(project => project.ProjectManagerId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAssignments(ModelBuilder builder)
    {
        builder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasIndex(assignment => new { assignment.ProjectId, assignment.ConsultantId }).IsUnique();
            entity.HasOne(assignment => assignment.Project).WithMany(project => project.ProjectAssignments)
                .HasForeignKey(assignment => assignment.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.Consultant).WithMany(user => user.ProjectAssignments)
                .HasForeignKey(assignment => assignment.ConsultantId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTimesheets(ModelBuilder builder)
    {
        builder.Entity<Timesheet>(entity =>
        {
            entity.HasIndex(timesheet => new { timesheet.ConsultantId, timesheet.ProjectId, timesheet.WeekStartDate }).IsUnique();
            entity.HasOne(timesheet => timesheet.Consultant).WithMany(user => user.Timesheets)
                .HasForeignKey(timesheet => timesheet.ConsultantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(timesheet => timesheet.Project).WithMany(project => project.Timesheets)
                .HasForeignKey(timesheet => timesheet.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEntries(ModelBuilder builder)
    {
        builder.Entity<TimesheetEntry>(entity =>
        {
            entity.Property(entry => entry.Hours).HasPrecision(5, 2);
            entity.Property(entry => entry.Description).HasMaxLength(2000).IsRequired();
            entity.Property(entry => entry.Feedback).HasMaxLength(2000);
            entity.HasOne(entry => entry.Timesheet).WithMany(timesheet => timesheet.Entries)
                .HasForeignKey(entry => entry.TimesheetId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReviews(ModelBuilder builder)
    {
        builder.Entity<TimesheetReview>(entity =>
        {
            entity.Property(review => review.Feedback).HasMaxLength(2000);
            entity.HasOne(review => review.Timesheet).WithMany(timesheet => timesheet.Reviews)
                .HasForeignKey(review => review.TimesheetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(review => review.TimesheetEntry).WithMany(entry => entry.Reviews)
                .HasForeignKey(review => review.TimesheetEntryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(review => review.Reviewer).WithMany(user => user.Reviews)
                .HasForeignKey(review => review.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
