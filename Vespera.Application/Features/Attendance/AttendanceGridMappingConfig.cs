using Mapster;
using Vespera.Domain.Attendance;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

public sealed class AttendanceGridMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Employee, AttendanceGridEmployeeProjection>()
            .Map(dest => dest.EmployeeId, src => src.Id.Value)
            .Map(dest => dest.EmployeeCode, src => src.Code.Value);

        config.NewConfig<AttendanceDay, AttendanceGridCellProjection>()
            .Map(dest => dest.EmployeeId, src => src.EmployeeId.Value)
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
