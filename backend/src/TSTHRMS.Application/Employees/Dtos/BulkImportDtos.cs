namespace TSTHRMS.Application.Employees.Dtos;

public record BulkImportRowResultDto(int RowNumber, bool IsValid, string? EmployeeCode, IReadOnlyList<string> Errors);

/// <summary>Same shape for a validate-only preview and an actual commit - CreatedCount is 0 for
/// the former since nothing is persisted until Commit runs.</summary>
public record BulkImportSummaryDto(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    int CreatedCount,
    IReadOnlyList<BulkImportRowResultDto> Rows);
