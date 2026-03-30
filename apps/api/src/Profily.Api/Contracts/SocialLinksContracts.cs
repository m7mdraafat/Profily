using FluentValidation;

namespace Profily.Api.Contracts;

public sealed class CreateSocialLinkRequest
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? IconFilename { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class UpdateSocialLinkRequest
{
    public string? Url { get; set; }
    public string? IconFilename { get; set; }
    public int? DisplayOrder { get; set; }
}

public sealed class CreateSocialLinkRequestValidator : AbstractValidator<CreateSocialLinkRequest>
{
    public CreateSocialLinkRequestValidator()
    {
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Must be a valid URL.");
        RuleFor(x => x.IconFilename).MaximumLength(100).When(x => x.IconFilename is not null);
    }
}

public sealed class UpdateSocialLinkRequestValidator : AbstractValidator<UpdateSocialLinkRequest>
{
    public UpdateSocialLinkRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => x.Url is not null)
            .WithMessage("Must be a valid URL.");
        RuleFor(x => x.IconFilename).MaximumLength(100).When(x => x.IconFilename is not null);
    }
}