﻿namespace OrganisationRegistry.Api.Backoffice.Body.Requests
{
    using System;
    using FluentValidation;
    using OrganisationRegistry.Body;
    using OrganisationRegistry.Body.Commands;
    using SqlServer.Body;

    public class UpdateBodyInfoInternalRequest
    {
        public Guid BodyId { get; set; }
        public UpdateBodyInfoRequest Body { get; set; }

        public UpdateBodyInfoInternalRequest(Guid bodyId, UpdateBodyInfoRequest body)
        {
            BodyId = bodyId;
            Body = body;
        }
    }

    public class UpdateBodyInfoRequest
    {
        public string Name { get; set; }

        public string ShortName { get; set; }

        public string Description { get; set; }
    }

    public class UpdateBodyInfoRequestValidator : AbstractValidator<UpdateBodyInfoInternalRequest>
    {
        public UpdateBodyInfoRequestValidator()
        {
            RuleFor(x => x.BodyId)
                .NotEmpty()
                .WithMessage("Id is required.");

            RuleFor(x => x.Body.Name)
                .NotEmpty()
                .WithMessage("Name is required.");

            RuleFor(x => x.Body.Name)
                .Length(0, BodyListConfiguration.NameLength)
                .WithMessage($"Name cannot be longer than {BodyListConfiguration.NameLength}.");
        }
    }

    public static class UpdateBodyInfoRequestMapping
    {
        public static UpdateBodyInfo Map(UpdateBodyInfoInternalRequest message)
        {
            return new UpdateBodyInfo(
                new BodyId(message.BodyId),
                message.Body.Name,
                message.Body.ShortName,
                message.Body.Description);
        }
    }
}
