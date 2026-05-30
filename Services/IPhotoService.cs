using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PinAppdePromo.Services
{
    public interface IPhotoService
    {
        Task<string> SubirImagenAsync(IFormFile archivo);
        Task<bool> BorrarImagenAsync(string idPublico);
    }
}
