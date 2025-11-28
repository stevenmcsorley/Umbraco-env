using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ImporterController : ControllerBase
{
    private static readonly List<object> _tempStorage = new();

    [HttpPost]
    public IActionResult ImportContent([FromBody] ContentJsonRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.ContentJson))
        {
            return BadRequest(new { error = "ContentJson is required" });
        }

        // Store in temp area for inspection (as per PRD requirement)
        var importEntry = new
        {
            id = Guid.NewGuid(),
            receivedAt = DateTime.UtcNow,
            contentJson = request.ContentJson,
            metadata = request.Metadata
        };

        _tempStorage.Add(importEntry);

        return Ok(new
        {
            success = true,
            importId = importEntry.id,
            message = "Content received and stored for inspection. No content nodes created.",
            storedAt = importEntry.receivedAt
        });
    }

    [HttpGet("temp")]
    public IActionResult GetTempImports()
    {
        return Ok(_tempStorage);
    }
}

public class ContentJsonRequest
{
    public string ContentJson { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

