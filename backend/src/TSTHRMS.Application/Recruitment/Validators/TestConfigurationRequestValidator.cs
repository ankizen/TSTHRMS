using FluentValidation;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment.Validators;

public class TestConfigurationRequestValidator : AbstractValidator<TestConfigurationRequest>
{
    public TestConfigurationRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Instructions).MaximumLength(4000);
        RuleFor(x => x.TimeLimitMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.ResponseWindowDays).InclusiveBetween(1, 30);
        RuleFor(x => x.PassThreshold).InclusiveBetween(0, 100);
        RuleFor(x => x.RetakeCooldownMonths).InclusiveBetween(0, 24);
    }
}
