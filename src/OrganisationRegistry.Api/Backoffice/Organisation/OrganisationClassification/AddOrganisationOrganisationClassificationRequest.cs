﻿namespace OrganisationRegistry.Api.Backoffice.Organisation.OrganisationClassification;

using System;
using FluentValidation;
using OrganisationClassificationType;
using OrganisationRegistry.Organisation;
using OrganisationRegistry.OrganisationClassification;

public class AddOrganisationOrganisationClassificationInternalRequest
{
    public Guid OrganisationId { get; set; }
    public AddOrganisationOrganisationClassificationRequest Body { get; }

    public AddOrganisationOrganisationClassificationInternalRequest(Guid organisationId, AddOrganisationOrganisationClassificationRequest message)
    {
        OrganisationId = organisationId;
        Body = message;
    }
}

public class AddOrganisationOrganisationClassificationRequest
{
    public Guid OrganisationOrganisationClassificationId { get; set; }
    public Guid OrganisationClassificationTypeId { get; set; }
    public Guid OrganisationClassificationId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class AddOrganisationOrganisationClassificationInternalRequestValidator : AbstractValidator<AddOrganisationOrganisationClassificationInternalRequest>
{
    public AddOrganisationOrganisationClassificationInternalRequestValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty()
            .WithMessage("Id is required.");

        RuleFor(x => x.Body.OrganisationClassificationTypeId)
            .NotEmpty()
            .WithMessage("Organisation Classification Type Id is required.");

        RuleFor(x => x.Body.OrganisationClassificationId)
            .NotEmpty()
            .WithMessage("Organisation Classification Id is required.");

        // TODO: Validate if OrganisationClassificationTypeId is valid

        RuleFor(x => x.Body.ValidTo)
            .GreaterThanOrEqualTo(x => x.Body.ValidFrom)
            .When(x => x.Body.ValidFrom.HasValue)
            .WithMessage("Valid To must be greater than or equal to Valid From.");

        RuleFor(x => x.OrganisationId)
            .NotEmpty()
            .WithMessage("Organisation Id is required.");

        // TODO: Validate if org id is valid
    }
}

public static class AddOrganisationOrganisationClassificationRequestMapping
{
    public static AddOrganisationOrganisationClassification Map(AddOrganisationOrganisationClassificationInternalRequest message)
        => new(
            message.Body.OrganisationOrganisationClassificationId,
            new OrganisationId(message.OrganisationId),
            new OrganisationClassificationTypeId(message.Body.OrganisationClassificationTypeId),
            new OrganisationClassificationId(message.Body.OrganisationClassificationId),
            new ValidFrom(message.Body.ValidFrom),
            new ValidTo(message.Body.ValidTo));
}
