using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct DesignationId(Guid Value)
{
    public static DesignationId New() => new(Guid.NewGuid());
}

public sealed class Designation : AuditableTenantAggregateRoot<DesignationId>
{
    private Designation(DesignationId id, TenantId tenantId, string title, int grade, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Title = title;
        Grade = grade;
    }

    public string Title { get; private set; }

    public int Grade { get; private set; }

    public static Result<Designation> Create(TenantId tenantId, string title, int grade, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Designation>(Error.Validation("designation.title_required", "Designation title is required."));
        }

        if (grade <= 0)
        {
            return Result.Failure<Designation>(Error.Validation("designation.invalid_grade", "Grade must be positive."));
        }

        return Result.Success(new Designation(DesignationId.New(), tenantId, title.Trim(), grade, occurredOn, createdBy));
    }

    public Result Rename(string title, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(Error.Validation("designation.title_required", "Designation title is required."));
        }

        Title = title.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result ChangeGrade(int grade, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (grade <= 0)
        {
            return Result.Failure(Error.Validation("designation.invalid_grade", "Grade must be positive."));
        }

        Grade = grade;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
