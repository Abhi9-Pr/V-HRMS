namespace Vespera.Application.Abstractions.Services;

public interface IPiiProtector
{
    public string Protect(string plainText);

    public string Unprotect(string protectedText);
}
