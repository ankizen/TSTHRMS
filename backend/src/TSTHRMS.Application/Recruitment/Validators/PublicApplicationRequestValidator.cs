using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class PublicApplicationRequestValidator : AbstractValidator<PublicApplicationRequest>
{
    public PublicApplicationRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CurrentCtc).GreaterThanOrEqualTo(0).When(x => x.CurrentCtc is not null);
        RuleFor(x => x.ExpectedCtc).GreaterThanOrEqualTo(0).When(x => x.ExpectedCtc is not null);
        RuleFor(x => x.NoticePeriodDays).GreaterThanOrEqualTo(0).When(x => x.NoticePeriodDays is not null);
        RuleFor(x => x.ConsentGiven).Equal(true)
            .WithMessage("Please provide consent to store and process your application data.");
    }
}
