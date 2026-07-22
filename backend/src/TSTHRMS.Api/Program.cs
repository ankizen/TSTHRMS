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

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LocalDev", policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.UseCors("LocalDev");

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

    // Serves the built React SPA from wwwroot for single-origin production deployment
    // (frontend/dist is copied there by the publish-time MSBuild target). No-op locally
    // since wwwroot is empty during `dotnet run` - the Vite dev server serves the SPA instead.
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
