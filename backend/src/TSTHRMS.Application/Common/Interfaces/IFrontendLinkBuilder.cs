namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Builds absolute links into the SPA for use in outbound emails to people who aren't logged in
/// (candidates) - the frontend's own base URL isn't knowable from inside an email, so it has to
/// be config-driven on the backend instead.
/// </summary>
public interface IFrontendLinkBuilder
{
    string BuildCareerSiteAssessmentLink(string tenantSlug, string token);

    string BuildCareerSiteOfferLink(string tenantSlug, string token);
}
