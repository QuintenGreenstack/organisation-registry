namespace OrganisationRegistry.UnitTests.Organisation.UpdateOrganisationLocation
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Infrastructure.Tests.Extensions.TestHelpers;
    using Location;
    using OrganisationRegistry.Infrastructure.Events;
    using Location.Events;
    using Microsoft.Extensions.Logging;
    using Moq;
    using OrganisationRegistry.Infrastructure.Authorization;
    using Tests.Shared;
    using OrganisationRegistry.Organisation;
    using OrganisationRegistry.Organisation.Commands;
    using OrganisationRegistry.Organisation.Events;
    using OrganisationRegistry.Organisation.Exceptions;
    using Tests.Shared.Stubs;
    using Xunit;
    using Xunit.Abstractions;

    public class WhenUpdatingAnOrganisationLocationToAnAlreadyCoupledLocation : ExceptionSpecification<Organisation, OrganisationCommandHandlers, UpdateOrganisationLocation>
    {
        private OrganisationLocationAdded _organisationLocationAdded;
        private OrganisationLocationAdded _anotherOrganisationLocationAdded;
        private Guid _organisationId;
        private Guid _locationAId;
        private Guid _locationBId;
        private string _ovoNumber;

        protected override OrganisationCommandHandlers BuildHandler()
        {
            return new OrganisationCommandHandlers(
                new Mock<ILogger<OrganisationCommandHandlers>>().Object,
                Session,
                new SequentialOvoNumberGenerator(),
                null,
                new DateTimeProvider(),
                new OrganisationRegistryConfigurationStub(),
                Mock.Of<ISecurityService>());
        }

        protected override IEnumerable<IEvent> Given()
        {
            _organisationId = Guid.NewGuid();
            _locationAId = Guid.NewGuid();
            _locationBId = Guid.NewGuid();
            _organisationLocationAdded = new OrganisationLocationAdded(_organisationId, Guid.NewGuid(), _locationAId, "Gebouw A", false, null, null, null, null) { Version = 2 };
            _anotherOrganisationLocationAdded = new OrganisationLocationAdded(_organisationId, Guid.NewGuid(), _locationBId, "Gebouw B", false, null, null, null, null) { Version = 3 };

            _ovoNumber = "OVO000012345";
            return new List<IEvent>
            {
                new OrganisationCreated(_organisationId, "Kind en Gezin", _ovoNumber, "K&G", Article.None, "Kindjes en gezinnetjes", new List<Purpose>(), false, null, null, null, null),
                new LocationCreated(_locationAId, "12345", "Albert 1 laan 32, 1000 Brussel", "Albert 1 laan 32", "1000", "Brussel", "Belgie"),
                new LocationCreated(_locationBId, "12346", "Albert 1 laan 34, 1000 Brussel", "Albert 1 laan 32", "1000", "Brussel", "Belgie"),
                _organisationLocationAdded,
                _anotherOrganisationLocationAdded
            };
        }

        protected override UpdateOrganisationLocation When()
        {
            return new UpdateOrganisationLocation(
                _anotherOrganisationLocationAdded.OrganisationLocationId,
                new OrganisationId(_organisationId),
                new LocationId(_organisationLocationAdded.LocationId),
                false,
                null,
                new ValidFrom(),
                new ValidTo(),
                Source.Wegwijs)
            {
                User = new UserBuilder().AddRoles(Role.DecentraalBeheerder).AddOrganisations(_ovoNumber).Build()
            };;
        }

        protected override int ExpectedNumberOfEvents => 0;

        [Fact]
        public void ThrowsAnException()
        {
            Exception.Should().BeOfType<LocationAlreadyCoupledToInThisPeriod>();
            Exception.Message.Should().Be("Deze locatie is in deze periode reeds gekoppeld aan de organisatie.");
        }

        public WhenUpdatingAnOrganisationLocationToAnAlreadyCoupledLocation(ITestOutputHelper helper) : base(helper) { }
    }
}
