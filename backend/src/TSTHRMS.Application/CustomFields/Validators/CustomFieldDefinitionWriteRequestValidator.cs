using FluentValidation;
using TSTHRMS.Application.CustomFields.Dtos;
using TSTHRMS.Domain.CustomFields;

namespace TSTHRMS.Application.CustomFields.Validators;

public class CustomFieldDefinitionWriteRequestValidator : AbstractValidator<CustomFieldDefinitionWriteRequest>
{
    public CustomFieldDefinitionWriteRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z][a-z0-9_]*$")
            .WithMessage("Name must be lowercase letters, numbers, and underscores, starting with a letter.");

        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FieldType).IsInEnum();

        RuleFor(x => x.Options)
            .Must(options => options is { Count: > 0 })
            .When(x => x.FieldType == CustomFieldType.Select)
            .WithMessage("Select fields need at least one option.");
    }
}
