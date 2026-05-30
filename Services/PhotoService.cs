using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace PinAppdePromo.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(IConfiguration config)
        {
            var acc = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );

            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> SubirImagenAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0) return null;

            var uploadResult = new ImageUploadResult();

            using (var stream = archivo.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(archivo.FileName, stream),
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult.SecureUrl?.ToString();
        }

        public async Task<bool> BorrarImagenAsync(string idPublico)
        {
            var deleteParams = new DeletionParams(idPublico);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
    }
}
