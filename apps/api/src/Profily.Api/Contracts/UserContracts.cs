using FluentValidation;

namespace Profily.Api.Contracts;

public sealed class UpdateUserRequest
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.DisplayName).MaximumLength(200).When(x => x.DisplayName is not null);
        RuleFor(x => x.Bio).MaximumLength(2000).When(x => x.Bio is not null);
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
        RuleFor(x => x.Company).MaximumLength(200).When(x => x.Company is not null);
        RuleFor(x => x.Email).MaximumLength(300).EmailAddress().When(x => x.Email is not null);
    }
}