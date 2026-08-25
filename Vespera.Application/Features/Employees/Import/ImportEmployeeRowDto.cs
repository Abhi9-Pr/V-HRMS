namespace Vespera.Application.Features.Employees.Import;

/// <summary>
/// One row of a bulk employee import. Department/designation/location are referenced by their
/// human-readable identifiers (department <c>Code</c>, designation <c>Title</c>, location
/// <c>Name</c>) rather than GUIDs, since a person filling in a CSV by hand knows "Engineering"
/// or "ENG", not a database id — <c>Designation</c>/<c>Location</c> have no separate code field
/// (see <c>Vespera.Domain.Eis.Designation</c>/<c>Location</c>), so their own display name is the
/// natural lookup key.
/// </summary>
public sealed record ImportEmployeeRowDto(
    int RowNumber,
    string Code,
    string FirstName,
    string LastName,
    string WorkEmail,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly DateOfJoining,
    string DepartmentCode,
    string DesignationTitle,
    string LocationName);
