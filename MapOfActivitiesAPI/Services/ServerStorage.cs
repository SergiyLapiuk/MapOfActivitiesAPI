using MapOfActivitiesAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace MapOfActivitiesAPI.Services
{
    public class ServerStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _webhost;
        private readonly string folder = "/imagesEvent/";
        private readonly string url = "/imagesEvent/";
        public ServerStorage(IWebHostEnvironment webhost)
        {
            _webhost = webhost;
        }

        public async Task<string> Upload(string image)
        {
            string fileName = string.Empty;

            if(image == "")
            {
                return null;
            }

            try
            {
                // Витягуємо дані з base64 рядка
                string base64Data = Regex.Match(image, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                byte[] byteArray = Convert.FromBase64String(base64Data);

                // Визначаємо тип файлу
                string fileType = Regex.Match(image, @"data:image/(?<type>.+?);").Groups["type"].Value;
                // Генеруємо унікальне ім'я для файлу
                fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}.{fileType}";

                // Створюємо потік для зображення
                using var imageStream = new MemoryStream(byteArray);
                // Повний шлях до файла
                string filePath = Path.Combine(_webhost.WebRootPath + folder, fileName); // Переконайтеся, що "folder" існує
                using FileStream stream = new FileStream(filePath, FileMode.Create);
                await imageStream.CopyToAsync(stream);
            }
            catch (Exception e)
            {
                // Виводимо більш інформативне повідомлення про помилку
                throw new Exception($"Failed to upload image: {e.Message}", e);
            }
            // Повертаємо ім'я файла як результат
            return fileName;
        }

        public async Task<FileStreamResult> GetImage(string id)
        {
            var filePath = Path.Combine(_webhost.WebRootPath + folder, id);

            // Тут можна додати перевірку на існування файлу та інші перевірки безпеки
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Image not found.");
            }

            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0; // Скидуємо позицію для читання

            return new FileStreamResult(memoryStream, "image/jpeg");
        }

        public async Task<bool> Delete(string fileName)
        {
            if (fileName == "") return false;
            try
            {
                System.IO.File.Delete(_webhost.WebRootPath + folder + fileName);
            }
            catch
            {
                throw new Exception();
            }
            return true;
        }
    }
}
