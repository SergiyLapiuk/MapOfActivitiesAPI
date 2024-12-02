using Microsoft.AspNetCore.Mvc;

namespace MapOfActivitiesAPI.Interfaces
{
    public interface IFileStorage
    {
        public Task<string> Upload(string image);
        //public Task<bool> Upload(IFormFile image, string fileName);
        public Task<bool> Delete(string fileName);
        public Task<FileStreamResult> GetImage(string fileName);
    }
}
