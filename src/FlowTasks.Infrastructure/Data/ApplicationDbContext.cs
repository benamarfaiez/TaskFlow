using FlowTasks.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskProject = FlowTasks.Domain.Entities.TaskProject;

namespace FlowTasks.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<TaskProject> Tasks { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<TaskHistory> TaskHistories { get; set; }
    public DbSet<Sprint> Sprints { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project configuration
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Key).IsUnique();
            entity.HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProjectMember configuration
        builder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(pm => pm.Id);
            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();
            entity.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMembers)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Task configuration
        builder.Entity<TaskProject>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Key).IsUnique();
            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Reporter)
                .WithMany(u => u.ReportedTasks)
                .HasForeignKey(t => t.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Sprint)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.SprintId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Epic)
                .WithMany()
                .HasForeignKey(t => t.EpicId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.Parent)
                .WithMany(t => t.Subtasks)
                .HasForeignKey(t => t.ParentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TaskComment configuration
        builder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(tc => tc.Id);
            entity.HasOne(tc => tc.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tc => tc.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // TaskHistory configuration
        builder.Entity<TaskHistory>(entity =>
        {
            entity.HasKey(th => th.Id);
            entity.HasOne(th => th.Task)
                .WithMany(t => t.History)
                .HasForeignKey(th => th.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(th => th.User)
                .WithMany(u => u.TaskHistories)
                .HasForeignKey(th => th.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sprint configuration
        builder.Entity<Sprint>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Project)
                .WithMany(p => p.Sprints)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

