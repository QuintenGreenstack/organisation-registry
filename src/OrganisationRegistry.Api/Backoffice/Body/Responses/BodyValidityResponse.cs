namespace OrganisationRegistry.Api.Backoffice.Body.Responses
{
    using System;
    using SqlServer.Body;

    public class BodyValidityResponse
    {
        public Guid Id { get; }

        public DateTime? FormalValidFrom { get; }
        public DateTime? FormalValidTo { get; }

        public BodyValidityResponse(BodyDetail projectionItem)
        {
            Id = projectionItem.Id;

            FormalValidFrom = projectionItem.FormalValidFrom;
            FormalValidTo = projectionItem.FormalValidTo;
        }
    }
}
