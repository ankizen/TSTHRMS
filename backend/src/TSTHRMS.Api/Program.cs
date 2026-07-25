using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using TSTHRMS.Api.Extensions;
using TSTHRMS.Api.Filters;
using TSTHRMS.Api.Middleware;
using TSTHRMS.Api.Services;
using TSTHRMS.Application.Auth.Validators;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Infrastructure;
using TSTHRMS.Infrastructure.Identity;
using TSTHRMS.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    builder.Services
        .AddControllers(options => options.Filters.Add<ValidationFilter>())
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddHealthChecks();

    // "Default" is what a split deployment (frontend on Vercel, API on Coolify - different
    // origins) actually uses, driven by config (Cors:AllowedOrigins, e.g. env var
    // Cors__AllowedOrigins__0) so the allowed origin never needs a code change. "LocalDev" stays
    // hardcoded to the Vite dev server port since that's always the same locally.
    var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalDev", policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());

        options.AddPolicy("Default", policy => policy
            .WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseCors(app.Environment.IsDevelopment() ? "LocalDev" : "Default");

    // Runs in every environment (not just Development) so a fresh Coolify/container deploy
    // comes up with an up-to-date schema and the initial HRAdmin seeded automatically -
    // migrations are idempotent, so this is a no-op on every restart after the first. Disable
    // via Database:MigrateOnStartup=false (e.g. env var Database__MigrateOnStartup) if migrations
    // should instead run as a separate release step later.
    if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        await ApplicationDbContextSeed.SeedAsync(
            db, userManager, roleManager, app.Configuration, app.Logger);
    }

    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.MapHealthChecks("/health");

    // Serves the built React SPA from wwwroot for single-origin deployment (frontend/dist copied
    // there by the publish-time MSBuild target - see docs/deployment-windows-server-iis.md).
    // The Docker image (docs/deployment-coolify-vercel.md) skips that target, so wwwroot stays
    // empty and this is a harmless no-op there too, same as it is locally under `dotnet run`.
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TSTHRMS.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
