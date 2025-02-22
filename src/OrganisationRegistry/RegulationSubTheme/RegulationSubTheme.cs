namespace OrganisationRegistry.RegulationSubTheme
{
    using Events;
    using Infrastructure.Domain;
    using RegulationTheme;

    public class RegulationSubTheme: AggregateRoot
    {
        public RegulationSubThemeName Name { get; private set; }
        public RegulationThemeId RegulationThemeId { get; private set; }

        private RegulationThemeName _regulationThemeName;

        public RegulationSubTheme() { }

        public RegulationSubTheme(
            RegulationSubThemeId id,
            RegulationSubThemeName name,
            RegulationTheme regulationTheme)
        {
            ApplyChange(new RegulationSubThemeCreated(
                id,
                name,
                regulationTheme.Id,
                regulationTheme.Name));
        }

        public void Update(
            RegulationSubThemeName name,
            RegulationTheme regulationTheme)
        {
            ApplyChange(new RegulationSubThemeUpdated(
                Id,
                name, regulationTheme.Id, regulationTheme.Name,
                Name, RegulationThemeId, _regulationThemeName));
        }

        private void Apply(RegulationSubThemeCreated @event)
        {
            Id = @event.RegulationSubThemeId;
            Name = new RegulationSubThemeName(@event.Name);
            RegulationThemeId = new RegulationThemeId(@event.RegulationThemeId);
        }

        private void Apply(RegulationSubThemeUpdated @event)
        {
            Name = new RegulationSubThemeName(@event.Name);
            RegulationThemeId = new RegulationThemeId(@event.RegulationThemeId);
            _regulationThemeName = new RegulationThemeName(@event.RegulationThemeName);
        }
    }
}
