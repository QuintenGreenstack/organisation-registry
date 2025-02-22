namespace OrganisationRegistry.UnitTests.Organisation.UpdateOrganisationFormalFramework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using FormalFramework;
    using Infrastructure.Tests.Extensions.TestHelpers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using OrganisationRegistry.Infrastructure.Authorization;
    using Tests.Shared;
    using Tests.Shared.TestDataBuilders;
    using OrganisationRegistry.Infrastructure.Events;
    using OrganisationRegistry.Organisation;
    using OrganisationRegistry.Organisation.Commands;
    using OrganisationRegistry.Organisation.Events;
    using Tests.Shared.Stubs;
    using Xunit;
    using Xunit.Abstractions;

    public class WhenUpdatingAnOrganisationFormalFramework : Specification<Organisation, OrganisationCommandHandlers, UpdateOrganisationFormalFramework>
    {
        private static readonly DateTimeProviderStub DateTimeProviderStub = new DateTimeProviderStub(DateTime.Now);
        private readonly SequentialOvoNumberGenerator _ovoNumberGenerator = new SequentialOvoNumberGenerator();

        private OrganisationCreatedBuilder _childOrganisationCreated;
        private OrganisationCreatedBuilder _parentOrganisationACreated;
        private OrganisationCreatedBuilder _parentOrganisationBCreated;
        private FormalFrameworkCreatedBuilder _formalFrameworkACreated;
        private FormalFrameworkCreatedBuilder _formalFrameworkBCreated;
        private FormalFrameworkCategoryCreatedBuilder _formalFrameworkCategoryCreatedBuilder;
        private OrganisationFormalFrameworkAddedBuilder _childBecameDaughterOfParent;
        private readonly DateTime? _tomorrow = DateTimeProviderStub.Today.AddDays(1);

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

            _childOrganisationCreated = new OrganisationCreatedBuilder(_ovoNumberGenerator);
            _parentOrganisationACreated = new OrganisationCreatedBuilder(_ovoNumberGenerator);
            _parentOrganisationBCreated = new OrganisationCreatedBuilder(_ovoNumberGenerator);
            _formalFrameworkCategoryCreatedBuilder = new FormalFrameworkCategoryCreatedBuilder();
            _formalFrameworkACreated = new FormalFrameworkCreatedBuilder(_formalFrameworkCategoryCreatedBuilder.Id, _formalFrameworkCategoryCreatedBuilder.Name);
            _formalFrameworkBCreated = new FormalFrameworkCreatedBuilder(_formalFrameworkCategoryCreatedBuilder.Id, _formalFrameworkCategoryCreatedBuilder.Name);
            _childBecameDaughterOfParent =
                new OrganisationFormalFrameworkAddedBuilder(_childOrganisationCreated.Id, _formalFrameworkACreated.Id, _parentOrganisationACreated.Id);

            return new List<IEvent>
            {
                _childOrganisationCreated.Build(),
                _parentOrganisationACreated.Build(),
                _parentOrganisationBCreated.Build(),
                _formalFrameworkCategoryCreatedBuilder.Build(),
                _formalFrameworkACreated.Build(),
                _formalFrameworkBCreated.Build(),
                _childBecameDaughterOfParent.Build()
            };
        }

        protected override UpdateOrganisationFormalFramework When()
        {
            return new UpdateOrganisationFormalFramework(
                _childBecameDaughterOfParent.OrganisationFormalFrameworkId,
                new FormalFrameworkId(_formalFrameworkBCreated.Id),
                _childOrganisationCreated.Id,
                _parentOrganisationBCreated.Id,
                new ValidFrom(_tomorrow), new ValidTo(_tomorrow))
            {
                User = new UserBuilder()
                    .AddRoles(Role.AlgemeenBeheerder)
                    .Build()
            };
        }

        protected override int ExpectedNumberOfEvents => 1;

        [Fact]
        public void UpdatesTheOrganisationBuilding()
        {
            PublishedEvents[0].Should().BeOfType<Envelope<OrganisationFormalFrameworkUpdated>>();

            var organisationFormalFrameworkUpdated = PublishedEvents.First().UnwrapBody<OrganisationFormalFrameworkUpdated>();
            organisationFormalFrameworkUpdated.OrganisationId.Should().Be((Guid)_childOrganisationCreated.Id);
            organisationFormalFrameworkUpdated.PreviousParentOrganisationId.Should().Be((Guid)_parentOrganisationACreated.Id);
            organisationFormalFrameworkUpdated.ParentOrganisationId.Should().Be((Guid)_parentOrganisationBCreated.Id);
            organisationFormalFrameworkUpdated.FormalFrameworkId.Should().Be((Guid)_formalFrameworkBCreated.Id);
            organisationFormalFrameworkUpdated.ValidFrom.Should().Be(_tomorrow);
            organisationFormalFrameworkUpdated.ValidTo.Should().Be(_tomorrow);
        }

        public WhenUpdatingAnOrganisationFormalFramework(ITestOutputHelper helper) : base(helper) { }
    }
}
