﻿namespace OrganisationRegistry.Api.Person
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using Infrastructure;
    using OrganisationRegistry.Infrastructure.Commands;
    using System;
    using SqlServer.Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Infrastructure.Search.Sorting;
    using Infrastructure.Search.Pagination;
    using Infrastructure.Search.Filtering;
    using Queries;
    using SqlServer.Person;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("people/{personId}/mandates")]
    public class PersonMandateController : OrganisationRegistryController
    {
        public PersonMandateController(ICommandSender commandSender)
            : base(commandSender)
        {
        }

        /// <summary>Get a list of available mandates for a person.</summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context, [FromRoute] Guid personId)
        {
            var filtering = Request.ExtractFilteringRequest<PersonMandateListItem>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            var pagedPeople = new PersonMandateListQuery(context, personId).Fetch(filtering, sorting, pagination);

            Response.AddPaginationResponse(pagedPeople.PaginationInfo);
            Response.AddSortingResponse(sorting.SortBy, sorting.SortOrder);

            return Ok(await pagedPeople.Items.ToListAsync());
        }
    }
}
