using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;

namespace MyDockerProject.Controllers.Api;

[Route("engine")]
public class BookingEngineProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingEngineProxyController> _logger;

    public BookingEngineProxyController(
        IHttpClientFactory httpClientFactory,
        ILogger<BookingEngineProxyController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("BOOKING_ENGINE_URL") 
            ?? "http://booking_engine:3001");
        _logger = logger;
    }

    [HttpGet("{**path}")]
    [HttpPost("{**path}")]
    public async Task<IActionResult> Proxy(string? path)
    {
        try
        {
            var requestPath = string.IsNullOrEmpty(path) ? "/" : $"/{path}";
            var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : "";
            
            HttpRequestMessage request;
            
            if (Request.Method == "POST" || Request.Method == "PUT")
            {
                string bodyContent;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    bodyContent = await reader.ReadToEndAsync();
                }
                
                request = new HttpRequestMessage(new HttpMethod(Request.Method), requestPath + queryString)
                {
                    Content = new StringContent(bodyContent, Encoding.UTF8, Request.ContentType ?? "application/json")
                };
            }
            else
            {
                request = new HttpRequestMessage(new HttpMethod(Request.Method), requestPath + queryString);
            }
            
            // Copy headers
            foreach (var header in Request.Headers)
            {
                if (!header.Key.StartsWith(":") && header.Key != "Host")
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
            
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to booking engine: {Path}", path);
            return StatusCode(500, new { error = "Failed to connect to booking engine", message = ex.Message });
        }
    }
}

