namespace OrganisationRegistry.ElasticSearch.Projections.Organisations
{
    using System;
    using System.Data.Common;
    using System.Collections.Generic;
    using System.Linq;
    using Client;
    using ElasticSearch.Organisations;
    using OrganisationRegistry.Organisation.Events;
    using OrganisationRegistry.Infrastructure.Events;
    using Infrastructure;
    using Microsoft.Extensions.Logging;
    using Common;

    public class OrganisationParent :
        BaseProjection<OrganisationParent>,
        IEventHandler<OrganisationParentAdded>,
        IEventHandler<OrganisationParentUpdated>,
        IEventHandler<OrganisationInfoUpdated>,
        IEventHandler<OrganisationInfoUpdatedFromKbo>
    {
        private readonly Elastic _elastic;

        public OrganisationParent(
            ILogger<OrganisationParent> logger,
            Elastic elastic) : base(logger)
        {
            _elastic = elastic;
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationInfoUpdated> message)
        {
            MassUpdateOrganisationParentName(message.Body.OrganisationId, message.Body.Name, message.Number, message.Timestamp);
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationInfoUpdatedFromKbo> message)
        {
            MassUpdateOrganisationParentName(message.Body.OrganisationId, message.Body.Name, message.Number, message.Timestamp);
        }

        private void MassUpdateOrganisationParentName(Guid organisationId, string name, int number, DateTimeOffset timestamp)
        {
            // Update all which use this type, and put the changeId on them too!
            _elastic.Try(() => _elastic.WriteClient
                .MassUpdateOrganisation(
                    x => x.Parents.Single().ParentOrganisationId, organisationId,
                    "parents", "parentOrganisationId",
                    "parentOrganisationName", name,
                    number,
                    timestamp));
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationParentAdded> message)
        {
            var organisationDocument = _elastic.TryGet(() => _elastic.WriteClient.Get<OrganisationDocument>(message.Body.OrganisationId).ThrowOnFailure().Source);

            organisationDocument.ChangeId = message.Number;
            organisationDocument.ChangeTime = message.Timestamp;

            if (organisationDocument.Parents == null)
                organisationDocument.Parents = new List<OrganisationDocument.OrganisationParent>();

            organisationDocument.Parents.RemoveExistingListItems(x => x.OrganisationOrganisationParentId == message.Body.OrganisationOrganisationParentId);

            organisationDocument.Parents.Add(
                new OrganisationDocument.OrganisationParent(
                    message.Body.OrganisationOrganisationParentId,
                    message.Body.ParentOrganisationId,
                    message.Body.ParentOrganisationName,
                    new Period(message.Body.ValidFrom, message.Body.ValidTo)));

            _elastic.Try(() => _elastic.WriteClient.IndexDocument(organisationDocument).ThrowOnFailure());
        }

        public void Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationParentUpdated> message)
        {
            var organisationDocument = _elastic.TryGet(() => _elastic.WriteClient.Get<OrganisationDocument>(message.Body.OrganisationId).ThrowOnFailure().Source);

            organisationDocument.ChangeId = message.Number;
            organisationDocument.ChangeTime = message.Timestamp;

            organisationDocument.Parents.RemoveExistingListItems(x => x.OrganisationOrganisationParentId == message.Body.OrganisationOrganisationParentId);

            organisationDocument.Parents.Add(
                new OrganisationDocument.OrganisationParent(
                    message.Body.OrganisationOrganisationParentId,
                    message.Body.ParentOrganisationId,
                    message.Body.ParentOrganisationName,
                    new Period(message.Body.ValidFrom, message.Body.ValidTo)));

            _elastic.Try(() => _elastic.WriteClient.IndexDocument(organisationDocument).ThrowOnFailure());
        }
    }
}
