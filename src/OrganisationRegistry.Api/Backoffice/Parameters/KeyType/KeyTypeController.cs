﻿namespace OrganisationRegistry.Api.Backoffice.Parameters.KeyType
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Infrastructure;
    using Infrastructure.Search.Filtering;
    using Infrastructure.Search.Pagination;
    using Infrastructure.Search.Sorting;
    using Infrastructure.Security;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using OrganisationRegistry.Infrastructure.Authorization;
    using OrganisationRegistry.Infrastructure.Commands;
    using OrganisationRegistry.Organisation;
    using Queries;
    using Requests;
    using Security;
    using SqlServer.Infrastructure;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("keytypes")]
    public class KeyTypeController : OrganisationRegistryController
    {
        private readonly ISecurityService _securityService;
        private readonly IOrganisationRegistryConfiguration _configuration;

        public KeyTypeController(ICommandSender commandSender,
            ISecurityService securityService,
            IOrganisationRegistryConfiguration configuration)
            : base(commandSender)
        {
            _securityService = securityService;
            _configuration = configuration;
        }

        /// <summary>Get a list of available key types.</summary>
        [HttpGet]
        [OrganisationRegistryAuthorize]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context)
        {
            var filtering = Request.ExtractFilteringRequest<KeyTypeListQuery.KeyTypeListItemFilter>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            filtering.Filter ??= new KeyTypeListQuery.KeyTypeListItemFilter();

            if (!_securityService.CanUseKeyType(_securityService.GetUser(User), _configuration.OrafinKeyTypeId))
                filtering.Filter.ExcludeIds.Add(_configuration.OrafinKeyTypeId);

            var pagedKeyTypes = new KeyTypeListQuery(context).Fetch(filtering, sorting, pagination);

            Response.AddPaginationResponse(pagedKeyTypes.PaginationInfo);
            Response.AddSortingResponse(sorting.SortBy, sorting.SortOrder);

            return Ok(await pagedKeyTypes.Items.ToListAsync());
        }

        /// <summary>Get a key type.</summary>
        /// <response code="200">If the key type is found.</response>
        /// <response code="404">If the key type cannot be found.</response>
        [HttpGet("{id}")]
        [OrganisationRegistryAuthorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context, [FromRoute] Guid id)
        {
            var keyType = await context.KeyTypeList.FirstOrDefaultAsync(x => x.Id == id);

            if (keyType == null)
                return NotFound();

            return Ok(keyType);
        }

        /// <summary>Create a key type.</summary>
        /// <response code="201">If the key type is created, together with the location.</response>
        /// <response code="400">If the key type information does not pass validation.</response>
        [HttpPost]
        [OrganisationRegistryAuthorize(Roles = Roles.OrganisationRegistryBeheerder)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] CreateKeyTypeRequest message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await CommandSender.Send(CreateKeyTypeRequestMapping.Map(message));

            return Created(Url.Action(nameof(Get), new { id = message.Id }), null);
        }

        /// <summary>Update a key type.</summary>
        /// <response code="200">If the key type is updated, together with the location.</response>
        /// <response code="400">If the key type information does not pass validation.</response>
        [HttpPut("{id}")]
        [OrganisationRegistryAuthorize(Roles = Roles.OrganisationRegistryBeheerder)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] UpdateKeyTypeRequest message)
        {
            var internalMessage = new UpdateKeyTypeInternalRequest(id, message);

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(UpdateKeyTypeRequestMapping.Map(internalMessage));

            return OkWithLocation(Url.Action(nameof(Get), new { id = internalMessage.KeyTypeId }));
        }
    }
}
