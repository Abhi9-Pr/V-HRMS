using System.Globalization;
using Asp.Versioning;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Employees.Import;
using Vespera.Domain.Common;

namespace Vespera.Api.Controllers.V1;

/// <summary>
/// CSV/Excel employee import. Parsing happens here, not in Application — it's a request-format
/// concern, and the parsed <see cref="ImportEmployeeRowDto"/> shape is what actually crosses the
/// MediatR boundary. <c>dryRun</c> defaults to <c>true</c> so an accidental omission never
/// commits data by surprise.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees/import")]
public sealed class EmployeeImportController : ControllerBase
{
    private static readonly string[] RequiredColumns =
    [
        "Code", "FirstName", "LastName", "WorkEmail", "Phone",
        "DateOfBirth", "DateOfJoining", "DepartmentCode", "DesignationTitle", "LocationName",
    ];

    private readonly ISender _sender;

    public EmployeeImportController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The dry-run or commit report, with one result per row.</response>
    /// <response code="400">The file couldn't be parsed (wrong columns, unreadable dates, etc).</response>
    [HttpPost]
    [HasPermission(Permissions.EmployeeImport.Manage)]
    [ProducesResponseType(typeof(BulkImportReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import(
        IFormFile file, [FromQuery] bool? dryRun, CancellationToken cancellationToken)
    {
        IReadOnlyList<ImportEmployeeRowDto> rows;
        try
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            await using var stream = file.OpenReadStream();
            rows = extension switch
            {
                ".csv" => ParseCsv(stream),
                ".xlsx" => ParseExcel(stream),
                _ => throw new InvalidDataException("Only .csv and .xlsx files are supported."),
            };
        }
        catch (Exception ex) when (ex is InvalidDataException or CsvHelperException or FormatException)
        {
            var error = Error.Validation("employee_import.unreadable_file", ex.Message);
            return Result.Failure<BulkImportReportDto>(error).ToActionResult(this);
        }

        var command = new ImportEmployeesCommand(rows, dryRun ?? true);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    private static List<ImportEmployeeRowDto> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
        {
            throw new InvalidDataException("The CSV file has no header row.");
        }

        EnsureRequiredColumns(csv.HeaderRecord);

        var rows = new List<ImportEmployeeRowDto>();
        var rowNumber = 1;
        while (csv.Read())
        {
            rowNumber++;
            rows.Add(ToRow(rowNumber, name => csv.GetField(name) ?? string.Empty));
        }

        return rows;
    }

    private static List<ImportEmployeeRowDto> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var headerRow = worksheet.Row(1);
        var columnIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            columnIndexByName[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        }

        EnsureRequiredColumns(columnIndexByName.Keys.ToArray());

        var rows = new List<ImportEmployeeRowDto>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            string Field(string name) => row.Cell(columnIndexByName[name]).GetString();
            rows.Add(ToRow(row.RowNumber(), Field));
        }

        return rows;
    }

    private static void EnsureRequiredColumns(IReadOnlyCollection<string> presentColumns)
    {
        var missing = RequiredColumns.Where(required => !presentColumns.Contains(required, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException($"Missing required column(s): {string.Join(", ", missing)}.");
        }
    }

    private static ImportEmployeeRowDto ToRow(int rowNumber, Func<string, string> field)
    {
        return new ImportEmployeeRowDto(
            rowNumber,
            field("Code"),
            field("FirstName"),
            field("LastName"),
            field("WorkEmail"),
            field("Phone"),
            DateOnly.Parse(field("DateOfBirth"), CultureInfo.InvariantCulture),
            DateOnly.Parse(field("DateOfJoining"), CultureInfo.InvariantCulture),
            field("DepartmentCode"),
            field("DesignationTitle"),
            field("LocationName"));
    }
}
