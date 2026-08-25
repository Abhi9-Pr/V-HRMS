using Mapster;
using Vespera.Domain.Attendance;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class RotationPatternMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RotationPattern, RotationPatternDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Days, src => src.Days
                .Select(day => new RotationPatternDayDto(day.SequenceNumber, day.ShiftId != null ? day.ShiftId.Value.Value : (Guid?)null))
                .ToList());

        config.NewConfig<RotationPattern, RotationPatternSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
