using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class SendOfferRequestValidator : AbstractValidator<SendOfferRequest>
{
    public SendOfferRequestValidator()
    {
        RuleFor(x => x.ResponseWindowDays).InclusiveBetween(1, 30);
    }
}
