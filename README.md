# FlowTasks - Backend API

Backend complet pour une application de gestion de projets et de tâches similaire à Jira, développé avec .NET 8.

## 🚀 Stack Technique

- **.NET 8** (ASP.NET Core Web API)
- **Entity Framework Core 8** avec **PostgreSQL** (Npgsql)
- **JWT Authentication** + Refresh Tokens
- **ASP.NET Core Identity** pour les utilisateurs et rôles
- **SignalR** pour les notifications en temps réel
- **Architecture Clean / DDD-light** (API, Application, Domain, Infrastructure)
- **AutoMapper, FluentValidation, MediatR** (CQRS)
- **Swagger** avec authentification JWT
- **CORS** configuré pour Angular (http://localhost:4200)
- **Serilog** pour le logging (console + fichier)
- **Health Checks**

## 📁 Structure du Projet

```
src/
├─ FlowTasks.API          # Web API + Controllers + Program.cs
├─ FlowTasks.Application  # Services, DTOs, Interfaces
├─ FlowTasks.Domain       # Entities, Value Objects, Enums
└─ FlowTasks.Infrastructure # DbContext, Repositories, SignalR Hub, Identity config
```

## 🛠️ Prérequis

- .NET 8 SDK
- PostgreSQL 12+
- Visual Studio 2022 / VS Code 

## 📦 Installation

### 1. Cloner le projet

```bash
git clone <repository-url>
cd FlowTasks
```

### 2. Configurer PostgreSQL

Assurez-vous que PostgreSQL est installé et en cours d'exécution. Créez une base de données :

```sql
CREATE DATABASE FlowTasksDB;
```

### 3. Configurer la connexion

Modifiez `src/FlowTasks.API/appsettings.json` avec vos paramètres PostgreSQL :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FlowTasksDB;Username=postgres;Password=votre_mot_de_passe"
  }
}
```

### 4. Installer les dépendances et créer les migrations

```bash
dotnet restore
cd src/FlowTasks.API
dotnet ef migrations add InitialCreate --project ../FlowTasks.Infrastructure
```

### 5. Appliquer les migrations et créer la base de données

```bash
dotnet ef database update
```

### 6. Lancer l'API

```bash
dotnet run
```

L'API sera accessible sur :
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `http://localhost:5000/swagger` ou `https://localhost:5001/swagger`

## 🔐 Comptes par défaut

Après le seed initial, vous pouvez vous connecter avec :

**Admin:**
- Email: `admin@flowtasks.com`
- Password: `Admin123!`

**Utilisateurs de test:**
- Email: `john@flowtasks.com` / Password: `Test123!`
- Email: `jane@flowtasks.com` / Password: `Test123!`
- Email: `bob@flowtasks.com` / Password: `Test123!`

## 📡 Endpoints API

### Authentification

- `POST /api/auth/register` - Inscription
- `POST /api/auth/login` - Connexion
- `POST /api/auth/refresh` - Rafraîchir le token
- `POST /api/auth/logout` - Déconnexion
- `POST /api/auth/change-password` - Changer le mot de passe

### Utilisateurs

- `GET /api/users/me` - Profil utilisateur
- `PUT /api/users/me` - Mettre à jour le profil
- `GET /api/users` - Liste des utilisateurs
- `GET /api/users/project/{projectId}` - Membres d'un projet

### Projets

- `POST /api/projects` - Créer un projet
- `GET /api/projects` - Liste des projets de l'utilisateur
- `GET /api/projects/{id}` - Détails d'un projet
- `PUT /api/projects/{id}` - Mettre à jour un projet
- `DELETE /api/projects/{id}` - Supprimer un projet

### Membres de Projet

- `POST /api/projects/{projectId}/projectmembers` - Ajouter un membre
- `GET /api/projects/{projectId}/projectmembers` - Liste des membres
- `DELETE /api/projects/{projectId}/projectmembers/{memberId}` - Retirer un membre

