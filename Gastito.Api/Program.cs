using Gastito.Presentation;
using Gastito.Presentation.Middlewares;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Bind Serilog from appsettings
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

    // Register chained layers: Presentation -> BusinessLogic -> DataAccess
    builder.Services.AddPresentation(builder.Configuration);

    // CORS configuration from appsettings
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                         ?? new[] { "http://localhost:3000", "http://localhost:5173" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Swagger / OpenAPI documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "GASTITO API",
            Version = "v1",
            Description = "API REST para registrar y organizar ingresos y gastos - GASTITO"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese el token JWT devuelto por /api/v1/auth/login"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    var app = builder.Build();

    // 1. Global Exception Handler Middleware (RFC 7807 ProblemDetails)
    app.UseGlobalExceptionHandler();

    // 2. Serilog HTTP Request Logging
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment() || true) // enable Swagger in all development profiles
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "GASTITO API v1");
        });
    }

    app.UseCors("FrontendCorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/", () => Results.Redirect("/swagger"));

    // Apply pending migrations automatically on startup if using relational database (Docker / Production)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Gastito.DataAccess.AppDbContext>();
        if (Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.IsRelational(db.Database))
        {
            try
            {
                Log.Information("Aplicando migraciones pendientes de base de datos...");
                Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
                Log.Information("Migraciones aplicadas exitosamente.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al aplicar migraciones en el arranque de la API.");
            }
        }
    }

    app.Run();


public partial class Program { }
