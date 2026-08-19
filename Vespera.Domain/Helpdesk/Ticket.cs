using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk.Events;

namespace Vespera.Domain.Helpdesk;

public readonly record struct TicketCommentId(Guid Value)
{
    public static TicketCommentId New() => new(Guid.NewGuid());
}

public sealed class TicketComment : Entity<TicketCommentId>
{
    internal TicketComment(TicketCommentId id, EmployeeId authorId, string body, bool isInternal, DateTimeOffset createdAt)
        : base(id)
    {
        AuthorId = authorId;
        Body = body;
        IsInternal = isInternal;
        CreatedAt = createdAt;
    }

    public EmployeeId AuthorId { get; }

    public string Body { get; }

    public bool IsInternal { get; }

    public DateTimeOffset CreatedAt { get; }
}

public readonly record struct TicketId(Guid Value)
{
    public static TicketId New() => new(Guid.NewGuid());
}

public enum TicketStatus
{
    Open,
    InProgress,
    OnHold,
    Resolved,
    Closed,
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical,
}

public sealed class Ticket : AggregateRoot<TicketId>, ITenantScoped
{
    private readonly List<TicketComment> _comments = [];

    private Ticket(
        TicketId id, TenantId tenantId, EmployeeId raisedBy, TicketCategoryId categoryId, SlaPolicyId slaPolicyId,
        string subject, string description, TicketPriority priority, DateTimeOffset raisedAt, DateTimeOffset dueAt)
        : base(id)
    {
        TenantId = tenantId;
        RaisedBy = raisedBy;
        CategoryId = categoryId;
        SlaPolicyId = slaPolicyId;
        Subject = subject;
        Description = description;
        Priority = priority;
        RaisedAt = raisedAt;
        DueAt = dueAt;
        Status = TicketStatus.Open;
    }

    public TenantId TenantId { get; }

    public EmployeeId RaisedBy { get; }

    public TicketCategoryId CategoryId { get; }

    public SlaPolicyId SlaPolicyId { get; }

    public string Subject { get; }

    public string Description { get; }

    public TicketPriority Priority { get; }

    public TicketStatus Status { get; private set; }

    public DateTimeOffset RaisedAt { get; }

    public DateTimeOffset DueAt { get; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public EmployeeId? AssignedTo { get; private set; }

    public IReadOnlyCollection<TicketComment> Comments => _comments.AsReadOnly();

    public static Result<Ticket> Raise(
        TenantId tenantId, EmployeeId raisedBy, TicketCategoryId categoryId, SlaPolicyId slaPolicyId, string subject,
        string description, TicketPriority priority, DateTimeOffset raisedAt, TimeSpan resolutionTime)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<Ticket>(Error.Validation("ticket.subject_required", "Subject is required."));
        }

        return Result.Success(new Ticket(
            TicketId.New(), tenantId, raisedBy, categoryId, slaPolicyId, subject.Trim(), description, priority,
            raisedAt, raisedAt + resolutionTime));
    }

    public Result AddComment(EmployeeId authorId, string body, bool isInternal, DateTimeOffset occurredOn)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure(Error.Validation("ticket.comment_required", "Comment body is required."));
        }

        _comments.Add(new TicketComment(TicketCommentId.New(), authorId, body.Trim(), isInternal, occurredOn));
        return Result.Success();
    }

    public Result AssignTo(EmployeeId employeeId)
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return Result.Failure(Error.Conflict("ticket.closed", "Cannot assign a resolved or closed ticket."));
        }

        AssignedTo = employeeId;
        Status = TicketStatus.InProgress;
        return Result.Success();
    }

    public Result Resolve(DateTimeOffset occurredOn)
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return Result.Failure(Error.Conflict("ticket.already_resolved", "Ticket is already resolved or closed."));
        }

        Status = TicketStatus.Resolved;
        ResolvedAt = occurredOn;
        return Result.Success();
    }

    public Result Close()
    {
        if (Status != TicketStatus.Resolved)
        {
            return Result.Failure(Error.Conflict("ticket.not_resolved", "Only a resolved ticket can be closed."));
        }

        Status = TicketStatus.Closed;
        return Result.Success();
    }

    public Result CheckSlaBreach(DateTimeOffset now)
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return Result.Success();
        }

        if (now <= DueAt)
        {
            return Result.Success();
        }

        Raise(new TicketSlaBreached(Id, DueAt, now));
        return Result.Success();
    }
}
