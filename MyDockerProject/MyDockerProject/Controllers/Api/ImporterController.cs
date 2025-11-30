using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Services;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ImporterController : ControllerBase
{
    private readonly DataImportService _importService;

    public ImporterController(DataImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportContent([FromBody] ImportRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.ContentJson))
        {
            return BadRequest(new { error = "ContentJson is required" });
        }

        try
        {
            var result = await _importService.ImportFromJsonAsync(request.ContentJson);
            
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    hotelsCreated = result.HotelsCreated,
                    hotelsUpdated = result.HotelsUpdated,
                    roomsCreated = result.RoomsCreated,
                    roomsUpdated = result.RoomsUpdated,
                    eventsCreated = result.EventsCreated,
                    eventsUpdated = result.EventsUpdated,
                    offersCreated = result.OffersCreated,
                    offersUpdated = result.OffersUpdated,
                    inventoryEntriesCreated = result.InventoryEntriesCreated,
                    inventoryEntriesUpdated = result.InventoryEntriesUpdated,
                    errors = result.Errors
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
    
    [HttpPost("import-file")]
    public async Task<IActionResult> ImportFromFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var jsonContent = await reader.ReadToEndAsync();
            
            var result = await _importService.ImportFromJsonAsync(jsonContent);
            
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    hotelsCreated = result.HotelsCreated,
                    hotelsUpdated = result.HotelsUpdated,
                    roomsCreated = result.RoomsCreated,
                    roomsUpdated = result.RoomsUpdated,
                    eventsCreated = result.EventsCreated,
                    eventsUpdated = result.EventsUpdated,
                    offersCreated = result.OffersCreated,
                    offersUpdated = result.OffersUpdated,
                    inventoryEntriesCreated = result.InventoryEntriesCreated,
                    inventoryEntriesUpdated = result.InventoryEntriesUpdated,
                    errors = result.Errors
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

public class ImportRequest
{
    public string ContentJson { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

