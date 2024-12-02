using MapOfActivitiesAPI.Interfaces;
using MapOfActivitiesAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MapOfActivitiesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsController : Controller
    {
        private MapOfActivitiesAPIContext _context;

        public ComplaintsController(MapOfActivitiesAPIContext context)
        {
            _context = context;
        }

        [HttpDelete]
        [Route("delete-all-complaints")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public async Task<IActionResult> DeleteAllComplaints()
        {
            try
            {
                _context.Complaints.RemoveRange(_context.Complaints);
                await _context.SaveChangesAsync();
                return Ok("All complaints deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting complaints: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("all-events-complaints")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public async Task<IActionResult> GetEventsComplaints()
        {
            if (_context.Complaints == null)
            {
                return NotFound();
            }

            var complaints = await _context.Complaints
                .Where(c => c.EventId != null)
                .Select(c => new
                {
                    c.Id,
                    c.EventId,
                    c.Header,
                    c.Description,
                    c.Time,
                    c.Status,
                    AuthorId = _context.Users.FirstOrDefault(u => u.Id == c.AuthorId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.AuthorId).UserId : null,
                    AuthorEmail = _context.Users.FirstOrDefault(u => u.Id == c.AuthorId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.AuthorId).Email : null,
                })
                .OrderBy(e => e.Status.StartsWith("Опрацьовано") || e.Status.StartsWith("Відхилено"))
                .ThenByDescending(e => e.Time)
                .ToListAsync();

            if (complaints == null)
            {
                return NoContent();
            }

            return Ok(complaints);
        }

        [HttpGet]
        [Route("all-users-complaints")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public async Task<IActionResult> GetUsersComplaints()
        {

            var complaints = await _context.Complaints
                .Where(c => c.UserId != null)
                .Select(c => new
                {
                    c.Id,
                    c.Header,
                    c.Description,
                    c.Time,
                    c.Status,
                    UserId = _context.Users.FirstOrDefault(u => u.Id == c.UserId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.UserId).UserId : null,
                    AuthorId = _context.Users.FirstOrDefault(u => u.Id == c.AuthorId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.AuthorId).UserId : null,
                    UserEmail = _context.Users.FirstOrDefault(u => u.Id == c.UserId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.UserId).Email : null,
                    AuthorEmail = _context.Users.FirstOrDefault(u => u.Id == c.AuthorId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.AuthorId).Email : null,
                })
                .OrderBy(e => e.Status.StartsWith("Опрацьовано") || e.Status.StartsWith("Відхилено"))
                .ThenByDescending(e => e.Time)
                .ToListAsync();

            if (complaints == null)
            {
                return NoContent();
            }

            return Ok(complaints);
        }



        /*
        [HttpGet]
        [Route("all-users-complaints")]
        public async Task<IActionResult> GetUsersComplaints()
        {
            if (_context.Complaints == null)
            {
                return NotFound();
            }

            var complaints = await _context.Complaints.Where(c => c.UserId != null).ToListAsync();

            //var userIds = complaints.Select(c => c.UserId).Distinct().ToList();


            if (complaints == null)
            {
                return NoContent();
            }

            return Ok(complaints); // треба додати пошту користувача й автора скарги у відправку на фронтенд
        }
        */

        [HttpGet]
        [Route("user-complaints/{userId}")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public async Task<IActionResult> GetUserComplaints(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            var userComplaints = await _context.Complaints
               .Where(c => c.UserId == user.Id)
               .Select(c => new
               {
                   c.Id,
                   c.Header,
                   c.Description,
                   AuthorEmail = _context.Users.FirstOrDefault(u => u.Id == c.AuthorId) != null ? _context.Users.FirstOrDefault(u => u.Id == c.AuthorId).Email : null,
               })
               .ToListAsync();


            if (userComplaints == null)
            {
                return NotFound();
            }

            return Ok(userComplaints);
        }


        //[Authorize(Roles = ApplicationUserRoles.Admin)]
        [HttpPost]
        [HttpPost("user-complaint/{userId}/{authorId}")]
        public async Task<ActionResult<Complaint>> PostComplaintUser(string userId, string authorId, Complaint c)
        {

            var toUser = await _context.Users.FirstOrDefaultAsync(e => e.UserId == userId);

            if (toUser == null)
            {
                return NotFound();
            }

            var fromUser = await _context.Users.FirstOrDefaultAsync(e => e.UserId == authorId);

            if (fromUser == null)
            {
                return NotFound();
            }

            Complaint UserComplaint = new Complaint();
            UserComplaint.Header = c.Header;
            UserComplaint.Description = c.Description;
            UserComplaint.UserId = toUser.Id;
            UserComplaint.AuthorId = fromUser.Id;
            UserComplaint.Status = "Очікується";

            DateTime currentTime = DateTime.UtcNow;
            UserComplaint.Time = currentTime;

            _context.Complaints.Add(UserComplaint);
            await _context.SaveChangesAsync();

            return Ok();
        }

        //[Authorize(Roles = ApplicationUserRoles.Admin)]
        [HttpDelete("user-complaint/{complaintId}")]
        public async Task<ActionResult<Complaint>> DeleteUserComplaint(string userId, int complaintId)
        {
            if (_context.Complaints == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserId == userId);

            var complaint = await _context.Complaints.Where(c => c.UserId == user.Id && c.Id == complaintId).FirstOrDefaultAsync();

            if (complaint == null)
            {
                return NotFound();
            }

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            return Ok();
        }

        //[Authorize(Roles = ApplicationUserRoles.Admin)]
        [HttpPost]
        [Route("event-complaint")]
        public async Task<ActionResult<Complaint>> PostComplainEvent(Complaint c)
        {

            var _event = await _context.Events.FirstOrDefaultAsync(e => e.Id == c.EventId);

            if (_event == null)
            {
                return NotFound();
            }

            //var author = await _context.Users.FirstOrDefaultAsync(u => u.Id == c.UserId);

            //if (author == null)
            //{
            //    return NotFound();
            //}

            Complaint EventComplaint = new Complaint();
            EventComplaint.Header = c.Header;
            EventComplaint.Description = c.Description;
            EventComplaint.EventId = _event.Id;
            EventComplaint.AuthorId = c.AuthorId;

            EventComplaint.Status = "Очікується";

            DateTime currentTime = DateTime.UtcNow;
            EventComplaint.Time = currentTime;

            _context.Complaints.Add(EventComplaint);

            await _context.SaveChangesAsync();

            return Ok();
        }

        //[Authorize(Roles = ApplicationUserRoles.Admin)]
        [HttpDelete("event-complaint/{complaintId}")]
        public async Task<ActionResult<Complaint>> DeleteEventComplaint(int eventId, int complaintId)
        {
            if (_context.Complaints == null)
            {
                return NotFound();
            }

            var _event = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);

            var complaint = await _context.Complaints.Where(c => c.EventId == _event.Id && c.Id == complaintId).FirstOrDefaultAsync();

            if (complaint == null)
            {
                return NotFound();
            }

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            return Ok();
        }


        [HttpPut]
        [Route("status-complaint")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public async Task<ActionResult<Complaint>> PutComplainStatusEvent(Complaint c)
        {

            var _complaint = await _context.Complaints.FirstOrDefaultAsync(e => e.Id == c.Id);

            if (_complaint == null)
            {
                return NotFound();
            }

            _complaint.Status = c.Status;
            
            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {
                if ((_context.Complaints?.Any(e => e.Id == c.Id)).GetValueOrDefault())
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            WebLog WebLog = new WebLog
            {
                Description = $"Cкарга номер {_complaint.Id}. Заголовок: {_complaint.Header}. Новий статус: {_complaint.Status}",
                Time = DateTime.UtcNow
            };

            _context.WebLogs.Add(WebLog);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
