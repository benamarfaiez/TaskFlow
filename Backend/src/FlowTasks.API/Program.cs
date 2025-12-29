using FlowTasks.Application.Interfaces;
using FlowTasks.Application.Services;
using FlowTasks.Domain.Entities;
using FlowTasks.Infrastructure;
using FlowTasks.Infrastructure.Data;
using FlowTasks.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/flowtasks-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FlowTasks API",
        Version = "v1",
        Description = "API for FlowTasks - Project and Task Management System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var dbConnectin = builder.Configuration["DbConnection"];
// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dbConnectin));

// Identity configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "ApplicationDbContext Check");

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(15);
    setup.MaximumHistoryEntriesPerEndpoint(60);
    setup.AddHealthCheckEndpoint("FlowTasks API", "/health");
})
.AddInMemoryStorage();

// JWT Authentication
var jwtKey = builder.Configuration["JwtKey"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlowTasks";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FlowTasks";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var UrlFrontend = builder.Configuration["UrlFrontend"];
// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(UrlFrontend)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// Repositories and UnitOfWork
builder.Services.AddRepositories();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITaskCommentService, TaskCommentService>();
builder.Services.AddScoped<ITaskHistoryService, TaskHistoryService>();
builder.Services.AddScoped<ISprintService, SprintService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlowTasks API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TaskHub>("/hubs/task");
app.MapHealthChecks("/health");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

