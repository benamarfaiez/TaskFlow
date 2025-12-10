# Guide de Démarrage Rapide - FlowTasks

## 🚀 Démarrage en 5 minutes

### 1. Prérequis
- .NET 8 SDK installé
- PostgreSQL en cours d'exécution

### 2. Configuration PostgreSQL

Créez la base de données :
```sql
CREATE DATABASE FlowTasksDB;
```

### 3. Configuration de la connexion

Éditez `src/FlowTasks.API/appsettings.json` :
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FlowTasksDB;Username=postgres;Password=votre_mot_de_passe"
  }
}
```

### 4. Commandes de démarrage

```bash
# Restaurer les packages NuGet
dotnet restore

# Créer les migrations
cd src/FlowTasks.API
dotnet ef migrations add InitialCreate --project ../FlowTasks.Infrastructure

# Appliquer les migrations et créer la base de données
dotnet ef database update --project ../FlowTasks.Infrastructure

# Lancer l'API
dotnet run
```

### 5. Accéder à l'API

- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **SignalR Hub**: ws://localhost:5000/hubs/task

### 6. Se connecter

Utilisez les identifiants par défaut :
- Email: `admin@flowtasks.com`
- Password: `Admin123!`

Dans Swagger :
1. Cliquez sur `/api/auth/login`
2. Entrez les identifiants
3. Copiez le `token` de la réponse
4. Cliquez sur "Authorize" en haut de Swagger
5. Entrez `Bearer <votre-token>`
6. Testez les endpoints protégés !

## 📝 Notes importantes

- Le seed des données se fait automatiquement au premier démarrage
- Les logs sont écrits dans le dossier `logs/`
- CORS est configuré pour `http://localhost:4200` (Angular)

## 🐛 Dépannage

**Erreur de connexion PostgreSQL** :
- Vérifiez que PostgreSQL est en cours d'exécution
- Vérifiez la chaîne de connexion dans `appsettings.json`

**Erreur de migration** :
- Supprimez le dossier `Migrations` et recréez la migration
- Vérifiez que la base de données existe

**Port déjà utilisé** :
- Modifiez les ports dans `Properties/launchSettings.json`

