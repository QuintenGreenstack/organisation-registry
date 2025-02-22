namespace OrganisationRegistry.Api.Backoffice.Parameters.DelegationAssignments
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
    using OrganisationRegistry.Infrastructure.Authorization;
    using OrganisationRegistry.Infrastructure.Commands;
    using Queries;
    using Requests;
    using Responses;
    using Security;
    using SqlServer.Infrastructure;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("manage/delegations")]
    public class DelegationAssignmentController : OrganisationRegistryController
    {
        public DelegationAssignmentController(ICommandSender commandSender)
            : base(commandSender)
        {
        }

        /// <summary>Get a list of available delegation assignments.</summary>
        [HttpGet("{delegationId}/assignments")]
        [OrganisationRegistryAuthorize(Roles = Roles.AlgemeenBeheerder + "," + Roles.DecentraalBeheerder)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromServices] OrganisationRegistryContext context, [FromServices] ISecurityService securityService, [FromRoute] Guid delegationId)
        {
            var delegation = await context.DelegationList.FirstOrDefaultAsync(x => x.Id == delegationId);

            if (delegation == null)
                return NotFound();

            if (!await securityService.CanEditDelegation(User, delegation.OrganisationId, delegation.BodyId))
                return Unauthorized(); // ModelState.AddModelError("NotAllowed", "U hebt niet voldoende rechten voor deze delegatie.");

            var filtering = Request.ExtractFilteringRequest<DelegationAssignmentListItemFilter>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            var pagedDelegationAssignments =
                new DelegationAssignmentListQuery(context, delegationId).Fetch(filtering, sorting, pagination);

            Response.AddPaginationResponse(pagedDelegationAssignments.PaginationInfo);
            Response.AddSortingResponse(sorting.SortBy, sorting.SortOrder);

            return Ok(await pagedDelegationAssignments.Items.ToListAsync());
        }

        /// <summary>Get a delegation assignment.</summary>
        /// <response code="200">If the delegation assignment is found.</response>
        /// <response code="404">If the delegation assignment cannot be found.</response>
        [HttpGet("{delegationId}/assignments/{id}")]
        [OrganisationRegistryAuthorize(Roles = Roles.AlgemeenBeheerder + "," + Roles.DecentraalBeheerder)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] ISecurityService securityService,
            [FromRoute] Guid delegationId,
            Guid id)
        {
            var delegationAssignment = await context.DelegationAssignmentList.FirstOrDefaultAsync(x => x.Id == id);

            if (delegationAssignment == null)
                return NotFound();

            var delegation = await context.DelegationList.FirstOrDefaultAsync(x => x.Id == delegationId);

            if (delegation == null)
                return NotFound();

            if (!await securityService.CanEditDelegation(User, delegation.OrganisationId, delegation.BodyId))
                return Unauthorized(); // ModelState.AddModelError("NotAllowed", "U hebt niet voldoende rechten voor deze delegatie.");

            return Ok(new DelegationAssignmentResponse(delegationAssignment));
        }

        /// <summary>Create a delegation assignment for an organisation.</summary>
        /// <response code="201">If the delegation assignment is created, together with the location.</response>
        /// <response code="400">If the delegation assignment information does not pass validation.</response>
        [HttpPost("{delegationId}/assignments")]
        [OrganisationRegistryAuthorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] ISecurityService securityService,
            [FromRoute] Guid delegationId,
            [FromBody] AddDelegationAssignmentRequest message)
        {
            var internalMessage = new AddDelegationAssignmentInternalRequest(delegationId, message);

            // TODO: Discuss, should we depend on a projection to check which OrganisationId a delegation belongs to?
            var delegation = await context.DelegationList.FirstOrDefaultAsync(x => x.Id == delegationId);

            if (delegation == null)
                return NotFound();

            if (!await securityService.CanEditDelegation(User, delegation.OrganisationId, delegation.BodyId))
                ModelState.AddModelError("NotAllowed", "U hebt niet voldoende rechten voor deze delegatie.");

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(AddDelegationAssignmentRequestMapping.Map(internalMessage));

            return CreatedWithLocation(nameof(Get), new { delegationId = delegationId, id = message.DelegationAssignmentId });
        }

        /// <summary>Update a delegation assignment.</summary>
        /// <response code="200">If the delegation assignment is updated, together with the location.</response>
        /// <response code="400">If the delegation assignment information does not pass validation.</response>
        [HttpPut("{delegationId}/assignments/{id}")]
        [OrganisationRegistryAuthorize(Roles = Roles.AlgemeenBeheerder + "," + Roles.DecentraalBeheerder)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Put(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] ISecurityService securityService,
            [FromRoute] Guid delegationId,
            [FromRoute] Guid id,
            [FromBody] UpdateDelegationAssignmentRequest message)
        {
            var internalMessage = new UpdateDelegationAssignmentInternalRequest(delegationId, message);

            // TODO: Discuss, should we depend on a projection to check which OrganisationId a delegation belongs to?
            var delegation = await context.DelegationList.FirstOrDefaultAsync(x => x.Id == delegationId);

            if (delegation == null)
                return NotFound();

            if (!await securityService.CanEditDelegation(User, delegation.OrganisationId, delegation.BodyId))
                ModelState.AddModelError("NotAllowed", "U hebt niet voldoende rechten voor deze delegatie.");

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(UpdateDelegationAssignmentRequestMapping.Map(internalMessage));

            return OkWithLocationHeader(nameof(Get), new { delegationId = delegationId, id = id });
        }

        /// <summary>Remove a delegation assignment.</summary>
        /// <response code="200">If the delegation assignment is removed, together with the location.</response>
        /// <response code="400">If the delegation assignment information does not pass validation.</response>
        [HttpDelete("{delegationId}/assignments/{delegationAssignmentId}/{bodyId}/{bodySeatId}")]
        [OrganisationRegistryAuthorize(Roles = Roles.AlgemeenBeheerder)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(
            [FromServices] OrganisationRegistryContext context,
            [FromServices] ISecurityService securityService,
            [FromRoute] Guid delegationId,
            [FromRoute] Guid delegationAssignmentId,
            [FromRoute] Guid bodyId,
            [FromRoute] Guid bodySeatId)
        {
            var message = new RemoveDelegationAssignmentRequest
            {
                BodyId = bodyId,
                BodySeatId = bodySeatId,
                DelegationAssignmentId = delegationAssignmentId
            };

            var internalMessage = new RemoveDelegationAssignmentInternalRequest(delegationId, message);

            // TODO: Discuss, should we depend on a projection to check which OrganisationId a delegation belongs to?
            var delegation = await context.DelegationList.FirstOrDefaultAsync(x => x.Id == delegationId);

            if (delegation == null)
                return NotFound();

            if (!await securityService.CanEditDelegation(User, delegation.OrganisationId, delegation.BodyId))
                ModelState.AddModelError("NotAllowed", "U hebt niet voldoende rechten voor deze delegatie.");

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            await CommandSender.Send(RemoveDelegationAssignmentRequestMapping.Map(internalMessage));

            return OkWithLocationHeader(nameof(Get), new { delegationId = delegationId, id = delegationAssignmentId });
        }
    }
}
