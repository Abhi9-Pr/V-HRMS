using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Payroll;

/// <summary>
/// Renders a payslip PDF with QuestPDF. The layout — header, employee details, two-column
/// earnings/deductions tables, net pay, a diagonal "CONFIDENTIAL" watermark, a system-generated
/// footer — is a conventional Indian payslip invented for this build, not a match against a real
/// spec (see <see cref="IPayslipRenderer"/>'s remarks). Password protection and the document hash
/// used for tamper detection both happen after this returns, in the caller and
/// <see cref="IPdfPasswordProtector"/> respectively — this class only lays out content.
/// </summary>
public sealed class QuestPdfPayslipRenderer : IPayslipRenderer
{
    private static readonly string[] MonthNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    public byte[] Render(PayslipRenderRequest request)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Background().AlignCenter().AlignMiddle().Rotate(-30)
                    .Text("CONFIDENTIAL").FontSize(64).FontColor(Colors.Grey.Lighten3).Bold();

                page.Header().Column(column =>
                {
                    column.Item().Text(request.CompanyName).FontSize(16).Bold();
                    column.Item().Text($"Payslip for {MonthNames[request.Month - 1]} {request.Year}").FontSize(12);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Text($"Employee: {request.EmployeeName} ({request.EmployeeCode})");
                        table.Cell().Text($"Designation: {request.Designation}");
                        table.Cell().Text($"Department: {request.Department}");
                        table.Cell().Text(
                            request.LossOfPayDays > 0
                                ? $"Loss of Pay: {request.LossOfPayDays} day(s)"
                                : string.Empty);
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(earnings =>
                        {
                            earnings.Item().Text("Earnings").Bold();
                            foreach (var line in request.EarningLines)
                            {
                                earnings.Item().Row(lineRow =>
                                {
                                    lineRow.RelativeItem().Text(line.ComponentName);
                                    lineRow.ConstantItem(80).AlignRight().Text($"{line.Amount:N2}");
                                });
                            }

                            earnings.Item().PaddingTop(4).Row(totalRow =>
                            {
                                totalRow.RelativeItem().Text("Gross Pay").Bold();
                                totalRow.ConstantItem(80).AlignRight().Text($"{request.GrossPay:N2}").Bold();
                            });
                        });

                        row.RelativeItem().Column(deductions =>
                        {
                            deductions.Item().Text("Deductions").Bold();
                            foreach (var line in request.DeductionLines)
                            {
                                deductions.Item().Row(lineRow =>
                                {
                                    lineRow.RelativeItem().Text(line.ComponentName);
                                    lineRow.ConstantItem(80).AlignRight().Text($"{line.Amount:N2}");
                                });
                            }

                            deductions.Item().PaddingTop(4).Row(totalRow =>
                            {
                                totalRow.RelativeItem().Text("Total Deductions").Bold();
                                totalRow.ConstantItem(80).AlignRight().Text($"{request.TotalDeductions:N2}").Bold();
                            });
                        });
                    });

                    column.Item().PaddingTop(10).BorderTop(1).PaddingTop(6)
                        .Text($"Net Pay: {request.CurrencyCode} {request.NetPay:N2}").FontSize(14).Bold();
                });

                page.Footer().AlignCenter()
                    .Text("This is a system-generated payslip and does not require a signature.")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });

        return document.GeneratePdf();
    }
}
