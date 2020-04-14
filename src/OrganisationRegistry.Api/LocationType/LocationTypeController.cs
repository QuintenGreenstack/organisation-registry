﻿namespace OrganisationRegistry.Api.LocationType
{
    using System;
    using System.Threading.Tasks;
    using Infrastructure;
    using Microsoft.AspNetCore.Mvc;
    using Requests;
    using OrganisationRegistry.Infrastructure.Commands;
    using Queries;
    using Infrastructure.Search.Filtering;
    using Infrastructure.Search.Pagination;
    using Infrastructure.Search.Sorting;
    using Microsoft.EntityFrameworkCore;
    using SqlServer.Infrastructure;
    using SqlServer.LocationType;
    using System.Net;
    using Infrastructure.Security;
    using Security;
    using OrganisationRegistry.Organisation;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("locationtypes")]
    public class LocationTypeController : OrganisationRegistryController
    {
        public LocationTypeController(ICommandSender commandSender)
            : base(commandSender)
        {
        }

        /// <summary>Get a list of available location types.</summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] IOrganisationRegistryConfiguration configuration)
        {
            var filtering = Request.ExtractFilteringRequest<LocationTypeListItem>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            var pagedLocationTypes = new LocationTypeListQuery(context, configuration).Fetch(filtering, sorting, pagination);

            Response.AddPaginationResponse(pagedLocationTypes.PaginationInfo);
            Response.AddSortingResponse(sorting.SortBy, sorting.SortOrder);

            return Ok(await pagedLocationTypes.Items.ToListAsync());
        }

        /// <summary>Get a location type.</summary>
        /// <response code="200">If the location type is found.</response>
        /// <response code="404">If the location type cannot be found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LocationTypeListItem), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(NotFoundResult), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context, [FromRoute] Guid id)
        {
            var key = await context.LocationTypeList.FirstOrDefaultAsync(x => x.Id == id);

            if (key == null)
                return NotFound();

            return Ok(key);
        }

        /// <summary>Create a location type.</summary>
        /// <response code="201">If the location type is created, together with the location.</response>
        /// <response code="400">If the location type information does not pass validation.</response>
        [HttpPost]
        [OrganisationRegistryAuthorize(Roles = Roles.OrganisationRegistryBeheerder)]
        [ProducesResponseType(typeof(CreatedResult), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(BadRequestResult), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] CreateLocationTypeRequest message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await CommandSender.Send(CreateLocationTypeRequestMapping.Map(message));

            return Created(Url.Action(nameof(Get), new { id = message.Id }), null);
        }

        /// <summary>Update a location type.</summary>
        /// <response code="200">If the location type is updated, together with the location.</response>
        /// <response code="400">If the location type information does not pass validation.</response>
        [HttpPut("{id}")]
        [OrganisationRegistryAuthorize(Roles = Roles.OrganisationRegistryBeheerder)]
        [ProducesResponseType(typeof(OkResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BadRequestResult), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] UpdateLocationTypeRequest message)
        {
            var internalMessage = new UpdateLocationTypeInternalRequest(id, message);

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(UpdateLocationTypeRequestMapping.Map(internalMessage));

            return OkWithLocation(Url.Action(nameof(Get), new { id = internalMessage.LocationTypeId }));
        }
    }
}
