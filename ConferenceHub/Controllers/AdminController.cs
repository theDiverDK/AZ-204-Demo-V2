using ConferenceHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public AdminController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // GET: Admin/SessionsJson
        public IActionResult SessionsJson()
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "sessions.json");
            var model = new AdminSessionsJsonViewModel
            {
                FilePath = path
            };

            try
            {
                if (!System.IO.File.Exists(path))
                {
                    model.Error = "File not found.";
                    return View(model);
                }

                var fileInfo = new FileInfo(path);
                model.LastModifiedUtc = fileInfo.LastWriteTimeUtc;
                model.SizeBytes = fileInfo.Length;
                model.Content = System.IO.File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                model.Error = ex.Message;
            }

            return View(model);
        }
    }
}
