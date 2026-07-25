using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class RequestCandidateOtpRequestValidator : AbstractValidator<RequestCandidateOtpRequest>
{
    public RequestCandidateOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
