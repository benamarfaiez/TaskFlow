using FlowTasks.Infrastructure.Interfaces;
using FlowTasks.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FlowTasks.Infrastructure;

public static class CollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specific Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddScoped<ITaskHistoryRepository, TaskHistoryRepository>();

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

