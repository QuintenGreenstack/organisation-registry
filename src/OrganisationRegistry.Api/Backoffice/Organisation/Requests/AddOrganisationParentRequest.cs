﻿namespace OrganisationRegistry.Api.Backoffice.Organisation.Requests
{
    using System;
    using FluentValidation;
    using OrganisationRegistry.Organisation;
    using OrganisationRegistry.Organisation.Commands;

    public class AddOrganisationParentInternalRequest
    {
        public Guid OrganisationId { get; set; }
        public AddOrganisationParentRequest Body { get; }

        public AddOrganisationParentInternalRequest(Guid organisationId, AddOrganisationParentRequest message)
        {
            OrganisationId = organisationId;
            Body = message;
        }
    }

    public class AddOrganisationParentRequest
    {
        public Guid OrganisationOrganisationParentId { get; set; }
        public Guid ParentOrganisationId { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class AddOrganisationParentInternalRequestValidator : AbstractValidator<AddOrganisationParentInternalRequest>
    {
        public AddOrganisationParentInternalRequestValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Id is required.");

            RuleFor(x => x.Body.ParentOrganisationId)
                .NotEmpty()
                .WithMessage("Parent Organisation Id is required.");

            // TODO: Validate if FunctionTypeId is valid

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

    public static class AddOrganisationParentRequestMapping
    {
        public static AddOrganisationParent Map(AddOrganisationParentInternalRequest message)
        {
            return new AddOrganisationParent(
                message.Body.OrganisationOrganisationParentId,
                new OrganisationId(message.OrganisationId),
                new OrganisationId(message.Body.ParentOrganisationId),
                new ValidFrom(message.Body.ValidFrom),
                new ValidTo(message.Body.ValidTo));
        }
    }
}
