using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

public interface IEmployeeBulkImportService
{
    /// <summary>An .xlsx workbook with the expected header row plus one clearly-labelled example
    /// row to delete before uploading.</summary>
    byte[] GetTemplate();

    /// <summary>Parses and validates every row without creating anything - the pre-commit
    /// preview the row-level error report is built from.</summary>
    Task<BulkImportSummaryDto> ValidateAsync(Stream fileContent, CancellationToken cancellationToken = default);

    /// <summary>Re-parses and validates, then creates an employee for every row that passes -
    /// stateless by design (no separate "confirm this previously-validated upload" step) so a
    /// re-uploaded, corrected file is always re-validated against current data.</summary>
    Task<BulkImportSummaryDto> CommitAsync(Stream fileContent, CancellationToken cancellationToken = default);
}
