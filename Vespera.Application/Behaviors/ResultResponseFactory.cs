using System.Reflection;
using Vespera.Domain.Common;

namespace Vespera.Application.Behaviors;

/// <summary>
/// Builds a failure TResponse for behaviors constrained to `where TResponse : Result`, without
/// knowing at compile time whether TResponse is Result or Result&lt;TValue&gt;.
/// </summary>
internal static class ResultResponseFactory
{
    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition);

    public static TResponse Create<TResponse>(Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failure = GenericFailureMethod.MakeGenericMethod(valueType).Invoke(null, [error]);
        return (TResponse)failure!;
    }
}
