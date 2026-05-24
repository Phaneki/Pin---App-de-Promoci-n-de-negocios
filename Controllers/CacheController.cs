using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public CacheController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestRedis()
    {
        var tiempoCaché = await _cache.GetStringAsync("test_key");

        if (tiempoCaché == null)
        {
            tiempoCaché = DateTime.Now.ToString();
            await _cache.SetStringAsync("test_key", tiempoCaché);
            return Ok(new { mensaje = "Guardado en Redis por primera vez", hora = tiempoCaché });
        }

        return Ok(new { mensaje = "Leído desde Redis (Caché)", hora = tiempoCaché });
    }
}