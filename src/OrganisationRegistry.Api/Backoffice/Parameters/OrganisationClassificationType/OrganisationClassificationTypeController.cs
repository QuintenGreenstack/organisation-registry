﻿namespace OrganisationRegistry.Api.Backoffice.Parameters.OrganisationClassificationType
{
    using System;
    using System.Threading.Tasks;
    using Infrastructure;
    using Infrastructure.Search.Filtering;
    using Infrastructure.Search.Pagination;
    using Infrastructure.Search.Sorting;
    using Infrastructure.Security;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using OrganisationRegistry.Configuration;
    using OrganisationRegistry.Infrastructure.Commands;
    using Queries;
    using Requests;
    using Security;
    using SqlServer.Infrastructure;
    using SqlServer.OrganisationClassificationType;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("organisationclassificationtypes")]
    public class OrganisationClassificationTypeController : OrganisationRegistryController
    {
        public OrganisationClassificationTypeController(ICommandSender commandSender)
            : base(commandSender)
        {
        }

        /// <summary>Get a list of available organisation classification types.</summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] IOrganisationRegistryConfiguration organisationRegistryConfiguration)
        {
            var filtering = Request.ExtractFilteringRequest<OrganisationClassificationTypeListItem>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            var pagedOrganisationClassificationTypes = new OrganisationClassificationTypeListQuery(context, organisationRegistryConfiguration).Fetch(filtering, sorting, pagination);

            Response.AddPaginationResponse(pagedOrganisationClassificationTypes.PaginationInfo);
            Response.AddSortingResponse(sorting.SortBy, sorting.SortOrder);

            return Ok(await pagedOrganisationClassificationTypes.Items.ToListAsync());
        }

        /// <summary>Get an organisation classificication type.</summary>
        /// <response code="200">If the organisation classification type is found.</response>
        /// <response code="404">If the organisation classificiation type cannot be found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context, [FromRoute] Guid id)
        {
            var key = await context.OrganisationClassificationTypeList.FirstOrDefaultAsync(x => x.Id == id);

            if (key == null)
                return NotFound();

            return Ok(key);
        }

        /// <summary>Create an organisation classification type.</summary>
        /// <response code="201">If the organisation classificiation type is created, together with the location.</response>
        /// <response code="400">If the organisation classificiation type information does not pass validation.</response>
        [HttpPost]
        [OrganisationRegistryAuthorize(Roles = Roles.AlgemeenBeheerder)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] CreateOrganisationClassificationTypeRequest message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await CommandSender.Send(CreateOrganisationClassificationTypeRequestMapping.Map(message));

            return CreatedWithLocation(nameof(Get), new { id = message.Id });
        }

        /// <summary>Update an organisation classification type.</summary>
        /// <response code="200">If the organisation classification type is updated, together with the location.</response>
        /// <response code="400">If the organisation classification type information does not pass validation.</response>
        [HttpPut("{id}")]
        [OrganisationRegistryAuthorize(Roles = Roles.AlgemeenBeheerder)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] UpdateOrganisationClassificationTypeRequest message)
        {
            var internalMessage = new UpdateOrganisationClassificationTypeInternalRequest(id, message);

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(UpdateOrganisationClassificationTypeRequestMapping.Map(internalMessage));

            return OkWithLocationHeader(nameof(Get), new { id = internalMessage.OrganisationClassificationTypeId });
        }
    }
}
