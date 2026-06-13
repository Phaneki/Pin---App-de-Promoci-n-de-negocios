using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PinAppdePromo.Controllers
{
    /// <summary>
    /// 📸 Controlador API para sincronizar y gestionar imágenes de negocios desde Google Places
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessImagesController : ControllerBase
    {
        private readonly PinDbContext _context;
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly ILogger<BusinessImagesController> _logger;

        public BusinessImagesController(PinDbContext context, IGooglePlacesService googlePlacesService, ILogger<BusinessImagesController> logger)
        {
            _context = context;
            _googlePlacesService = googlePlacesService;
            _logger = logger;
        }

        /// <summary>
        /// 🔄 Sincroniza imágenes de todos los negocios sin fotos desde Google Places
        /// Endpoint: POST /api/businessimages/sync-all
        /// </summary>
        [HttpGet("sync-all")]
        [HttpPost("sync-all")]
        public async Task<IActionResult> SyncAllBusinessImages()
        {
            try
            {
                _logger.LogInformation("📸 Iniciando sincronización de imágenes para todos los negocios...");

                // Obtener todos los negocios que NO tienen imágenes
                var businessesWithoutImages = await _context.Businesses
                    .Where(b => !b.Images.Any())
                    .ToListAsync();

                if (!businessesWithoutImages.Any())
                {
                    return Ok(new { message = "✅ Todos los negocios ya tienen imágenes." });
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var business in businessesWithoutImages)
                {
                    try
                    {
                        var photoUrl = await _googlePlacesService.GetBusinessPhotoUrlAsync(business.TradeName, business.Address);

                        if (!string.IsNullOrEmpty(photoUrl))
                        {
                            // Crear nueva imagen en la base de datos
                            var businessImage = new BusinessImage
                            {
                                BusinessId = business.BusinessId,
                                ImageUrl = photoUrl
                            };

                            _context.BusinessImages.Add(businessImage);
                            successCount++;
                            _logger.LogInformation($"✅ Imagen sincronizada para: {business.TradeName}");
                        }
                        else
                        {
                            failureCount++;
                            _logger.LogWarning($"⚠️ No se pudo obtener imagen para: {business.TradeName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError($"❌ Error sincronizando {business.TradeName}: {ex.Message}");
                    }
                }

                // Guardar cambios
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"✅ Sincronización completada",
                    successful = successCount,
                    failed = failureCount,
                    total = businessesWithoutImages.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en sincronización general: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 🔄 Sincroniza imagen de un negocio específico
        /// Endpoint: POST /api/businessimages/sync/{businessId}
        /// </summary>
        [HttpGet("sync/{businessId:int}")]
        [HttpPost("sync/{businessId:int}")]
        public async Task<IActionResult> SyncBusinessImage(int businessId)
        {
            try
            {
                var business = await _context.Businesses
                    .Include(b => b.Images)
                    .FirstOrDefaultAsync(b => b.BusinessId == businessId);

                if (business == null)
                {
                    return NotFound(new { error = "Negocio no encontrado" });
                }

                // Si ya tiene imágenes, no necesita sincronizar
                if (business.Images.Any())
                {
                    return Ok(new { message = "Este negocio ya tiene imágenes.", imageCount = business.Images.Count });
                }

                _logger.LogInformation($"📸 Sincronizando imagen para: {business.TradeName}");

                var photoUrl = await _googlePlacesService.GetBusinessPhotoUrlAsync(business.TradeName, business.Address);

                if (string.IsNullOrEmpty(photoUrl))
                {
                    return BadRequest(new { error = "No se pudo obtener imagen de Google Places" });
                }

                // Crear nueva imagen
                var businessImage = new BusinessImage
                {
                    BusinessId = business.BusinessId,
                    ImageUrl = photoUrl
                };

                _context.BusinessImages.Add(businessImage);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "✅ Imagen sincronizada correctamente",
                    imageUrl = photoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en sincronización: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 🖼️ Obtiene la foto de un negocio sin guardarla (preview)
        /// Endpoint: GET /api/businessimages/preview/{businessId}
        /// </summary>
        [HttpGet("preview/{businessId:int}")]
        public async Task<IActionResult> GetBusinessPhotoPreview(int businessId)
        {
            try
            {
                var business = await _context.Businesses.FindAsync(businessId);
                if (business == null)
                {
                    return NotFound(new { error = "Negocio no encontrado" });
                }

                var photoUrl = await _googlePlacesService.GetBusinessPhotoUrlAsync(business.TradeName, business.Address);

                return Ok(new
                {
                    businessId = businessId,
                    tradeName = business.TradeName,
                    photoUrl = photoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error obteniendo preview: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// ❌ Elimina una imagen de negocio
        /// Endpoint: DELETE /api/businessimages/{imageId}
        /// </summary>
        [HttpDelete("{imageId:int}")]
        public async Task<IActionResult> DeleteBusinessImage(int imageId)
        {
            try
            {
                var image = await _context.BusinessImages.FindAsync(imageId);
                if (image == null)
                {
                    return NotFound(new { error = "Imagen no encontrada" });
                }

                _context.BusinessImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok(new { message = "✅ Imagen eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error eliminando imagen: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
