using FluentValidation;
using TSTHRMS.Application.Documents.Dtos;

namespace TSTHRMS.Application.Documents.Validators;

public class EmployeeDocumentWriteRequestValidator : AbstractValidator<EmployeeDocumentWriteRequest>
{
    public EmployeeDocumentWriteRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
