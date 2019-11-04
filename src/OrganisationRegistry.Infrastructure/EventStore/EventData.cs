namespace OrganisationRegistry.Infrastructure.EventStore
{
    using System;

    public class EventData
    {
        public Guid Id { get; private set; }
        public int Number { get; private set; }
        public int Version { get; private set; }
        public string Name { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string Data { get; private set; }
        public string Ip { get; private set; }
        public string LastName { get; private set; }
        public string FirstName { get; private set; }
        public string UserId { get; private set; }

        public EventData WithName(string name)
            => new EventData
            {
                Id = Id,
                Number = Number,
                Version = Version,
                Name = name,
                Timestamp = Timestamp,
                Data = Data,
                Ip = Ip,
                LastName = LastName,
                FirstName = FirstName,
                UserId = UserId
            };
    }
}
