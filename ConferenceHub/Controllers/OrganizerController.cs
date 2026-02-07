using ConferenceHub.Models;
using ConferenceHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceHub.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly IDataService _dataService;

        public OrganizerController(IDataService dataService)
        {
            _dataService = dataService;
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
                await _dataService.UpdateSessionAsync(session);
                TempData["Success"] = "Session updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(session);
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
