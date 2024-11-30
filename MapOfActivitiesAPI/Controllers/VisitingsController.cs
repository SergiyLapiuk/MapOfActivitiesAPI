using MapOfActivitiesAPI.Interfaces;
using MapOfActivitiesAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MapOfActivitiesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitingsController : ControllerBase
    {
        private MapOfActivitiesAPIContext _context;
        public VisitingsController(MapOfActivitiesAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("users-go-to-the-event")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersGoToTheEvent(int eventId)
        {
            var eventsUsers = _context.Visitings
        .Where(e => e.EventId == eventId)
        .Select(u => u.UserId)
        .ToList();

            var users = new List<User>();
            foreach (var userId in eventsUsers)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    users.Add(user);
                }

            }
            return users;
        }

        [HttpGet]
        [Route("is-joiner")]
        public async Task<ActionResult<Boolean>> IsJoiner(string userId, int eventId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var visit = await _context.Visitings.Where(v => v.UserId == user.Id && v.EventId == eventId).FirstOrDefaultAsync();
            return Ok(visit != null);
        }

        [HttpPost]
        [Route("add-visiting")]

        public async Task<ActionResult<User>> CreateVisiting(string userId, int eventId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var visits = await _context.Visitings.Where(v => v.UserId == user.Id && v.EventId == eventId).FirstOrDefaultAsync();
            if (visits==null)
            {
                Visitings visit = new Visitings();
                visit.UserId = user.Id;
                visit.EventId = eventId;
                _context.Visitings.Add(visit);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPost]
        [Route("delete-visiting")]
        public async Task<IActionResult> DeleteVisiting(string userId, int eventId)
        {
            if (_context.Visitings == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var visit = await _context.Visitings.Where(v => v.UserId == user.Id && v.EventId == eventId).FirstOrDefaultAsync();
            if (visit == null)
            {
                return NotFound();
            }
            _context.Visitings.Remove(visit);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
