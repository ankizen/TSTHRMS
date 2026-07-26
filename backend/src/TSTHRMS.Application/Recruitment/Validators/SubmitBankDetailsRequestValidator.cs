using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class SubmitBankDetailsRequestValidator : AbstractValidator<SubmitBankDetailsRequest>
{
    public SubmitBankDetailsRequestValidator()
    {
        RuleFor(x => x.BankAccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BankIfscCode)
            .NotEmpty()
            .Matches(@"^[A-Za-z]{4}0[A-Za-z0-9]{6}$")
            .WithMessage("IFSC code must be 11 characters, e.g. HDFC0001234.");
    }
}
