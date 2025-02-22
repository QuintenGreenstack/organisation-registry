namespace OrganisationRegistry.ElasticSearch.Projections.Organisations
{
    using System.Data.Common;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using OrganisationRegistry.Organisation.Events;
    using OrganisationRegistry.Infrastructure.Events;
    using Building.Events;
    using ElasticSearch.Organisations;
    using Infrastructure;
    using Microsoft.Extensions.Logging;
    using Common;
    using Infrastructure.Change;

    public class OrganisationBuilding :
        BaseProjection<OrganisationBuilding>,
        IElasticEventHandler<OrganisationBuildingAdded>,
        IElasticEventHandler<OrganisationBuildingUpdated>,
        IElasticEventHandler<BuildingUpdated>,
        IElasticEventHandler<OrganisationTerminated>,
        IElasticEventHandler<OrganisationTerminatedV2>
    {
        public OrganisationBuilding(
            ILogger<OrganisationBuilding> logger) : base(logger)
        {
        }

        public async Task<IElasticChange> Handle(
            DbConnection dbConnection,
            DbTransaction dbTransaction,
            IEnvelope<BuildingUpdated> message)
        {
            return new ElasticMassChange
            (
                elastic => elastic.TryAsync(() => elastic
                    .MassUpdateOrganisationAsync(
                        x => x.Buildings.Single().BuildingId, message.Body.BuildingId,
                        "buildings", "buildingId",
                        "buildingName", message.Body.Name,
                        message.Number,
                        message.Timestamp))
            );
        }

        public async Task<IElasticChange> Handle(
            DbConnection dbConnection,
            DbTransaction dbTransaction,
            IEnvelope<OrganisationBuildingAdded> message)
        {
            return new ElasticPerDocumentChange<OrganisationDocument>
            (
                message.Body.OrganisationId,
                document =>
                {
                    document.ChangeId = message.Number;
                    document.ChangeTime = message.Timestamp;

                    if (document.Buildings == null)
                        document.Buildings = new List<OrganisationDocument.OrganisationBuilding>();

                    document.Buildings.RemoveExistingListItems(x =>
                        x.OrganisationBuildingId == message.Body.OrganisationBuildingId);

                    document.Buildings.Add(
                        new OrganisationDocument.OrganisationBuilding(
                            message.Body.OrganisationBuildingId,
                            message.Body.BuildingId,
                            message.Body.BuildingName,
                            message.Body.IsMainBuilding,
                            Period.FromDates(message.Body.ValidFrom, message.Body.ValidTo)));
                }
            );
        }

        public async Task<IElasticChange> Handle(
            DbConnection dbConnection,
            DbTransaction dbTransaction,
            IEnvelope<OrganisationBuildingUpdated> message)
        {
            return new ElasticPerDocumentChange<OrganisationDocument>
            (
                message.Body.OrganisationId,
                document =>
                {
                    document.ChangeId = message.Number;
                    document.ChangeTime = message.Timestamp;

                    document.Buildings.RemoveExistingListItems(x =>
                        x.OrganisationBuildingId == message.Body.OrganisationBuildingId);

                    document.Buildings.Add(
                        new OrganisationDocument.OrganisationBuilding(
                            message.Body.OrganisationBuildingId,
                            message.Body.BuildingId,
                            message.Body.BuildingName,
                            message.Body.IsMainBuilding,
                            Period.FromDates(message.Body.ValidFrom, message.Body.ValidTo)));
                }
            );
        }

        public async Task<IElasticChange> Handle(
            DbConnection dbConnection,
            DbTransaction dbTransaction,
            IEnvelope<OrganisationTerminated> message)
        {
            return new ElasticPerDocumentChange<OrganisationDocument>
            (
                message.Body.OrganisationId,
                document =>
                {
                    document.ChangeId = message.Number;
                    document.ChangeTime = message.Timestamp;

                    foreach (var (key, value) in message.Body.FieldsToTerminate.Buildings)
                    {
                        var organisationBuilding =
                            document
                                .Buildings
                                .Single(x => x.OrganisationBuildingId == key);

                        organisationBuilding.Validity.End = value;
                    }
                }
            );
        }

        public async Task<IElasticChange> Handle(
            DbConnection dbConnection,
            DbTransaction dbTransaction,
            IEnvelope<OrganisationTerminatedV2> message)
        {
            return new ElasticPerDocumentChange<OrganisationDocument>
            (
                message.Body.OrganisationId,
                document =>
                {
                    document.ChangeId = message.Number;
                    document.ChangeTime = message.Timestamp;

                    foreach (var (key, value) in message.Body.FieldsToTerminate.Buildings)
                    {
                        var organisationBuilding =
                            document
                                .Buildings
                                .Single(x => x.OrganisationBuildingId == key);

                        organisationBuilding.Validity.End = value;
                    }
                }
            );
        }
    }
}