### Tâches

- `POST /api/projects/{projectId}/tasks` - Créer une tâche
- `GET /api/projects/{projectId}/tasks` - Liste des tâches (avec filtres, pagination, tri)
- `GET /api/projects/{projectId}/tasks/{id}` - Détails d'une tâche
- `PUT /api/projects/{projectId}/tasks/{id}` - Mettre à jour une tâche
- `DELETE /api/projects/{projectId}/tasks/{id}` - Supprimer une tâche
- `GET /api/projects/{projectId}/tasks/board` - Board Kanban (groupé par statut)

### Commentaires

- `POST /api/tasks/{taskId}/taskcomments` - Ajouter un commentaire
- `GET /api/tasks/{taskId}/taskcomments` - Liste des commentaires
- `PUT /api/tasks/{taskId}/taskcomments/{commentId}` - Modifier un commentaire
- `DELETE /api/tasks/{taskId}/taskcomments/{commentId}` - Supprimer un commentaire

### Historique

- `GET /api/tasks/{taskId}/taskhistory` - Historique d'une tâche

### Sprints

- `POST /api/projects/{projectId}/sprints` - Créer un sprint
- `GET /api/projects/{projectId}/sprints` - Liste des sprints
- `GET /api/projects/{projectId}/sprints/{id}` - Détails d'un sprint
- `PUT /api/projects/{projectId}/sprints/{id}` - Mettre à jour un sprint
- `DELETE /api/projects/{projectId}/sprints/{id}` - Supprimer un sprint

### Health Check

- `GET /health` - Vérification de l'état de l'API

## 🔌 SignalR Hub

Le hub SignalR est disponible sur `/hubs/task` et envoie les événements suivants :

- `TaskCreated` - Nouvelle tâche créée
- `TaskUpdated` - Tâche mise à jour
- `TaskMoved` - Tâche déplacée (changement de statut)
- `TaskDeleted` - Tâche supprimée
- `CommentAdded` - Nouveau commentaire ajouté
- `UserAssigned` - Utilisateur assigné à une tâche

### Connexion au Hub (JavaScript)

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/hubs/task")
    .build();

connection.on("TaskCreated", (taskKey) => {
    console.log("Task created:", taskKey);
});

connection.start().then(() => {
    // Rejoindre un groupe de projet
    connection.invoke("JoinProjectGroup", projectId);
});
```

## 🔒 Authentification JWT

Tous les endpoints (sauf `/api/auth/*`) nécessitent un token JWT dans le header :

```
Authorization: Bearer <your-jwt-token>
```

Dans Swagger, cliquez sur le bouton "Authorize" et entrez `Bearer <token>`.

## 📝 Filtres et Pagination

Les endpoints de liste de tâches supportent :

- **Pagination**: `pageNumber`, `pageSize`
- **Recherche**: `search` (dans summary, description, key)
- **Filtres**: `status`, `type`, `priority`, `assigneeId`, `sprintId`
- **Tri**: `sortBy` (summary, priority, status, dueDate, createdAt), `sortDescending`

Exemple :
```
GET /api/projects/{projectId}/tasks?pageNumber=1&pageSize=20&status=InProgress&sortBy=priority&sortDescending=true
```

## 🧪 Tests

Pour tester l'API :

1. Inscrivez-vous ou connectez-vous via `/api/auth/login`
2. Copiez le `token` de la réponse
3. Dans Swagger, cliquez sur "Authorize" et entrez `Bearer <token>`
4. Testez les endpoints protégés

## 📊 Base de Données

Le seed initial crée :
- 1 utilisateur admin
- 3 utilisateurs de test
- 2 projets d'exemple
- Plusieurs tâches d'exemple

## 🐳 Docker (Optionnel)

Pour utiliser PostgreSQL avec Docker :

```bash
docker run --name flowtasks-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=FlowTasksDB -p 5432:5432 -d postgres:15
```
