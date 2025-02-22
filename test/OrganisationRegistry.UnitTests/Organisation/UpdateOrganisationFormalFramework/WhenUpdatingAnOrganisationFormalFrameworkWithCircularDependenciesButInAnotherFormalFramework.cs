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

    public class WhenUpdatingAnOrganisationFormalFrameworkWithCircularDependenciesButInAnotherFormalFramework : Specification<Organisation, OrganisationCommandHandlers, UpdateOrganisationFormalFramework>
    {
        private static readonly DateTimeProviderStub DateTimeProviderStub = new DateTimeProviderStub(DateTime.Now);
        private readonly SequentialOvoNumberGenerator _ovoNumberGenerator = new SequentialOvoNumberGenerator();

        private OrganisationCreatedBuilder _childOrganisationCreated;
        private OrganisationCreatedBuilder _parentOrganisationCreated;
        private OrganisationCreatedBuilder _grandParentOrganisationCreated;
        private FormalFrameworkCreatedBuilder _formalFrameworkACreated;
        private FormalFrameworkCategoryCreatedBuilder _formalFrameworkCategoryCreatedBuilder;
        private OrganisationFormalFrameworkAddedBuilder _childBecameDaughterOfParent;
        private OrganisationFormalFrameworkAddedBuilder _parentBecameDaughterOfGrandParent;
        private OrganisationCreatedBuilder _greatGrandParentOrganisationCreated;
        private OrganisationFormalFrameworkAddedBuilder _grandParentBecameDaughterOfGreatGrandParent;
        private FormalFrameworkCreatedBuilder _formalFrameworkBCreated;

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
            _parentOrganisationCreated = new OrganisationCreatedBuilder(_ovoNumberGenerator);
            _grandParentOrganisationCreated = new OrganisationCreatedBuilder(_ovoNumberGenerator);
            _greatGrandParentOrganisationCreated = new OrganisationCreatedBuilder(_ovoNumberGenerator);
            _formalFrameworkCategoryCreatedBuilder = new FormalFrameworkCategoryCreatedBuilder();
            _formalFrameworkACreated = new FormalFrameworkCreatedBuilder(_formalFrameworkCategoryCreatedBuilder.Id, _formalFrameworkCategoryCreatedBuilder.Name);
            _formalFrameworkBCreated = new FormalFrameworkCreatedBuilder(_formalFrameworkCategoryCreatedBuilder.Id, _formalFrameworkCategoryCreatedBuilder.Name);
            _childBecameDaughterOfParent =
                new OrganisationFormalFrameworkAddedBuilder(_childOrganisationCreated.Id, _formalFrameworkACreated.Id, _parentOrganisationCreated.Id);
            _parentBecameDaughterOfGrandParent =
                new OrganisationFormalFrameworkAddedBuilder(_parentOrganisationCreated.Id, _formalFrameworkACreated.Id, _grandParentOrganisationCreated.Id);
            _grandParentBecameDaughterOfGreatGrandParent =
                new OrganisationFormalFrameworkAddedBuilder(_grandParentOrganisationCreated.Id, _formalFrameworkACreated.Id, _greatGrandParentOrganisationCreated.Id);

            return new List<IEvent>
            {
                _childOrganisationCreated.Build(),
                _parentOrganisationCreated.Build(),
                _grandParentOrganisationCreated.Build(),
                _greatGrandParentOrganisationCreated.Build(),
                _formalFrameworkCategoryCreatedBuilder.Build(),
                _formalFrameworkACreated.Build(),
                _formalFrameworkBCreated.Build(),
                _childBecameDaughterOfParent.Build(),
                _parentBecameDaughterOfGrandParent.Build(),
                _grandParentBecameDaughterOfGreatGrandParent.Build()
            };
        }

        protected override UpdateOrganisationFormalFramework When()
        {
            return new UpdateOrganisationFormalFramework(
                _grandParentBecameDaughterOfGreatGrandParent.OrganisationFormalFrameworkId,
                new FormalFrameworkId(_formalFrameworkBCreated.Id),
                _grandParentOrganisationCreated.Id,
                _childOrganisationCreated.Id,
                new ValidFrom(DateTimeProviderStub.Today), new ValidTo(DateTimeProviderStub.Today))
            {
                User = new UserBuilder()
                    .AddRoles(Role.AlgemeenBeheerder)
                    .Build()
            };
        }

        protected override int ExpectedNumberOfEvents => 2;

        [Fact]
        public void UpdatesTheOrganisationFormalFramework()
        {
            PublishedEvents[0].Should().BeOfType<Envelope<OrganisationFormalFrameworkUpdated>>();

            var organisationFormalFrameworkUpdated = PublishedEvents.First().UnwrapBody<OrganisationFormalFrameworkUpdated>();
            organisationFormalFrameworkUpdated.OrganisationId.Should().Be((Guid)_grandParentOrganisationCreated.Id);
            organisationFormalFrameworkUpdated.ParentOrganisationId.Should().Be((Guid)_childOrganisationCreated.Id);
            organisationFormalFrameworkUpdated.ValidFrom.Should().Be(DateTimeProviderStub.Today);
            organisationFormalFrameworkUpdated.ValidTo.Should().Be(DateTimeProviderStub.Today);
        }

        [Fact]
        public void AssignsAParent()
        {
            var frameworkAssignedToOrganisation = PublishedEvents[1].UnwrapBody<FormalFrameworkAssignedToOrganisation>();
            frameworkAssignedToOrganisation.OrganisationId.Should().Be((Guid)_grandParentOrganisationCreated.Id);
            frameworkAssignedToOrganisation.ParentOrganisationId.Should().Be((Guid)_childOrganisationCreated.Id);
            frameworkAssignedToOrganisation.FormalFrameworkId.Should().Be((Guid)_formalFrameworkBCreated.Id);
        }

        public WhenUpdatingAnOrganisationFormalFrameworkWithCircularDependenciesButInAnotherFormalFramework(ITestOutputHelper helper) : base(helper) { }
    }
}
