using ClosedXML.Excel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Application.Employees;

/// <summary>
/// Section 13: bulk create employees from a spreadsheet. Deliberately covers a practical subset
/// of EmployeeWriteRequest rather than every field - reporting manager, probation/contract
/// overrides, and date-of-birth proof type are left for the edit form afterwards, since resolving
/// a manager by name would need a second pass (the manager might be in the same file) and isn't
/// worth the complexity for a first cut of this feature.
/// </summary>
public class EmployeeBulkImportService(
    IApplicationDbContext dbContext,
    IEmployeeService employeeService,
    IValidator<EmployeeWriteRequest> validator) : IEmployeeBulkImportService
{
    private static readonly string[] Columns =
    [
        "Legal Entity", "Product", "First Name", "Last Name", "Gender", "Date of Birth",
        "Personal Email", "Personal Phone", "Date of Joining", "Designation", "Grade",
        "Department", "Work Location", "Employment Type", "Monthly Gross Salary",
        "Professional Tax State", "Bank Account Number", "Bank IFSC Code"
    ];

    public byte[] GetTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");

        for (var col = 0; col < Columns.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = Columns[col];
        }

        sheet.Row(1).Style.Font.Bold = true;

        string[] example =
        [
            "EXAMPLE - delete this row", "Test Product", "Jane", "Doe", "Female", "1990-01-01",
            "jane.doe@example.com", "9999999999", "2024-01-15", "Software Engineer", "L2",
            "Engineering", "Mumbai HQ", "FullTime", "50000", "Maharashtra", "1234567890123456", "HDFC0001234"
        ];
        for (var col = 0; col < example.Length; col++)
        {
            sheet.Cell(2, col + 1).Value = example[col];
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<BulkImportSummaryDto> ValidateAsync(Stream fileContent, CancellationToken cancellationToken = default)
    {
        var rows = await ParseRowsAsync(fileContent, cancellationToken);
        return Summarize(rows, createdCount: 0);
    }

    public async Task<BulkImportSummaryDto> CommitAsync(Stream fileContent, CancellationToken cancellationToken = default)
    {
        var rows = await ParseRowsAsync(fileContent, cancellationToken);
        var createdCount = 0;

        foreach (var row in rows)
        {
            if (row.Request is null || row.Errors.Count > 0)
            {
                continue;
            }

            var created = await employeeService.CreateAsync(row.Request, cancellationToken);
            row.EmployeeCode = created.EmployeeCode;
            createdCount++;
        }

        return Summarize(rows, createdCount);
    }

    private async Task<List<ParsedRow>> ParseRowsAsync(Stream fileContent, CancellationToken cancellationToken)
    {
        var legalEntities = await dbContext.LegalEntities.AsNoTracking().ToListAsync(cancellationToken);
        var products = await dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook(fileContent);
        var sheet = workbook.Worksheets.First();

        var rows = new List<ParsedRow>();
        var rowNumber = 1; // row 1 is the header

        foreach (var excelRow in sheet.RowsUsed().Skip(1))
        {
            rowNumber++;

            if (excelRow.Cells().All(c => string.IsNullOrWhiteSpace(c.GetString())))
            {
                continue;
            }

            rows.Add(await ParseRowAsync(excelRow, rowNumber, legalEntities, products, cancellationToken));
        }

        return rows;
    }

    private async Task<ParsedRow> ParseRowAsync(
        IXLRow excelRow, int rowNumber, List<LegalEntity> legalEntities, List<Product> products,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        string? Cell(int col) => excelRow.Cell(col).GetString() is { Length: > 0 } s ? s.Trim() : null;

        var legalEntityName = Cell(1);
        var productName = Cell(2);
        var firstName = Cell(3);
        var lastName = Cell(4);
        var genderText = Cell(5);
        var dobText = Cell(6);
        var personalEmail = Cell(7);
        var personalPhone = Cell(8);
        var dojText = Cell(9);
        var designation = Cell(10);
        var grade = Cell(11);
        var department = Cell(12);
        var workLocation = Cell(13);
        var employmentTypeText = Cell(14);
        var salaryText = Cell(15);
        var professionalTaxState = Cell(16);
        var bankAccountNumber = Cell(17);
        var bankIfscCode = Cell(18);

        var legalEntity = legalEntities.FirstOrDefault(e => string.Equals(e.Name, legalEntityName, StringComparison.OrdinalIgnoreCase));
        if (legalEntityName is null)
        {
            errors.Add("Legal Entity is required.");
        }
        else if (legalEntity is null)
        {
            errors.Add($"Legal Entity '{legalEntityName}' was not found.");
        }

        var product = products.FirstOrDefault(p => string.Equals(p.Name, productName, StringComparison.OrdinalIgnoreCase));
        if (productName is null)
        {
            errors.Add("Product is required.");
        }
        else if (product is null)
        {
            errors.Add($"Product '{productName}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add("First Name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add("Last Name is required.");
        }

        var gender = Gender.PreferNotToSay;
        if (string.IsNullOrWhiteSpace(genderText))
        {
            errors.Add("Gender is required.");
        }
        else if (!Enum.TryParse(genderText, ignoreCase: true, out gender))
        {
            errors.Add($"Gender '{genderText}' is not valid (Male, Female, Other, PreferNotToSay).");
        }

        DateOnly? dateOfBirth = null;
        if (!string.IsNullOrWhiteSpace(dobText))
        {
            if (DateOnly.TryParse(dobText, out var parsedDob))
            {
                dateOfBirth = parsedDob;
            }
            else
            {
                errors.Add($"Date of Birth '{dobText}' is not a valid date (expected yyyy-MM-dd).");
            }
        }

        var dateOfJoining = default(DateOnly);
        if (string.IsNullOrWhiteSpace(dojText))
        {
            errors.Add("Date of Joining is required.");
        }
        else if (!DateOnly.TryParse(dojText, out dateOfJoining))
        {
            errors.Add($"Date of Joining '{dojText}' is not a valid date (expected yyyy-MM-dd).");
        }

        var employmentType = EmploymentType.FullTime;
        if (string.IsNullOrWhiteSpace(employmentTypeText))
        {
            errors.Add("Employment Type is required.");
        }
        else if (!Enum.TryParse(employmentTypeText, ignoreCase: true, out employmentType))
        {
            errors.Add($"Employment Type '{employmentTypeText}' is not valid (FullTime, Contract, Intern).");
        }

        decimal? salary = null;
        if (!string.IsNullOrWhiteSpace(salaryText))
        {
            if (decimal.TryParse(salaryText, out var parsedSalary))
            {
                salary = parsedSalary;
            }
            else
            {
                errors.Add($"Monthly Gross Salary '{salaryText}' is not a valid number.");
            }
        }

        EmployeeWriteRequest? request = null;
        if (errors.Count == 0)
        {
            request = new EmployeeWriteRequest(
                legalEntity!.Id, product!.Id, firstName!, lastName!, gender, dateOfBirth,
                personalEmail, personalPhone, null, null, null, null, null,
                bankAccountNumber, bankIfscCode, dateOfJoining, designation, grade, department,
                workLocation, null, employmentType, salary, null, professionalTaxState, null, null, null);

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
            }
        }

        return new ParsedRow(rowNumber, request, errors);
    }

    private static BulkImportSummaryDto Summarize(List<ParsedRow> rows, int createdCount)
    {
        var rowDtos = rows
            .Select(r => new BulkImportRowResultDto(r.RowNumber, r.Request is not null && r.Errors.Count == 0, r.EmployeeCode, r.Errors))
            .ToList();

        return new BulkImportSummaryDto(
            rowDtos.Count,
            rowDtos.Count(r => r.IsValid),
            rowDtos.Count(r => !r.IsValid),
            createdCount,
            rowDtos);
    }

    private class ParsedRow(int rowNumber, EmployeeWriteRequest? request, List<string> errors)
    {
        public int RowNumber { get; } = rowNumber;
        public EmployeeWriteRequest? Request { get; } = request;
        public List<string> Errors { get; } = errors;
        public string? EmployeeCode { get; set; }
    }
}
