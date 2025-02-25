using FluentValidation;

namespace espasyo.Application.Incidents.Queries.GetClusters;

public class GetClustersQueryValidator : AbstractValidator<GetClustersQuery>
{
    public GetClustersQueryValidator()
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


    }

   
}