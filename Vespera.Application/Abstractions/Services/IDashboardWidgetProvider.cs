using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Abstractions.Services;

/// <summary>
/// One dashboard widget. Adding a widget to the landing dashboard means writing one class that
/// implements this interface and adding one <c>AddScoped&lt;IDashboardWidgetProvider, X&gt;()</c>
/// line to <c>ApplicationServiceCollectionExtensions</c> — see
/// <c>DashboardWidgetProviderRegistrationTests</c> in Vespera.Architecture.Tests for the mechanical
/// proof, the same OCP idiom as <c>IPayrollComponentRule</c>/<c>IExpensePolicyRule</c>.
///
/// A provider does its own composition (typically <c>ISender.Send</c> against an existing
/// query) and returns a plain payload object that gets JSON-serialized as-is — there is no shared
/// widget DTO base type, because each widget's shape is genuinely different. Failures are
/// communicated through <see cref="Result{TValue}"/>, never a thrown exception; the aggregator
/// additionally guards every call in try/catch so a provider bug can't blank the rest of the
/// dashboard (see <c>GetDashboardQueryHandler</c>).
/// </summary>
public interface IDashboardWidgetProvider
{
    /// <summary>Stable identifier persisted in <see cref="WidgetPreference.WidgetKey"/> and used
    /// as the Angular widget-registry lookup key. Never rename once shipped — existing users'
    /// saved layouts reference it by string.</summary>
    public string WidgetKey { get; }

    public int DefaultOrder { get; }

    public bool DefaultVisible { get; }

    public WidgetSize DefaultSize { get; }

    public Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken);
}
