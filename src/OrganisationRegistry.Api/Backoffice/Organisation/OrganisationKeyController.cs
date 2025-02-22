﻿namespace OrganisationRegistry.Api.Backoffice.Organisation
{
    using System;
    using System.Threading.Tasks;
    using Handling.Authorization;
    using Infrastructure;
    using Infrastructure.Search.Filtering;
    using Infrastructure.Search.Pagination;
    using Infrastructure.Search.Sorting;
    using Infrastructure.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using OrganisationRegistry.Configuration;
    using OrganisationRegistry.Infrastructure.AppSpecific;
    using OrganisationRegistry.Infrastructure.Authorization;
    using OrganisationRegistry.Infrastructure.Commands;
    using Queries;
    using Requests;
    using SqlServer.Infrastructure;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("organisations/{organisationId}/keys")]
    public class OrganisationKeyController : OrganisationRegistryController
    {
        public OrganisationKeyController(ICommandSender commandSender)
            : base(commandSender)
        {
        }

        /// <summary>Get a list of available keys for an organisation.</summary>
        [HttpGet]
        [OrganisationRegistryAuthorize]
        [AllowAnonymous]
        public async Task<IActionResult> Get(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] IMemoryCaches memoryCaches,
            [FromServices] IOrganisationRegistryConfiguration configuration,
            [FromServices] ISecurityService securityService,
            [FromRoute] Guid organisationId)
        {
            var filtering = Request.ExtractFilteringRequest<OrganisationKeyListItemFilter>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            var user = await securityService.GetUser(User);
            Func<Guid, bool> isAuthorizedForKeyType = keyTypeId => new KeyPolicy(
                    memoryCaches.OvoNumbers[organisationId],
                    configuration,
                    keyTypeId)
                .Check(user)
                .IsSuccessful;

            var pagedOrganisations = new OrganisationKeyListQuery(context, organisationId, isAuthorizedForKeyType).Fetch(filtering, sorting, pagination);

            Response.AddPaginationResponse(pagedOrganisations.PaginationInfo);
            Response.AddSortingResponse(sorting.SortBy, sorting.SortOrder);

            return Ok(await pagedOrganisations.Items.ToListAsync());
        }

        /// <summary>Get a key for an organisation.</summary>
        /// <response code="200">If the key is found.</response>
        /// <response code="404">If the key cannot be found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context, [FromRoute] Guid organisationId, [FromRoute] Guid id)
        {
            var organisation = await context.OrganisationKeyList.FirstOrDefaultAsync(x => x.OrganisationKeyId == id);

            if (organisation == null)
                return NotFound();

            return Ok(organisation);
        }

        /// <summary>Create a key for an organisation.</summary>
        /// <response code="201">If the key is created, together with the location.</response>
        /// <response code="400">If the key information does not pass validation.</response>
        [HttpPost]
        [OrganisationRegistryAuthorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromServices] ISecurityService securityService, [FromRoute] Guid organisationId, [FromBody] AddOrganisationKeyRequest message)
        {
            var internalMessage = new AddOrganisationKeyInternalRequest(organisationId, message);

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(AddOrganisationKeyRequestMapping.Map(internalMessage));

            return CreatedWithLocation(nameof(Get), new { id = message.OrganisationKeyId });
        }

        /// <summary>Update a key for an organisation.</summary>
        /// <response code="201">If the key is updated, together with the location.</response>
        /// <response code="400">If the key information does not pass validation.</response>
        [HttpPut("{id}")]
        [OrganisationRegistryAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put([FromServices] ISecurityService securityService, [FromRoute] Guid organisationId, [FromBody] UpdateOrganisationKeyRequest message)
        {
            var internalMessage = new UpdateOrganisationKeyInternalRequest(organisationId, message);

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(UpdateOrganisationKeyRequestMapping.Map(internalMessage));

            return Ok();
        }
    }
}
