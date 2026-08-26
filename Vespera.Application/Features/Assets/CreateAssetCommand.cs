using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed record CreateAssetCommand(
    string AssetTag,
    string Category,
    decimal PurchaseCost,
    Currency PurchaseCostCurrency,
    DateOnly PurchaseDate,
    string? SerialNumber,
    string? MacAddress,
    DateOnly? WarrantyExpiryDate,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
