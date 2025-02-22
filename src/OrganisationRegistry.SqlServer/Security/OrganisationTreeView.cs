namespace OrganisationRegistry.SqlServer.Security
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Microsoft.Extensions.Logging;
    using Organisation;
    using OrganisationRegistry.Infrastructure;
    using OrganisationRegistry.Infrastructure.AppSpecific;
    using OrganisationRegistry.Infrastructure.Events;
    using OrganisationRegistry.Infrastructure.EventStore;
    using OrganisationRegistry.Organisation.Events;
    using OrganisationRegistry.Security;

    public class OrganisationTreeItem
    {
        public string OvoNumber { get; set; } = null!;

        public string? OrganisationTree { get; set; }
    }

    public class OrganisationTreeListConfiguration : EntityMappingConfiguration<OrganisationTreeItem>
    {
        public override void Map(EntityTypeBuilder<OrganisationTreeItem> b)
        {
            b.ToTable(nameof(OrganisationTreeView.ProjectionTables.OrganisationTreeList), WellknownSchemas.BackofficeSchema)
                .HasKey(p => p.OvoNumber);

            b.Property(p => p.OvoNumber).HasMaxLength(OrganisationListConfiguration.OvoNumberLength);

            b.Property(p => p.OrganisationTree);
        }
    }

    public class OrganisationTreeView :
        Projection<OrganisationTreeView>,
        IEventHandler<OrganisationCreated>,
        IEventHandler<OrganisationCreatedFromKbo>,
        IEventHandler<ParentAssignedToOrganisation>,
        IEventHandler<ParentClearedFromOrganisation>,
        IEventHandler<Rollback>
    {
        protected override string[] ProjectionTableNames => Enum.GetNames(typeof(ProjectionTables));
        public override string Schema => WellknownSchemas.BackofficeSchema;

        public enum ProjectionTables
        {
            OrganisationTreeList
        }

        private readonly IMemoryCaches _memoryCaches;
        private readonly IEventStore _eventStore;
        private readonly ICache<OrganisationSecurityInformation> _cache;
        private ITree<OvoNumber> _tree = null!;

        private class OvoNumber : INodeValue
        {
            public string Id { get; }

            public OvoNumber(string ovoNumber)
            {
                Id = ovoNumber;
            }
        }

        public OrganisationTreeView(
            ILogger<OrganisationTreeView> logger,
            IMemoryCaches memoryCaches,
            IEventStore eventStore,
            IContextFactory contextFactory,
            ICache<OrganisationSecurityInformation> cache) : base(logger, contextFactory)
        {
            _memoryCaches = memoryCaches;
            _eventStore = eventStore;
            _cache = cache;

            Initialise();
        }

        private void Initialise()
        {
            _tree = BuildInitialTree(_memoryCaches.OrganisationParents, _memoryCaches.OvoNumbers);
        }

        private static ITree<OvoNumber> BuildInitialTree(IReadOnlyDictionary<Guid, Guid?> organisationParents, IReadOnlyDictionary<Guid, string> organisationOvoNumbers)
        {
            var tree = new Tree<OvoNumber>();

            // Start by just adding all orgs
            foreach (var organisationParent in organisationParents)
                tree.AddNode(new OvoNumber(organisationOvoNumbers[organisationParent.Key]));

            // And then link up their parents
            foreach (var organisationParent in organisationParents.Where(x => x.Value.HasValue))
                tree.ChangeNodeParent(new OvoNumber(organisationOvoNumbers[organisationParent.Key]), new OvoNumber(organisationOvoNumbers[organisationParent.Value!.Value]));

            return tree;
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationCreated> message)
        {
            _tree.AddNode(new OvoNumber(message.Body.OvoNumber));
            var changes = _tree.GetChanges().ToList();
            _tree.AcceptChanges();

            await UpdateChanges(dbConnection, dbTransaction, ContextFactory, changes, _cache);
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationCreatedFromKbo> message)
        {
            _tree.AddNode(new OvoNumber(message.Body.OvoNumber));
            var changes = _tree.GetChanges().ToList();
            _tree.AcceptChanges();

            await UpdateChanges(dbConnection, dbTransaction, ContextFactory, changes, _cache);
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<ParentAssignedToOrganisation> message)
        {
            var ovoNumber = _memoryCaches.OvoNumbers[message.Body.OrganisationId];
            var parentOvoNumber = _memoryCaches.OvoNumbers[message.Body.ParentOrganisationId];

            _tree.ChangeNodeParent(new OvoNumber(ovoNumber), new OvoNumber(parentOvoNumber));
            var changes = _tree.GetChanges().ToList();
            _tree.AcceptChanges();

            await UpdateChanges(dbConnection, dbTransaction, ContextFactory, changes, _cache);
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<ParentClearedFromOrganisation> message)
        {
            var ovoNumber = _memoryCaches.OvoNumbers[message.Body.OrganisationId];

            _tree.RemoveNodeParent(new OvoNumber(ovoNumber));
            var changes = _tree.GetChanges().ToList();
            _tree.AcceptChanges();

            await UpdateChanges(dbConnection, dbTransaction, ContextFactory, changes, _cache);
        }

        private static async Task UpdateChanges(
            DbConnection dbConnection,
            DbTransaction dbTransaction,
            IContextFactory contextFactory,
            IEnumerable<INode<OvoNumber>> changes,
            ICache<OrganisationSecurityInformation> cache)
        {
            await using var context = contextFactory.CreateTransactional(dbConnection, dbTransaction);

            var organisationTreeList = await context.OrganisationTreeList.ToListAsync();

            foreach (var change in changes)
            {
                var treeItem = organisationTreeList.SingleOrDefault(x => x.OvoNumber == change.Id);
                if (treeItem != null)
                {
                    treeItem.OrganisationTree = change.Traverse().ToSeparatedList();
                }
                else
                {
                    context.Add(new OrganisationTreeItem
                    {
                        OvoNumber = change.Id,
                        OrganisationTree = change.Traverse().ToSeparatedList()
                    });
                }
            }

            await context.SaveChangesAsync();
            cache.ExpireAll();
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<Rollback> message)
        {
            // Something went wrong, check if any of the events actually matter to us
            var anyInterestingEvents = message.Body.Events.Any(x =>
                x is OrganisationCreated ||
                x is ParentAssignedToOrganisation ||
                x is ParentClearedFromOrganisation);

            if (anyInterestingEvents)
                Initialise();

            await Task.CompletedTask;
        }

        public override async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<RebuildProjection> message)
        {
            await RebuildProjection(_eventStore, dbConnection, dbTransaction, message, _ => _tree = new Tree<OvoNumber>());
        }
    }
}
