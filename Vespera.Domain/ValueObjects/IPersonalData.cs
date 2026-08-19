namespace Vespera.Domain.ValueObjects;

/// <summary>Marks a value object as carrying personally identifiable information.</summary>
public interface IPersonalData
{
    public string Masked();
}
