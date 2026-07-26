using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TSTHRMS.Application.Auditing;
using TSTHRMS.Application.Auth;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.CustomFields;
using TSTHRMS.Application.Documents;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Users;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Infrastructure.Auth;
using TSTHRMS.Infrastructure.Email;
using TSTHRMS.Infrastructure.Identity;
using TSTHRMS.Infrastructure.Persistence;
using TSTHRMS.Infrastructure.Persistence.Interceptors;
using TSTHRMS.Infrastructure.Storage;
using TSTHRMS.Infrastructure.Web;

namespace TSTHRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

            // Fixed server version (not ServerVersion.AutoDetect) so migrations/design-time
            // tooling don't require a live database connection to run.
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 0)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure());

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ISequenceGenerator, SequenceGenerator>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeBulkImportService, EmployeeBulkImportService>();

        services.Configure<LocalFileStorageOptions>(configuration.GetSection(LocalFileStorageOptions.SectionName));
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IEducationService, EducationService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IPreviousEmploymentService, PreviousEmploymentService>();
        services.AddScoped<IIdentityDocumentService, IdentityDocumentService>();
        services.AddScoped<INomineeService, NomineeService>();
        services.AddScoped<IDocumentRepositoryService, DocumentRepositoryService>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IMyProfileService, MyProfileService>();
        services.AddScoped<IEmployeeEditRequestService, EmployeeEditRequestService>();
        services.AddScoped<ICustomFieldService, CustomFieldService>();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IJobRequisitionService, JobRequisitionService>();
        services.AddScoped<ICareerSiteService, CareerSiteService>();
        services.AddScoped<IApplicantService, ApplicantService>();
        services.AddScoped<IInterviewService, InterviewService>();

        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));
        services.AddScoped<IFrontendLinkBuilder, FrontendLinkBuilder>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<ICandidatePortalAuthService, CandidatePortalAuthService>();
        services.AddScoped<ICandidatePortalService, CandidatePortalService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IBackgroundVerificationService, BackgroundVerificationService>();
        services.AddScoped<IPreboardingService, PreboardingService>();
        services.AddScoped<IOnboardingService, OnboardingService>();

        return services;
    }
}
