using FluentValidation;

namespace espasyo.Application.Incidents.Queries.GetGroupedClusters;

public class GetGroupedClustersQueryValidator : AbstractValidator<GetGroupedClustersQuery>
{
    public GetGroupedClustersQueryValidator()
    {
        RuleFor(x => x.DateFrom)
            .NotEmpty()
            .When(x => x.DateTo.HasValue)
            .WithMessage("DateFrom must be provided if DateTo is provided.");

        RuleFor(x => x.DateTo)
            .NotEmpty()
            .When(x => x.DateFrom.HasValue)
            .WithMessage("DateTo must be provided if DateFrom is provided.");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom must be less than or equal to DateTo.");

        RuleFor(x => x.Features)
            .NotEmpty()
            .WithMessage("Features must contain at least Latitude and Longitude.")
            .Must(x => x.Contains("Latitude") && x.Contains("Longitude"))
            .WithMessage("Features must contain both 'Latitude' and 'Longitude'.");

        RuleFor(x => x.Filters.CrimeTypes)
            .Empty()
            .When(x => x.Features is null || !x.Features.Contains("CrimeType"))
            .WithMessage("CrimeTypes filter must be empty when CrimeType is not in Features.");

        RuleFor(x => x.Filters.Severities)
            .Empty()
            .When(x => x.Features is null || !x.Features.Contains("Severity"))
            .WithMessage("Severities filter must be empty when Severity is not in Features.");
    }
}