﻿namespace OrganisationRegistry.Tests.Shared.TestDataBuilders
{
    using System;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Organisation;

    public class OrganisationFormalFrameworkTestDataBuilder
    {
        private OrganisationFormalFramework _organisationFormalFramework;

        public OrganisationFormalFrameworkTestDataBuilder(ISpecimenBuilder fixture)
        {
            _organisationFormalFramework = new OrganisationFormalFramework(
                fixture.Create<Guid>(),
                fixture.Create<Guid>(),
                fixture.Create<string>(),
                fixture.Create<Guid>(),
                fixture.Create<string>(),
                fixture.Create<Period>());
        }

        public OrganisationFormalFrameworkTestDataBuilder WithValidity(ValidFrom from, ValidTo to)
        {
            _organisationFormalFramework = _organisationFormalFramework.WithValidity(new Period(from, to));
            return this;
        }

        public OrganisationFormalFrameworkTestDataBuilder WithFormalFrameworkId(Guid formalFrameworkId)
        {
            _organisationFormalFramework = new OrganisationFormalFramework(
                _organisationFormalFramework.OrganisationFormalFrameworkId,
                formalFrameworkId,
                _organisationFormalFramework.FormalFrameworkName,
                _organisationFormalFramework.ParentOrganisationId,
                _organisationFormalFramework.FormalFrameworkName,
                _organisationFormalFramework.Validity);
            return this;
        }

        public OrganisationFormalFramework Build()
            => _organisationFormalFramework;

        public static implicit operator OrganisationFormalFramework(OrganisationFormalFrameworkTestDataBuilder builder)
            => builder.Build();
    }
}
