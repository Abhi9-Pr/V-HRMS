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
    internal TicketComment(
        TicketCommentId id, EmployeeId authorId, string body, bool isInternal, DateTimeOffset createdAt,
        TicketCommentId? parentCommentId = null, IReadOnlyList<string>? attachmentReferences = null)
        : base(id)
    {
        AuthorId = authorId;
        Body = body;
        IsInternal = isInternal;
        CreatedAt = createdAt;
        ParentCommentId = parentCommentId;
        AttachmentReferences = attachmentReferences ?? [];
    }

    public EmployeeId AuthorId { get; }

    public string Body { get; }

    public bool IsInternal { get; }

    public DateTimeOffset CreatedAt { get; }

    /// <summary>Null for a top-level comment; otherwise the comment this one threads under.</summary>
    public TicketCommentId? ParentCommentId { get; }

    public IReadOnlyList<string> AttachmentReferences { get; }
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

    public bool SlaBreachNotified { get; private set; }

    public bool SlaWarningNotified { get; private set; }

    public int? SatisfactionRating { get; private set; }

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

    /// <summary>Same as the <see cref="TimeSpan"/> overload, but takes an already-computed due
    /// instant directly — used when the caller has run <see cref="Services.BusinessHoursCalculator"/>
    /// itself (Domain has zero external dependencies and can't read the holiday calendar).</summary>
    public static Result<Ticket> Raise(
        TenantId tenantId, EmployeeId raisedBy, TicketCategoryId categoryId, SlaPolicyId slaPolicyId, string subject,
        string description, TicketPriority priority, DateTimeOffset raisedAt, DateTimeOffset dueAt)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<Ticket>(Error.Validation("ticket.subject_required", "Subject is required."));
        }

        return Result.Success(new Ticket(
            TicketId.New(), tenantId, raisedBy, categoryId, slaPolicyId, subject.Trim(), description, priority, raisedAt, dueAt));
    }

    public Result AddComment(
        EmployeeId authorId, string body, bool isInternal, DateTimeOffset occurredOn,
        TicketCommentId? parentCommentId = null, IReadOnlyList<string>? attachmentReferences = null)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure(Error.Validation("ticket.comment_required", "Comment body is required."));
        }

        if (parentCommentId is { } parentId && !_comments.Any(comment => comment.Id == parentId))
        {
            return Result.Failure(Error.NotFound("ticket.parent_comment_not_found", "The comment being replied to was not found on this ticket."));
        }

        _comments.Add(new TicketComment(TicketCommentId.New(), authorId, body.Trim(), isInternal, occurredOn, parentCommentId, attachmentReferences));
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

        if (SlaBreachNotified)
        {
            return Result.Success();
        }

        if (now <= DueAt)
        {
            return Result.Success();
        }

        SlaBreachNotified = true;
        Raise(new TicketSlaBreached(Id, TenantId, DueAt, now));
        return Result.Success();
    }

    /// <summary>Raises an "approaching breach" warning once, at most, the first time <paramref name="now"/>
    /// crosses <paramref name="warningThresholdPercent"/> of the way through the ticket's
    /// raised-to-due window. A no-op once resolved/closed or once already warned/breached.</summary>
    public Result CheckSlaWarning(DateTimeOffset now, decimal warningThresholdPercent = 0.8m)
    {
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return Result.Success();
        }

        if (SlaWarningNotified)
        {
            return Result.Success();
        }

        var totalWindow = DueAt - RaisedAt;
        var warningAt = RaisedAt + (totalWindow * (double)warningThresholdPercent);

        if (now >= warningAt && now < DueAt)
        {
            SlaWarningNotified = true;
            Raise(new TicketSlaWarningRaised(Id, TenantId, DueAt, now));
        }

        return Result.Success();
    }

    public Result RateSatisfaction(int rating)
    {
        if (Status != TicketStatus.Closed)
        {
            return Result.Failure(Error.Conflict("ticket.not_closed", "Satisfaction can only be rated once the ticket is closed."));
        }

        if (rating is < 1 or > 5)
        {
            return Result.Failure(Error.Validation("ticket.invalid_satisfaction_rating", "Rating must be between 1 and 5."));
        }

        SatisfactionRating = rating;
        return Result.Success();
    }
}
