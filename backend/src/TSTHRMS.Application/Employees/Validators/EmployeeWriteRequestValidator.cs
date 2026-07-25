using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees.Validators;

public class EmployeeWriteRequestValidator : AbstractValidator<EmployeeWriteRequest>
{
    public EmployeeWriteRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.EmploymentType).IsInEnum();

        RuleFor(x => x.LegalEntityId).NotEqual(Guid.Empty).WithMessage("Legal entity is required.");
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).WithMessage("Product is required.");

        RuleFor(x => x.PersonalEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail))
            .WithMessage("Enter a valid personal email address.");

        RuleFor(x => x.DateOfJoining)
            .NotEqual(default(DateOnly))
            .WithMessage("Date of joining is required.")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Date of joining cannot be in the future.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(x => x.DateOfJoining)
            .When(x => x.DateOfBirth is not null)
            .WithMessage("Date of birth must be before the date of joining.");

        RuleFor(x => x.BankIfscCode)
            .Matches(@"^[A-Za-z]{4}0[A-Za-z0-9]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.BankIfscCode))
            .WithMessage("IFSC code must be 11 characters, e.g. HDFC0001234.");

        RuleFor(x => x.ReportingManagerId)
            .NotEqual(Guid.Empty)
            .When(x => x.ReportingManagerId is not null)
            .WithMessage("Select a valid reporting manager.");
    }
}
