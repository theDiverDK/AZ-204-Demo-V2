using ConferenceHub.Models;
using ConferenceHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceHub.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly IDataService _dataService;
        private readonly ISlideStorageService _slideStorageService;
        private static readonly string[] AllowedSlideExtensions = [".pdf", ".jpg", ".jpeg"];

        public OrganizerController(IDataService dataService, ISlideStorageService slideStorageService)
        {
            _dataService = dataService;
            _slideStorageService = slideStorageService;
        }

        // GET: Organizer
        public async Task<IActionResult> Index()
        {
            var sessions = await _dataService.GetSessionsAsync();
            var registrations = await _dataService.GetRegistrationsAsync();
            ViewBag.TotalRegistrations = registrations.Count;
            return View(sessions);
        }

        // GET: Organizer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Organizer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
            if (ModelState.IsValid)
            {
                session.CurrentRegistrations = 0;
                session.SlideUrls ??= new List<string>();
                await _dataService.AddSessionAsync(session);
                TempData["Success"] = "Session created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(session);
        }

        // GET: Organizer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var session = await _dataService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }
            return View(session);
        }

        // POST: Organizer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session)
        {
            if (id != session.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingSession = await _dataService.GetSessionByIdAsync(id);
                if (existingSession == null)
                {
                    return NotFound();
                }

                session.SlideUrls = existingSession.SlideUrls;
                await _dataService.UpdateSessionAsync(session);
                TempData["Success"] = "Session updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(session);
        }

        // POST: Organizer/UploadSlides/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSlides(int id, List<IFormFile> slides)
        {
            var session = await _dataService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var validSlides = slides
                .Where(s => s.Length > 0 && AllowedSlideExtensions.Contains(Path.GetExtension(s.FileName).ToLowerInvariant()))
                .ToList();

            if (!validSlides.Any())
            {
                TempData["Error"] = "No valid slides selected. Allowed file types: PDF, JPG, JPEG.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var uploadedUrls = await _slideStorageService.UploadSlidesAsync(id, validSlides);
            session.SlideUrls.AddRange(uploadedUrls);
            await _dataService.UpdateSessionAsync(session);

            TempData["Success"] = $"Uploaded {uploadedUrls.Count} slide(s) successfully.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: Organizer/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _dataService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }
            return View(session);
        }

        // POST: Organizer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _dataService.DeleteSessionAsync(id);
            TempData["Success"] = "Session deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Organizer/Registrations
        public async Task<IActionResult> Registrations()
        {
            var registrations = await _dataService.GetRegistrationsAsync();
            var sessions = await _dataService.GetSessionsAsync();
            
            var registrationDetails = registrations.Select(r => new
            {
                Registration = r,
                Session = sessions.FirstOrDefault(s => s.Id == r.SessionId)
            }).ToList();

            return View(registrationDetails);
        }
    }
}
