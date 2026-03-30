using FluentValidation;

namespace Profily.Api.Contracts;

public sealed class CreateExperienceRequest
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class UpdateExperienceRequest
{
    public string? Title { get; set; }
    public string? Company { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool? IsCurrent { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}

public sealed class CreateExperienceRequestValidator : AbstractValidator<CreateExperienceRequest>
{
    public CreateExperienceRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty()
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date must not be in the future.");
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateExperienceRequestValidator : AbstractValidator<UpdateExperienceRequest>
{
    public UpdateExperienceRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200).When(x => x.Company is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}