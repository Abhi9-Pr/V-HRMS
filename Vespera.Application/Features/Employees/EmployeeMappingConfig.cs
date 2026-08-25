using Mapster;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class EmployeeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Employee, EmployeeSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Code, src => src.Code.Value)
            .Map(dest => dest.FullName, src => src.FirstName + " " + src.LastName)
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
