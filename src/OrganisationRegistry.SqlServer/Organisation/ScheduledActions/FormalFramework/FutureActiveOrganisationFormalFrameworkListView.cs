﻿namespace OrganisationRegistry.SqlServer.Organisation.ScheduledActions.FormalFramework
{
    using System;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Microsoft.Extensions.Logging;
    using OrganisationRegistry.Infrastructure;
    using OrganisationRegistry.Infrastructure.Events;
    using OrganisationRegistry.Organisation.Events;
    using RebuildProjection = OrganisationRegistry.Infrastructure.Events.RebuildProjection;

    public class FutureActiveOrganisationFormalFrameworkListItem
    {
        public Guid OrganisationFormalFrameworkId { get; set; }

        public Guid OrganisationId { get; set; }

        public Guid FormalFrameworkId { get; set; }

        public DateTime? ValidFrom { get; set; }
    }

    public class FutureActiveOrganisationFormalFrameworkListConfiguration : EntityMappingConfiguration<FutureActiveOrganisationFormalFrameworkListItem>
    {
        public override void Map(EntityTypeBuilder<FutureActiveOrganisationFormalFrameworkListItem> b)
        {
            b.ToTable(nameof(FutureActiveOrganisationFormalFrameworkListView.ProjectionTables.FutureActiveOrganisationFormalFrameworkList), WellknownSchemas.BackofficeSchema)
                .HasKey(p => p.OrganisationFormalFrameworkId)
                .IsClustered(false);

            b.Property(p => p.OrganisationId).IsRequired();

            b.Property(p => p.FormalFrameworkId).IsRequired();

            b.Property(p => p.ValidFrom);

            b.HasIndex(x => x.ValidFrom);
        }
    }

    public class FutureActiveOrganisationFormalFrameworkListView :
        Projection<FutureActiveOrganisationFormalFrameworkListView>,
        IEventHandler<OrganisationFormalFrameworkAdded>,
        IEventHandler<OrganisationFormalFrameworkUpdated>,
        IEventHandler<FormalFrameworkAssignedToOrganisation>
    {
        private readonly IEventStore _eventStore;
        private readonly IDateTimeProvider _dateTimeProvider;
        public FutureActiveOrganisationFormalFrameworkListView(
            ILogger<FutureActiveOrganisationFormalFrameworkListView> logger,
            IEventStore eventStore,
            IDateTimeProvider dateTimeProvider,
            IContextFactory contextFactory) : base(logger, contextFactory)
        {
            _eventStore = eventStore;
            _dateTimeProvider = dateTimeProvider;

        }

        protected override string[] ProjectionTableNames => Enum.GetNames(typeof(ProjectionTables));
        public override string Schema => WellknownSchemas.BackofficeSchema;

        public enum ProjectionTables
        {
            FutureActiveOrganisationFormalFrameworkList
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationFormalFrameworkAdded> message)
        {
            var validFrom = new ValidFrom(message.Body.ValidFrom);
            if (validFrom.IsInPastOf(_dateTimeProvider.Today, true))
                return;

            using (var context = ContextFactory.CreateTransactional(dbConnection, dbTransaction))
                InsertFutureActiveOrganisationFormalFramework(context, message);
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationFormalFrameworkUpdated> message)
        {
            using (var context = ContextFactory.CreateTransactional(dbConnection, dbTransaction))
            {
                var validFrom = new ValidFrom(message.Body.ValidFrom);
                if (validFrom.IsInPastOf(_dateTimeProvider.Today, true))
                {
                    DeleteFutureActiveOrganisationFormalFramework(context, message.Body.OrganisationFormalFrameworkId);
                }
                else
                {
                    UpsertFutureActiveOrganisationFormalFramework(context, message);
                }
            }
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<FormalFrameworkAssignedToOrganisation> message)
        {
            using (var context = ContextFactory.CreateTransactional(dbConnection, dbTransaction))
                DeleteFutureActiveOrganisationFormalFramework(context, message.Body.OrganisationFormalFrameworkId);
        }

        public override async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<RebuildProjection> message)
        {
            await RebuildProjection(_eventStore, dbConnection, dbTransaction, message);
        }

        private static void InsertFutureActiveOrganisationFormalFramework(
            OrganisationRegistryContext context,
            IEnvelope<OrganisationFormalFrameworkAdded> message)
        {
            var futureActiveOrganisationFormalFrameworkListItem = new FutureActiveOrganisationFormalFrameworkListItem
            {
                OrganisationId = message.Body.OrganisationId,
                FormalFrameworkId = message.Body.FormalFrameworkId,
                OrganisationFormalFrameworkId = message.Body.OrganisationFormalFrameworkId,
                ValidFrom = message.Body.ValidFrom
            };

            context.FutureActiveOrganisationFormalFrameworkList.Add(futureActiveOrganisationFormalFrameworkListItem);
            context.SaveChanges();
        }

        private static void UpsertFutureActiveOrganisationFormalFramework(
            OrganisationRegistryContext context,
            IEnvelope<OrganisationFormalFrameworkUpdated> message)
        {
            var futureActiveOrganisationFormalFramework =
                context.FutureActiveOrganisationFormalFrameworkList.SingleOrDefault(
                    item => item.OrganisationFormalFrameworkId == message.Body.OrganisationFormalFrameworkId);

            if (futureActiveOrganisationFormalFramework == null)
            {
                var futureActiveOrganisationFormalFrameworkListItem =
                    new FutureActiveOrganisationFormalFrameworkListItem
                    {
                        OrganisationId = message.Body.OrganisationId,
                        FormalFrameworkId = message.Body.FormalFrameworkId,
                        OrganisationFormalFrameworkId = message.Body.OrganisationFormalFrameworkId,
                        ValidFrom = message.Body.ValidFrom
                    };

                context.FutureActiveOrganisationFormalFrameworkList.Add(futureActiveOrganisationFormalFrameworkListItem);
            }
            else
            {
                futureActiveOrganisationFormalFramework.OrganisationFormalFrameworkId = message.Body.OrganisationFormalFrameworkId;
                futureActiveOrganisationFormalFramework.OrganisationId = message.Body.OrganisationId;
                futureActiveOrganisationFormalFramework.FormalFrameworkId = message.Body.FormalFrameworkId;
                futureActiveOrganisationFormalFramework.ValidFrom = message.Body.ValidFrom;
            }

            context.SaveChanges();
        }

        private static void DeleteFutureActiveOrganisationFormalFramework(
            OrganisationRegistryContext context,
            Guid organisationFormalFrameworkId)
        {
            var futureActiveOrganisationFormalFramework =
                context.FutureActiveOrganisationFormalFrameworkList.SingleOrDefault(
                    item => item.OrganisationFormalFrameworkId == organisationFormalFrameworkId);

            if (futureActiveOrganisationFormalFramework == null)
                return;

            context.FutureActiveOrganisationFormalFrameworkList.Remove(futureActiveOrganisationFormalFramework);
            context.SaveChanges();
        }
    }
}
