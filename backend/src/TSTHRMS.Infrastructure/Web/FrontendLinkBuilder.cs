using Microsoft.Extensions.Options;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.Web;

public class FrontendLinkBuilder(IOptions<FrontendOptions> options) : IFrontendLinkBuilder
{
    public string BuildCareerSiteAssessmentLink(string tenantSlug, string token) =>
        $"{options.Value.BaseUrl.TrimEnd('/')}/careers/{tenantSlug}/assessment/{token}";
}
