using FluentValidation;

namespace Profily.Api.Extensions;

public static class ValidationExtensions
{
    public static async Task ValidateAndThrowExAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Core.Exceptions.ValidationException(errors);
        }
    }
}