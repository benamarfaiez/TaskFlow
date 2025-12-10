using FlowTasks.Domain.Entities;
using FlowTasks.Domain.Enums;
using FlowTasks.Infrastructure.Repositories;

namespace FlowTasks.Application.Services;

/// <summary>
/// Exemple de service montrant l'utilisation des transactions avec UnitOfWork.
/// Ce service illustre comment gérer des opérations complexes nécessitant une transaction.
/// </summary>
public class ExampleTransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExampleTransactionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Exemple : Créer un projet avec plusieurs tâches dans une transaction.
    /// Si une erreur survient, toutes les opérations sont annulées.
    /// </summary>
    public async Task<Project> CreateProjectWithTasksAsync(
        string userId,
        string projectKey,
        string projectName,
        List<string> taskSummaries)
    {
        try
        {
            // Démarrer une transaction
            await _unitOfWork.BeginTransactionAsync();

            // Créer le projet
            var project = new Project
            {
                Key = projectKey.ToUpper(),
                Name = projectName,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Projects.AddAsync(project);

            // Ajouter le propriétaire comme membre admin
            await _unitOfWork.ProjectMembers.AddAsync(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = userId,
                Role = ProjectRole.Admin
            });

            // Créer les tâches
            var tasks = new List<TaskProject>();
            for (int i = 0; i < taskSummaries.Count; i++)
            {
                var task = new TaskProject
                {
                    Key = $"{project.Key}-{i + 1}",
                    Summary = taskSummaries[i],
                    ProjectId = project.Id,
                    ReporterId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Tasks.AddAsync(task);
                tasks.Add(task);
            }

            // Sauvegarder toutes les modifications
            await _unitOfWork.CompleteAsync();

            // Valider la transaction
            await _unitOfWork.CommitTransactionAsync();

            return project;
        }
        catch (Exception)
        {
            // En cas d'erreur, annuler toutes les modifications
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Exemple : Déplacer toutes les tâches d'un sprint vers un autre dans une transaction.
    /// </summary>
    public async Task MoveTasksBetweenSprintsAsync(
        string fromSprintId,
        string toSprintId,
        string userId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Récupérer les tâches du sprint source
            var tasks = await _unitOfWork.Tasks.GetBySprintIdAsync(fromSprintId);

            // Vérifier que le sprint de destination existe
            var toSprint = await _unitOfWork.Sprints.GetByIdAsync(toSprintId);
            if (toSprint == null)
            {
                throw new InvalidOperationException("Sprint de destination introuvable");
            }

            // Déplacer chaque tâche
            foreach (var task in tasks)
            {
                task.SprintId = toSprintId;
                task.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Tasks.Update(task);
            }

            // Sauvegarder et valider
            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Exemple : Supprimer un projet et toutes ses données associées dans une transaction.
    /// </summary>
    public async Task DeleteProjectWithAllDataAsync(string projectId, string userId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Vérifier les permissions (exemple simplifié)
            var isAdmin = await _unitOfWork.ProjectMembers.ExistsAsync(
                pm => pm.ProjectId == projectId && 
                      pm.UserId == userId && 
                      pm.Role == ProjectRole.Admin);

            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Seuls les admins peuvent supprimer un projet");
            }

            // Récupérer toutes les tâches du projet
            var tasks = await _unitOfWork.Tasks.GetByProjectIdAsync(projectId);

            // Supprimer tous les commentaires des tâches
            foreach (var task in tasks)
            {
                var comments = await _unitOfWork.TaskComments.GetByTaskIdAsync(task.Id);
                _unitOfWork.TaskComments.DeleteRange(comments);
            }

            // Supprimer toutes les tâches
            _unitOfWork.Tasks.DeleteRange(tasks);

            // Supprimer tous les sprints
            var sprints = await _unitOfWork.Sprints.GetByProjectIdAsync(projectId);
            _unitOfWork.Sprints.DeleteRange(sprints);

            // Supprimer tous les membres
            var members = await _unitOfWork.ProjectMembers.GetByProjectIdAsync(projectId);
            _unitOfWork.ProjectMembers.DeleteRange(members);

            // Supprimer le projet
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project != null)
            {
                _unitOfWork.Projects.Delete(project);
            }

            // Sauvegarder et valider
            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}

