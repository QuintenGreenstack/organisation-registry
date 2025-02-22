﻿namespace OrganisationRegistry.Api.Backoffice.Parameters.SeatType
{
    using System;
    using System.Linq;
    using OrganisationRegistry.SeatType;
    using SqlServer.Infrastructure;

    public class UniqueNameValidator : IUniqueNameValidator<SeatType>
    {
        private readonly OrganisationRegistryContext _context;

        public UniqueNameValidator(OrganisationRegistryContext context)
        {
            _context = context;
        }

        public bool IsNameTaken(string name)
        {
            return _context.SeatTypeList.Any(item => item.Name == name);
        }

        public bool IsNameTaken(Guid id, string name)
        {
            return _context.SeatTypeList
                .AsQueryable()
                .Where(item => item.Id != id)
                .Any(item => item.Name == name);
        }
    }
}
