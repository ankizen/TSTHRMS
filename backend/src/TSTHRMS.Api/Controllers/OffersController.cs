using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 8's internal side: create/revise/approve/send an offer. Same HR/Manager ownership
/// scoping as the other internal recruitment controllers.
/// </summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class OffersController(IOfferService offerService) : ControllerBase
{
    [HttpGet("api/recruitment/applications/{applicationId:guid}/offer")]
    public async Task<ActionResult<OfferDto>> GetForApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var offer = await offerService.GetForApplicationAsync(applicationId, cancellationToken);
        return offer is null ? NotFound() : Ok(offer);
    }

    [HttpPost("api/recruitment/applications/{applicationId:guid}/offer")]
    public async Task<ActionResult<OfferDto>> Create(
        Guid applicationId, CreateOrReviseOfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await offerService.CreateAsync(applicationId, request, cancellationToken);
        return offer is null ? BadRequest() : Ok(offer);
    }

    [HttpPut("api/recruitment/offers/{offerId:guid}")]
    public async Task<ActionResult<OfferDto>> Revise(
        Guid offerId, CreateOrReviseOfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await offerService.ReviseAsync(offerId, request, cancellationToken);
        return offer is null ? NotFound() : Ok(offer);
    }

    [HttpPost("api/recruitment/offers/{offerId:guid}/submit")]
    public async Task<ActionResult<OfferDto>> Submit(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await offerService.SubmitForApprovalAsync(offerId, cancellationToken);
        return offer is null ? NotFound() : Ok(offer);
    }

    [HttpPost("api/recruitment/offers/{offerId:guid}/approve")]
    public async Task<ActionResult<OfferDto>> Approve(
        Guid offerId, OfferDecisionRequest request, CancellationToken cancellationToken)
    {
        var offer = await offerService.ApproveAsync(offerId, request, cancellationToken);
        return offer is null ? NotFound() : Ok(offer);
    }

    [HttpPost("api/recruitment/offers/{offerId:guid}/send")]
    public async Task<ActionResult<OfferDto>> Send(
        Guid offerId, SendOfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await offerService.SendAsync(offerId, request, cancellationToken);
        return offer is null ? NotFound() : Ok(offer);
    }
}
