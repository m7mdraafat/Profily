using FluentValidation;

namespace Profily.Api.Contracts;

public sealed class CreateEducationRequest
{
    public string Degree { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class UpdateEducationRequest
{
    public string? Degree { get; set; }
    public string? School { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}

public sealed class CreateEducationRequestValidator : AbstractValidator<CreateEducationRequest>
{
    public CreateEducationRequestValidator()
    {
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(200);
        RuleFor(x => x.School).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateEducationRequestValidator : AbstractValidator<UpdateEducationRequest>
{
    public UpdateEducationRequestValidator()
    {
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.School).NotEmpty().MaximumLength(200).When(x => x.School is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}