﻿namespace OrganisationRegistry.Api.Edit.Organisation
{
    using System;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Infrastructure;
    using Infrastructure.Security;
    using Infrastructure.Swagger;
    using Infrastructure.Swagger.Examples;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.FeatureManagement.Mvc;
    using OrganisationRegistry.Infrastructure;
    using OrganisationRegistry.Infrastructure.Authorization;
    using OrganisationRegistry.Infrastructure.Commands;
    using Requests;
    using Swashbuckle.AspNetCore.Filters;
    using ProblemDetails = Be.Vlaanderen.Basisregisters.BasicApiProblem.ProblemDetails;

    [FeatureGate(FeatureFlags.EditApi)]
    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [OrganisationRegistryRoute("edit/organisations/{organisationId:guid}/keys")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Organisatiesleutels")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.EditApi)]
    public class OrganisationKeyController : OrganisationRegistryController
    {
        public OrganisationKeyController(ICommandSender commandSender)
            : base(commandSender)
        {
        }

        /// <summary>Voeg een organisatiesleutel toe.</summary>
        /// <remarks>Voegt een organisatiesleutel toe aan een organisatie.
        /// <br />
        /// Organisatiesleutels van hetzelfde type mogen niet overlappen in tijd.</remarks>
        /// <param name="securityService"></param>
        /// <param name="organisationId">Id van de organisatie.</param>
        /// <param name="message"></param>
        /// <response code="201">Als het verzoek aanvaard is.</response>
        /// <response code="400">Als het verzoek ongeldige data bevat.<example>a;sdlf</example></response>
        /// <response code="500">Als er een interne fout is opgetreden.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrors), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status201Created, typeof(EmptyResponseExamples))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples))]
        [SwaggerLocationHeader]
        public async Task<IActionResult> Post(
            [FromServices] IEditSecurityService securityService,
            [FromRoute] Guid organisationId,
            [FromBody] AddOrganisationKeyRequest message)
        {
            var internalMessage = new AddOrganisationKeyInternalRequest(organisationId, message);

            if (!securityService.CanAddKey(message.KeyTypeId))
                return Forbid();

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            var addOrganisationKey = AddOrganisationKeyRequestMapping.Map(internalMessage);
            addOrganisationKey.User = WellknownUsers.Orafin;
            await CommandSender.Send(addOrganisationKey);

            return CreatedWithLocation(
                nameof(Backoffice.Organisation.OrganisationKeyController.Get),
                    new { id = message.OrganisationKeyId });
        }

        /// <summary>Pas een organisatiesleutel aan.</summary>
        /// <param name="organisationId">Id van de organisatie.</param>
        /// <param name="securityService"></param>
        /// <param name="organisationKeyId">Id van de organisatiesleutel.</param>
        /// <param name="message"></param>
        /// <response code="200">Indien de organisatiesleutel succesvol is aangepast.</response>
        /// <response code="400">Indien er validatiefouten zijn.</response>
        [HttpPut("{organisationKeyId:guid}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrors), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(EmptyResponseExamples))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples))]
        public async Task<IActionResult> Put(
            [FromServices] IEditSecurityService securityService,
            [FromRoute] Guid organisationKeyId,
            [FromRoute] Guid organisationId,
            [FromBody] UpdateOrganisationKeyRequest message)
        {
            var internalMessage = new UpdateOrganisationKeyInternalRequest(
                organisationId,
                organisationKeyId,
                message);

            if (!securityService.CanEditKey(message.KeyTypeId))
                ModelState.AddModelError("NotAllowed", "U hebt niet voldoende rechten voor dit sleuteltype.");

            if (!TryValidateModel(internalMessage))
                return BadRequest(ModelState);

            var updateOrganisationKey = UpdateOrganisationKeyRequestMapping.Map(internalMessage);
            updateOrganisationKey.User = WellknownUsers.Orafin;
            await CommandSender.Send(updateOrganisationKey);

            return Ok();
        }
    }
}
