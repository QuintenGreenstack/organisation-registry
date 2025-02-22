namespace OrganisationRegistry.Api.Backoffice.Admin.Events.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Be.Vlaanderen.Basisregisters.Api.Search.Helpers;
    using Be.Vlaanderen.Basisregisters.Converters.Timestamp;
    using Infrastructure.Search;
    using Infrastructure.Search.Filtering;
    using Infrastructure.Search.Sorting;
    using Newtonsoft.Json;
    using OrganisationRegistry.Infrastructure.Events;
    using OrganisationRegistry.Infrastructure.EventStore;
    using SqlServer.Event;
    using SqlServer.Infrastructure;

    public class EventWithData
    {
        public Guid Id { get; }
        public int Number { get; }
        public int Version { get; }
        public string Name { get; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTime Timestamp { get; }

        public IEvent? Data { get; }
        public string Ip { get; }
        public string LastName { get; }
        public string FirstName { get; }
        public string UserId { get; }

        public EventWithData(
            Guid id,
            int number,
            int version,
            string name,
            DateTime timestamp,
            string data,
            string ip,
            string lastName,
            string firstName,
            string userId)
        {
            var eventType = name.ToEventType();
            var eventData = (IEvent?)JsonConvert.DeserializeObject(data, eventType);

            Id = id;
            Number = number;
            Version = version;
            Name = eventType.Name;
            Timestamp = timestamp;
            Data = eventData;
            Ip = ip;
            LastName = lastName;
            FirstName = firstName;
            UserId = userId;
        }

        public EventWithData(EventListItem x) : this(
            x.Id,
            x.Number,
            x.Version,
            x.Name,
            x.Timestamp,
            x.Data,
            x.Ip ?? "",
            x.LastName ?? "",
            x.FirstName ?? "",
            x.UserId ?? "")
        {
        }
    }

    public class EventListQuery: Query<EventListItem, EventListItemFilter, EventWithData>
    {
        private readonly OrganisationRegistryContext _context;

        protected override ISorting Sorting => new EventListSorting();

        protected override Expression<Func<EventListItem, EventWithData>> Transformation =>
            x => new EventWithData(
                x.Id,
                x.Number,
                x.Version,
                x.Name,
                x.Timestamp,
                x.Data,
                x.Ip ?? "",
                x.LastName ?? "",
                x.FirstName ?? "",
                x.UserId ?? "");

        public EventListQuery(OrganisationRegistryContext context)
        {
            _context = context;
        }

        protected override IQueryable<EventListItem> Filter(FilteringHeader<EventListItemFilter> filtering)
        {
            var events = _context.Events.AsQueryable();

            if (filtering.Filter is not { } filter)
                return events;

            if (filter.EventNumber is > 0)
                events = events.Where(x => x.Number == filter.EventNumber.Value);

            if (filter.AggregateId.HasValue)
                events = events.Where(x => x.Id == filter.AggregateId);

            if (!filter.Name.IsNullOrWhiteSpace())
                events = events.Where(x => x.Name.Contains(filter.Name));

            if (!filter.FirstName.IsNullOrWhiteSpace())
                events = events.Where(x => x.FirstName != null && x.FirstName.Contains(filter.FirstName));

            if (!filter.LastName.IsNullOrWhiteSpace())
                events = events.Where(x => x.LastName != null && x.LastName.Contains(filter.LastName));

            if (!filter.Data.IsNullOrWhiteSpace())
                events = events.Where(x => x.Data.Contains(filter.Data));

            if (!filter.Ip.IsNullOrWhiteSpace())
                events = events.Where(x => x.Ip != null && x.Ip.Contains(filter.Ip));

            return events;
        }

        private class EventListSorting : ISorting
        {
            public IEnumerable<string> SortableFields { get; } = new[]
            {
                nameof(EventListItem.Number),
                nameof(EventListItem.Name),
                nameof(EventListItem.Version),
                nameof(EventListItem.Timestamp),
                nameof(EventListItem.FirstName),
                nameof(EventListItem.LastName),
            };

            public SortingHeader DefaultSortingHeader { get; } =
                new SortingHeader(nameof(EventListItem.Number), SortOrder.Descending);
        }
    }

    public class EventListItemFilter
    {
        public Guid? AggregateId { get; set; }
        public int? EventNumber { get; set; }
        public string Name { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Data { get; set; } = null!;
        public string Ip { get; set; } = null!;
    }
}
