using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed record ConfigureAssetDepreciationCommand(
    Guid AssetId,
    DepreciationMethod Method,
    int UsefulLifeMonths,
    decimal SalvageValue,
    Currency SalvageValueCurrency,
    string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
