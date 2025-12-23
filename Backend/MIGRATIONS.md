# Instructions pour les Migrations Entity Framework

## Créer une migration initiale

```bash
cd src/FlowTasks.API
dotnet ef migrations add InitialCreate --project ../FlowTasks.Infrastructure --startup-project .
```

## Appliquer les migrations à la base de données

```bash
cd src/FlowTasks.API
dotnet ef database update --project ../FlowTasks.Infrastructure --startup-project .
```

## Créer une nouvelle migration

```bash
cd src/FlowTasks.API
dotnet ef migrations add NomDeLaMigration --project ../FlowTasks.Infrastructure --startup-project .
```

## Annuler la dernière migration

```bash
cd src/FlowTasks.API
dotnet ef migrations remove --project ../FlowTasks.Infrastructure --startup-project .
```

## Voir les migrations en attente

```bash
cd src/FlowTasks.API
dotnet ef migrations list --project ../FlowTasks.Infrastructure --startup-project .
```

## Notes

- Assurez-vous que PostgreSQL est en cours d'exécution avant d'appliquer les migrations
- Vérifiez la chaîne de connexion dans `appsettings.json`
- Le seed des données se fait automatiquement au démarrage de l'API (voir `Program.cs`)

