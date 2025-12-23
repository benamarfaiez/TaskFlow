using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskStatus = FlowTasks.Domain.Enums.TaskStatus;

namespace FlowTasks.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Create roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        // Create admin user
        var adminEmail = "admin@flowtasks.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Create test users
        var testUsers = new[]
        {
            new { Email = "john@flowtasks.com", FirstName = "John", LastName = "Doe", Password = "Test123!" },
            new { Email = "jane@flowtasks.com", FirstName = "Jane", LastName = "Smith", Password = "Test123!" },
            new { Email = "bob@flowtasks.com", FirstName = "Bob", LastName = "Johnson", Password = "Test123!" }
        };

        foreach (var testUser in testUsers)
        {
            var user = await userManager.FindByEmailAsync(testUser.Email);
            if (user == null)
            {
                user = new User
                {
                    UserName = testUser.Email,
                    Email = testUser.Email,
                    FirstName = testUser.FirstName,
                    LastName = testUser.LastName,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, testUser.Password);
                await userManager.AddToRoleAsync(user, "User");
            }
        }

        // Create projects
        if (!await context.Projects.AnyAsync())
        {
            var project1 = new Project
            {
                Key = "FLOW",
                Name = "FlowTasks Main Project",
                Description = "Main project for FlowTasks development",
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Projects.Add(project1);

            var project2 = new Project
            {
                Key = "DEMO",
                Name = "Demo Project",
                Description = "Demo project for testing",
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Projects.Add(project2);

            await context.SaveChangesAsync();

            // Add members to projects
            var john = await userManager.FindByEmailAsync("john@flowtasks.com");
            var jane = await userManager.FindByEmailAsync("jane@flowtasks.com");

            if (john != null && jane != null)
            {
                context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project1.Id,
                    UserId = john.Id,
                    Role = ProjectRole.Member
                });
                context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project1.Id,
                    UserId = jane.Id,
                    Role = ProjectRole.Admin
                });
                context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project2.Id,
                    UserId = john.Id,
                    Role = ProjectRole.Member
                });
            }

            await context.SaveChangesAsync();

            // Create tasks
            var task1 = new TaskProject
            {
                Key = "FLOW-1",
                Summary = "Implement authentication system",
                Description = "Create JWT authentication with refresh tokens",
                Type = TaskType.Story,
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.High,
                ProjectId = project1.Id,
                ReporterId = adminUser.Id,
                AssigneeId = john?.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task1);

            var task2 = new TaskProject
            {
                Key = "FLOW-2",
                Summary = "Fix login bug",
                Description = "Users cannot login with special characters in password",
                Type = TaskType.Bug,
                Status = TaskStatus.ToDo,
                Priority = TaskPriority.High,
                ProjectId = project1.Id,
                ReporterId = john?.Id ?? adminUser.Id,
                AssigneeId = jane?.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(task2);

            var epic1 = new TaskProject
            {
                Key = "FLOW-3",
                Summary = "User Management Epic",
                Description = "Complete user management features",
                Type = TaskType.Epic,
                Status = TaskStatus.ToDo,
                Priority = TaskPriority.Medium,
                ProjectId = project1.Id,
                ReporterId = adminUser.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(epic1);

            await context.SaveChangesAsync();

            // Create comments
            if (john != null)
            {
                context.TaskComments.Add(new TaskComment
                {
                    TaskId = task1.Id,
                    UserId = john.Id,
                    Content = "Working on JWT implementation",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }
}

