using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController(IApplicationDbContext dbContext, IFileStorageService fileStorageService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var stream = await fileStorageService.OpenReadAsync(document.StorageKey, cancellationToken);
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, document.ContentType, document.FileName);
    }
}
