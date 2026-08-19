namespace Vespera.Domain.Common;

/// <summary>
/// Marks a plain-typed property as carrying personally identifiable information, for entities
/// whose PII isn't already expressed via an <c>IPersonalData</c>-typed value object. Auditing
/// treats a property as PII, and redacts it to a one-way hash instead of writing it in clear, if
/// either this attribute is present or the property's declared type implements
/// <c>Vespera.Domain.ValueObjects.IPersonalData</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PiiAttribute : Attribute
{
}
