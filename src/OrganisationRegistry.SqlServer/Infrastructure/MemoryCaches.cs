namespace OrganisationRegistry.SqlServer.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using OrganisationRegistry.Body.Events;
    using OrganisationRegistry.ContactType.Events;
    using OrganisationRegistry.Infrastructure.AppSpecific;
    using OrganisationRegistry.Infrastructure.Events;
    using OrganisationRegistry.Organisation.Events;

    public enum MemoryCacheType
    {
        OvoNumbers,
        OrganisationNames,
        OrganisationParents,
        OrganisationValidFroms,
        OrganisationValidTos,
        BodyNames,
        ContactTypeNames,
        BodySeatNames,
        BodySeatNumbers,
        IsSeatPaid,
        UnderVlimpersManagement,
    }

    // Scoped as SingleInstance()
    public class MemoryCaches : IMemoryCaches
    {
        private Dictionary<Guid, string> _ovoNumbers = null!;
        private Dictionary<Guid, string> _organisationNames = null!;
        private Dictionary<Guid, Guid?> _organisationParents = null!;
        private Dictionary<Guid, DateTime?> _organisationValidFroms = null!;
        private Dictionary<Guid, DateTime?> _organisationValidTos = null!;

        private Dictionary<Guid, string> _bodyNames = null!;
        private Dictionary<Guid, string> _bodySeatNames = null!;
        private Dictionary<Guid, string?> _bodySeatNumbers = null!;

        private Dictionary<Guid, string> _contactTypeNames = null!;

        private Dictionary<Guid, bool> _isSeatPaid = null!;

        private List<Guid> _orgsUnderVlimpersManagement = null!;

        private readonly ILogger<MemoryCaches> _logger;

        public IReadOnlyDictionary<Guid, string> OvoNumbers =>
            ToReadOnlyDictionary(GetCache<string>(MemoryCacheType.OvoNumbers));

        public IReadOnlyDictionary<Guid, string> OrganisationNames =>
            ToReadOnlyDictionary(GetCache<string>(MemoryCacheType.OrganisationNames));

        public IReadOnlyDictionary<Guid, Guid?> OrganisationParents =>
            ToReadOnlyDictionary(GetCache<Guid?>(MemoryCacheType.OrganisationParents));

        public IReadOnlyDictionary<Guid, DateTime?> OrganisationValidFroms =>
            ToReadOnlyDictionary(GetCache<DateTime?>(MemoryCacheType.OrganisationValidFroms));

        public IReadOnlyDictionary<Guid, DateTime?> OrganisationValidTos =>
            ToReadOnlyDictionary(GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos));

        public IReadOnlyDictionary<Guid, string> BodyNames =>
            ToReadOnlyDictionary(GetCache<string>(MemoryCacheType.BodyNames));

        public IReadOnlyDictionary<Guid, string> BodySeatNames =>
            ToReadOnlyDictionary(GetCache<string>(MemoryCacheType.BodySeatNames));

        public IReadOnlyDictionary<Guid, string> BodySeatNumbers =>
            ToReadOnlyDictionary(GetCache<string>(MemoryCacheType.BodySeatNumbers));

        public IReadOnlyDictionary<Guid, string> ContactTypeNames =>
            ToReadOnlyDictionary(GetCache<string>(MemoryCacheType.ContactTypeNames));

        public IReadOnlyDictionary<Guid, bool> IsSeatPaid =>
            ToReadOnlyDictionary(GetCache<bool>(MemoryCacheType.IsSeatPaid));

        public IList<Guid> UnderVlimpersManagement =>
            new List<Guid>(GetSparseCache<Guid>(MemoryCacheType.UnderVlimpersManagement));

        public MemoryCaches(IContextFactory contextFactory, ILogger<MemoryCaches>? logger = null)
        {
            _logger = logger ?? new NullLogger<MemoryCaches>();

            using (var organisationRegistryContext = contextFactory.Create())
            {
                foreach (MemoryCacheType memoryCacheType in Enum.GetValues(typeof(MemoryCacheType)))
                    ResetCache(memoryCacheType, organisationRegistryContext).GetAwaiter().GetResult();
            }
        }

        internal Dictionary<Guid, T> GetCache<T>(MemoryCacheType cacheType)
        {
            switch (cacheType)
            {
                case MemoryCacheType.OvoNumbers:
                    return _ovoNumbers as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.OrganisationNames:
                    return _organisationNames as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.OrganisationParents:
                    return _organisationParents as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.OrganisationValidFroms:
                    return _organisationValidFroms as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.OrganisationValidTos:
                    return _organisationValidTos as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.BodyNames:
                    return _bodyNames as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.BodySeatNames:
                    return _bodySeatNames as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.BodySeatNumbers:
                    return _bodySeatNumbers as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.ContactTypeNames:
                    return _contactTypeNames as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                case MemoryCacheType.IsSeatPaid:
                    return _isSeatPaid as Dictionary<Guid, T> ?? throw new InvalidOperationException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        internal IList<Guid> GetSparseCache<T>(MemoryCacheType cacheType)
        {
            switch (cacheType)
            {
                case MemoryCacheType.UnderVlimpersManagement:
                    return _orgsUnderVlimpersManagement ?? throw new InvalidOperationException();

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        internal async Task ResetCache(MemoryCacheType cacheType, OrganisationRegistryContext context)
        {
            _logger.LogInformation("Building memory cache for {CacheType}", cacheType);
            switch (cacheType)
            {
                case MemoryCacheType.OvoNumbers:
                    _ovoNumbers = await context.OrganisationDetail.AsNoTracking()
                        .Select(x => new {x.Id, x.OvoNumber})
                        .ToDictionaryAsync(x => x.Id, x => x.OvoNumber);
                    break;

                case MemoryCacheType.OrganisationNames:
                    _organisationNames = await context.OrganisationDetail.AsNoTracking()
                        .Select(x => new {x.Id, x.Name})
                        .ToDictionaryAsync(item => item.Id, item => item.Name);
                    break;

                case MemoryCacheType.OrganisationParents:
                    _organisationParents = await context.OrganisationDetail.AsNoTracking()
                        .Select(x => new {x.Id, x.ParentOrganisationId})
                        .ToDictionaryAsync(item => item.Id, item => item.ParentOrganisationId);
                    break;

                case MemoryCacheType.OrganisationValidFroms:
                    _organisationValidFroms = await context.OrganisationDetail.AsNoTracking()
                        .Select(x => new {x.Id, x.ValidFrom})
                        .ToDictionaryAsync(item => item.Id, item => item.ValidFrom);
                    break;

                case MemoryCacheType.OrganisationValidTos:
                    _organisationValidTos = await context.OrganisationDetail.AsNoTracking()
                        .Select(x => new {x.Id, x.ValidTo})
                        .ToDictionaryAsync(item => item.Id, item => item.ValidTo);
                    break;

                case MemoryCacheType.BodyNames:
                    _bodyNames = await context.BodyDetail.AsNoTracking()
                        .Select(x => new {x.Id, x.Name})
                        .ToDictionaryAsync(item => item.Id, item => item.Name);
                    break;

                case MemoryCacheType.BodySeatNames:
                    _bodySeatNames = await context.BodySeatList.AsNoTracking()
                        .Select(x => new {x.BodySeatId, x.Name})
                        .ToDictionaryAsync(item => item.BodySeatId, item => item.Name);
                    break;

                case MemoryCacheType.BodySeatNumbers:
                    _bodySeatNumbers = await context.BodySeatList.AsNoTracking()
                        .Select(x => new {x.BodySeatId, x.BodySeatNumber})
                        .ToDictionaryAsync(item => item.BodySeatId, item => item.BodySeatNumber);
                    break;

                case MemoryCacheType.ContactTypeNames:
                    _contactTypeNames = await context.ContactTypeList.AsNoTracking()
                        .Select(x => new {x.Id, x.Name})
                        .ToDictionaryAsync(item => item.Id, item => item.Name);
                    break;

                case MemoryCacheType.IsSeatPaid:
                    _isSeatPaid = await context.BodySeatList.AsNoTracking()
                        .Select(x => new {x.BodySeatId, x.PaidSeat})
                        .ToDictionaryAsync(item => item.BodySeatId, item => item.PaidSeat);
                    break;

                case MemoryCacheType.UnderVlimpersManagement:
                    _orgsUnderVlimpersManagement = await context.OrganisationDetail.AsNoTracking()
                        .Where(x => x.UnderVlimpersManagement)
                        .Select(x => x.Id)
                        .ToListAsync();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheType), cacheType, null);
            }
        }

        private static ReadOnlyDictionary<Guid, T> ToReadOnlyDictionary<T>(IDictionary<Guid, T> dictionary)
        {
            return new ReadOnlyDictionary<Guid, T>(dictionary);
        }
    }

    public interface IMemoryCachesMaintainer :
        IEventHandler<ContactTypeCreated>,
        IEventHandler<ContactTypeUpdated>,
        IEventHandler<OrganisationCreated>,
        IEventHandler<OrganisationCreatedFromKbo>,
        IEventHandler<OrganisationInfoUpdated>,
        IEventHandler<OrganisationNameUpdated>,
        IEventHandler<OrganisationValidityUpdated>,
        IEventHandler<OrganisationInfoUpdatedFromKbo>,
        IEventHandler<OrganisationCouplingWithKboCancelled>,
        IEventHandler<ParentAssignedToOrganisation>,
        IEventHandler<OrganisationParentUpdated>,
        IEventHandler<ParentClearedFromOrganisation>,
        IEventHandler<BodyRegistered>,
        IEventHandler<BodyInfoChanged>,
        IEventHandler<BodySeatAdded>,
        IEventHandler<BodySeatUpdated>,
        IEventHandler<ResetMemoryCache>,
        IEventHandler<OrganisationTerminated>,
        IEventHandler<OrganisationTerminatedV2>,
        IEventHandler<OrganisationPlacedUnderVlimpersManagement>,
        IEventHandler<OrganisationReleasedFromVlimpersManagement>
    {
    }

    public class MemoryCachesMaintainer : IMemoryCachesMaintainer
    {
        private readonly MemoryCaches _memoryCaches;
        private readonly IContextFactory _contextFactory;

        public MemoryCachesMaintainer(MemoryCaches memoryCaches, IContextFactory contextFactory)
        {
            _memoryCaches = memoryCaches;
            _contextFactory = contextFactory;
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<BodyRegistered> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.BodyNames)
                .UpdateMemoryCache(message.Body.BodyId, message.Body.Name);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<BodyInfoChanged> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.BodyNames)
                .UpdateMemoryCache(message.Body.BodyId, message.Body.Name);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<BodySeatAdded> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.BodySeatNames)
                .UpdateMemoryCache(message.Body.BodyId, message.Body.Name);

            _memoryCaches.GetCache<string>(MemoryCacheType.BodySeatNumbers)
                .UpdateMemoryCache(message.Body.BodyId, message.Body.BodySeatNumber);

            _memoryCaches.GetCache<bool>(MemoryCacheType.IsSeatPaid)
                .UpdateMemoryCache(message.Body.BodySeatId, message.Body.PaidSeat);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<BodySeatUpdated> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.BodySeatNames)
                .UpdateMemoryCache(message.Body.BodyId, message.Body.Name);

            _memoryCaches.GetCache<bool>(MemoryCacheType.IsSeatPaid)
                .UpdateMemoryCache(message.Body.BodySeatId, message.Body.PaidSeat);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<ContactTypeCreated> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.ContactTypeNames)
                .UpdateMemoryCache(message.Body.ContactTypeId, message.Body.Name);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<ContactTypeUpdated> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.ContactTypeNames)
                .UpdateMemoryCache(message.Body.ContactTypeId, message.Body.Name);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationCreated> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.OvoNumbers)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.OvoNumber);

            _memoryCaches.GetCache<string>(MemoryCacheType.OrganisationNames)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.Name);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidFroms)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidFrom);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidTo);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationCreatedFromKbo> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.OvoNumbers)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.OvoNumber);

            _memoryCaches.GetCache<string>(MemoryCacheType.OrganisationNames)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.Name);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidFroms)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidFrom);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidTo);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationInfoUpdated> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.OvoNumbers)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.OvoNumber);

            _memoryCaches.GetCache<string>(MemoryCacheType.OrganisationNames)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.Name);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidFroms)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidFrom);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidTo);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationNameUpdated> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.OrganisationNames)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.Name);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationValidityUpdated> message)
        {
            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidFroms)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidFrom);

            _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ValidTo);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationTerminated> message)
        {
            if (message.Body.FieldsToTerminate.OrganisationValidity.HasValue)
                _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos)
                    .UpdateMemoryCache(message.Body.OrganisationId, message.Body.FieldsToTerminate.OrganisationValidity);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationTerminatedV2> message)
        {
            if (message.Body.FieldsToTerminate.OrganisationValidity.HasValue)
                _memoryCaches.GetCache<DateTime?>(MemoryCacheType.OrganisationValidTos)
                    .UpdateMemoryCache(message.Body.OrganisationId, message.Body.FieldsToTerminate.OrganisationValidity);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationInfoUpdatedFromKbo> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.OrganisationNames)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.Name);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationCouplingWithKboCancelled> message)
        {
            _memoryCaches.GetCache<string>(MemoryCacheType.OrganisationNames)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.NameBeforeKboCoupling);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<ParentAssignedToOrganisation> message)
        {
            _memoryCaches.GetCache<Guid?>(MemoryCacheType.OrganisationParents)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ParentOrganisationId);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<OrganisationParentUpdated> message)
        {
            _memoryCaches.GetCache<Guid?>(MemoryCacheType.OrganisationParents)
                .UpdateMemoryCache(message.Body.OrganisationId, message.Body.ParentOrganisationId);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<ParentClearedFromOrganisation> message)
        {
            _memoryCaches.GetCache<Guid?>(MemoryCacheType.OrganisationParents)
                .UpdateMemoryCache(message.Body.OrganisationId, null);
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationPlacedUnderVlimpersManagement> message)
        {
            _memoryCaches.GetSparseCache<Guid>(MemoryCacheType.UnderVlimpersManagement)
                .Add(message.Body.OrganisationId);
        }

        public async Task Handle(DbConnection dbConnection, DbTransaction dbTransaction, IEnvelope<OrganisationReleasedFromVlimpersManagement> message)
        {
            _memoryCaches.GetSparseCache<Guid>(MemoryCacheType.UnderVlimpersManagement)
                .Remove(message.Body.OrganisationId);
        }

        public async Task Handle(DbConnection _, DbTransaction __, IEnvelope<ResetMemoryCache> message)
        {
            await CheckResetCache(message.Body.Events, new[] { typeof(ContactTypeCreated), typeof(ContactTypeUpdated) }, new[] { MemoryCacheType.ContactTypeNames });

            await CheckResetCache(message.Body.Events, new[] { typeof(BodyRegistered), typeof(BodyInfoChanged) }, new[] { MemoryCacheType.BodyNames });

            await CheckResetCache(message.Body.Events, new[] { typeof(BodySeatAdded), typeof(BodySeatUpdated) }, new[]
            {
                MemoryCacheType.BodySeatNames,
                MemoryCacheType.BodySeatNumbers,
                MemoryCacheType.IsSeatPaid
            });

            await CheckResetCache(
                message.Body.Events,
                new[]
                {
                    typeof(OrganisationCreated),
                    typeof(OrganisationCreatedFromKbo),
                    typeof(OrganisationInfoUpdated),
                    typeof(OrganisationNameUpdated),
                    typeof(OrganisationValidityUpdated),
                    typeof(OrganisationInfoUpdatedFromKbo)
                },
                new[]
                {
                    MemoryCacheType.OvoNumbers,
                    MemoryCacheType.OrganisationNames,
                    MemoryCacheType.OrganisationValidFroms,
                    MemoryCacheType.OrganisationValidTos
                });

            await CheckResetCache(
                message.Body.Events,
                new[]
                {
                    typeof(ParentAssignedToOrganisation),
                    typeof(OrganisationParentUpdated),
                    typeof(ParentClearedFromOrganisation)
                },
                new[] { MemoryCacheType.OrganisationParents });

            await CheckResetCache(
                message.Body.Events,
                new[]
                {
                    typeof(OrganisationPlacedUnderVlimpersManagement),
                    typeof(OrganisationReleasedFromVlimpersManagement),
                },
                new[] { MemoryCacheType.UnderVlimpersManagement });
        }

        private async Task CheckResetCache(IEnumerable<IEvent> events, Type[] eventTypes, IEnumerable<MemoryCacheType> memoryCacheTypes)
        {
            await using var context = _contextFactory.Create();
            if (events.Any(x => eventTypes.Contains(x.GetType())))
                foreach (var memoryCacheType in memoryCacheTypes)
                    await _memoryCaches.ResetCache(memoryCacheType, context);
        }
    }
}
