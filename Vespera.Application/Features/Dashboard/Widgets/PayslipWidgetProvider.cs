using MediatR;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed class PayslipWidgetProvider : IDashboardWidgetProvider
{
    private readonly ISender _sender;

    public PayslipWidgetProvider(ISender sender)
    {
        _sender = sender;
    }

    public string WidgetKey => "payslip";

    public int DefaultOrder => 7;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Small;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyLatestPayslipQuery(), cancellationToken);
        return result.IsFailure ? Result.Failure<object?>(result.Error) : Result.Success<object?>(result.Value);
    }
}
