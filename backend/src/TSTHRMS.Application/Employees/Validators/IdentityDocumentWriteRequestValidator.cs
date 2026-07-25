using FluentValidation;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Validators;

public class IdentityDocumentWriteRequestValidator : AbstractValidator<IdentityDocumentWriteRequest>
{
    public IdentityDocumentWriteRequestValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Number).NotEmpty();

        RuleFor(x => x.Number)
            .Matches(@"^[A-Za-z]{5}[0-9]{4}[A-Za-z]$")
            .When(x => x.DocumentType == IdentityDocumentType.Pan)
            .WithMessage("PAN must be 10 characters in the format ABCDE1234F.");

        RuleFor(x => x.Number)
            .Matches(@"^\d{12}$")
            .When(x => x.DocumentType == IdentityDocumentType.Aadhaar)
            .WithMessage("Aadhaar number must be exactly 12 digits.");

        RuleFor(x => x.ExpiryDate)
            .NotNull()
            .When(x => x.DocumentType == IdentityDocumentType.Passport)
            .WithMessage("Expiry date is required for a passport.");
    }
}
