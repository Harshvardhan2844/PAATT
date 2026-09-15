using App.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();

    /// <summary>Append-only review history. Added in Phase 5 — needs a migration.</summary>
    public DbSet<TimesheetReviewEvent> TimesheetReviewEvents => Set<TimesheetReviewEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity tables

        // ---- Client ----
        builder.Entity<Client>(e =>
        {
            e.HasIndex(c => c.CompanyName);
        });

        // ---- Project ----
        builder.Entity<Project>(e =>
        {
            e.HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict); // don't cascade-delete projects if a client row is removed
        });

        // ---- ProjectAssignment ----
        builder.Entity<ProjectAssignment>(e =>
        {
            // One assignment row per employee per project (Manager XOR Consultant
            // on a given project is enforced by this + a service-layer check that
            // blocks a second row for the same (ProjectId, EmployeeId)).
            e.HasIndex(pa => new { pa.ProjectId, pa.EmployeeId }).IsUnique();

            e.Property(pa => pa.BudgetedHours).HasColumnType("decimal(6,2)");

            e.HasOne(pa => pa.Project)
                .WithMany(p => p.Assignments)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a project clears its assignments

            e.HasOne(pa => pa.Employee)
                .WithMany(u => u.ProjectAssignments)
                .HasForeignKey(pa => pa.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict); // don't cascade-delete a user via assignments
        });

        // ---- Timesheet ----
        builder.Entity<Timesheet>(e =>
        {
            // One Timesheet per (Employee, Project, Week) — timesheets are per-project.
            e.HasIndex(t => new { t.EmployeeId, t.ProjectId, t.WeekStartingDate }).IsUnique();

            e.HasOne(t => t.Employee)
                .WithMany(u => u.Timesheets)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Project)
                .WithMany(p => p.Timesheets)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict); // keep timesheet history even if project is later removed
        });

        // ---- TimesheetEntry ----
        builder.Entity<TimesheetEntry>(e =>
        {
            e.HasOne(te => te.Timesheet)
                .WithMany(t => t.Entries)
                .HasForeignKey(te => te.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a timesheet removes its entries

            // Speeds up the cross-project "sum hours for this employee on this date"
            // 24-hr/day validation, which queries across all of an employee's timesheets.
            e.HasIndex(te => new { te.Date });
        });

        // ---- TimesheetReviewEvent (Phase 5) ----
        builder.Entity<TimesheetReviewEvent>(e =>
        {
            e.HasOne(re => re.Timesheet)
                .WithMany(t => t.ReviewEvents)
                .HasForeignKey(re => re.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade); // history dies with its timesheet, nothing else

            // Deliberately no FK for TimesheetEntryId or ActorId: the history
            // has to outlive a deleted entry and must never block deleting a
            // person. See the entity's remarks.
            e.Property(re => re.ActorId).HasMaxLength(450);

            // The history is always read whole, for one timesheet, oldest first.
            e.HasIndex(re => new { re.TimesheetId, re.OccurredAtUtc });
        });
    }
}
