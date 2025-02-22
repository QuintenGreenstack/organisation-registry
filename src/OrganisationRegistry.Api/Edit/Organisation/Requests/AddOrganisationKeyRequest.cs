﻿namespace OrganisationRegistry.Api.Edit.Organisation.Requests
{
    using System;
    using FluentValidation;
    using KeyTypes;
    using OrganisationRegistry.Organisation;
    using OrganisationRegistry.Organisation.Commands;
    using SqlServer.Organisation;
    using Swashbuckle.AspNetCore.Annotations;

    public class AddOrganisationKeyInternalRequest
    {
        public Guid OrganisationId { get; set; }
        public AddOrganisationKeyRequest Body { get; }

        public AddOrganisationKeyInternalRequest(Guid organisationId, AddOrganisationKeyRequest message)
        {
            OrganisationId = organisationId;
            Body = message;
            Body.OrganisationKeyId ??= Guid.NewGuid();
        }
    }

    public class AddOrganisationKeyRequest
    {
        /// <summary>
        /// Unieke id van de organisatie sleutel. Wordt gegenereerd indien niet meegegeven.
        /// </summary>
        public Guid? OrganisationKeyId { get; set; }
        /// <summary>
        /// Id van het sleuteltype.
        /// </summary>
        public Guid KeyTypeId { get; set; }
        /// <summary>
        /// Waarde van de sleutel.
        /// </summary>
        public string KeyValue { get; set; }
        /// <summary>
        /// Geldig vanaf.
        /// </summary>
        [SwaggerSchema(Format = "date")]
        public DateTime? ValidFrom { get; set; }
        /// <summary>
        /// Geldig tot.
        /// </summary>
        [SwaggerSchema(Format = "date")]
        public DateTime? ValidTo { get; set; }
    }

    public class AddOrganisationKeyInternalRequestValidator : AbstractValidator<AddOrganisationKeyInternalRequest>
    {
        public AddOrganisationKeyInternalRequestValidator()
        {
            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Id is required.");

            RuleFor(x => x.Body.KeyTypeId)
                .NotEmpty()
                .WithMessage("Key Type Id is required.");

            RuleFor(x => x.Body.KeyValue)
                .NotEmpty()
                .WithMessage("Key Value is required.");

            RuleFor(x => x.Body.KeyValue)
                .Length(0, OrganisationKeyListConfiguration.KeyValueLength)
                .WithMessage($"Key Value cannot be longer than {OrganisationKeyListConfiguration.KeyValueLength}.");

            RuleFor(x => x.Body.ValidTo)
                .GreaterThanOrEqualTo(x => x.Body.ValidFrom)
                .When(x => x.Body.ValidFrom.HasValue)
                .WithMessage("Valid To must be greater than or equal to Valid From.");

            RuleFor(x => x.OrganisationId)
                .NotEmpty()
                .WithMessage("Organisation Id is required.");
        }
    }

    public static class AddOrganisationKeyRequestMapping
    {
        public static AddOrganisationKey Map(AddOrganisationKeyInternalRequest message)
        {
            return new AddOrganisationKey(
                message.Body.OrganisationKeyId!.Value,
                new OrganisationId(message.OrganisationId),
                new KeyTypeId(message.Body.KeyTypeId),
                message.Body.KeyValue,
                new ValidFrom(message.Body.ValidFrom),
                new ValidTo(message.Body.ValidTo));
        }
    }
}
