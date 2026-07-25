using ClosedXML.Excel;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

/// <summary>
/// ClosedXML (MIT-licensed) rather than EPPlus, since EPPlus's non-commercial license would be
/// a legal problem for a product sold to other companies.
/// </summary>
public static class EmployeeExcelExporter
{
    public static byte[] Export(IReadOnlyList<EmployeeListItemDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");

        string[] headers =
        [
            "Employee Code", "First Name", "Last Name", "Legal Entity", "Product",
            "Department", "Designation", "Work Location", "Status"
        ];

        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        sheet.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRow = i + 2;

            sheet.Cell(excelRow, 1).Value = row.EmployeeCode;
            sheet.Cell(excelRow, 2).Value = row.FirstName;
            sheet.Cell(excelRow, 3).Value = row.LastName;
            sheet.Cell(excelRow, 4).Value = row.LegalEntityName;
            sheet.Cell(excelRow, 5).Value = row.ProductName;
            sheet.Cell(excelRow, 6).Value = row.Department;
            sheet.Cell(excelRow, 7).Value = row.Designation;
            sheet.Cell(excelRow, 8).Value = row.WorkLocation;
            sheet.Cell(excelRow, 9).Value = row.Status.ToString();
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
