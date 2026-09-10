

namespace Consumer.Models;

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; set; }

    private ValidationResult(
        bool isValid,
        string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success()
    {
        return new ValidationResult(
            true,
            null);
    }

    public static ValidationResult Failure(
        string errorMessage)
    {
        return new ValidationResult(
            false,
            errorMessage);
    }
}
