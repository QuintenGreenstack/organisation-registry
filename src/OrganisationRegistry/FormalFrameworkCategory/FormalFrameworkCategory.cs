﻿namespace OrganisationRegistry.FormalFrameworkCategory
{
    using Events;
    using Infrastructure.Domain;

    public class FormalFrameworkCategory: AggregateRoot
    {
        public string Name { get; private set; }

        public FormalFrameworkCategory() { }

        public FormalFrameworkCategory(FormalFrameworkCategoryId id, string name)
        {
            ApplyChange(new FormalFrameworkCategoryCreated(id, name));
        }

        public void Update(string name)
        {
            ApplyChange(new FormalFrameworkCategoryUpdated(Id, name, Name));
        }

        private void Apply(FormalFrameworkCategoryCreated @event)
        {
            Id = @event.FormalFrameworkCategoryId;
            Name = @event.Name;
        }

        private void Apply(FormalFrameworkCategoryUpdated @event)
        {
            Name = @event.Name;
        }
    }
}
