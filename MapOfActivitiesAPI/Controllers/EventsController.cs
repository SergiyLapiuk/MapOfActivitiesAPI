using MapOfActivitiesAPI.Interfaces;
using MapOfActivitiesAPI.Models;
using MapOfActivitiesAPI.ModelsDTO;
using MapOfActivitiesAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Security.Claims;

namespace MapOfActivitiesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IFileStorage _fileStorage;
        private MapOfActivitiesAPIContext _context;
        private readonly ITokenService _tokenService;

        public EventsController(IFileStorage fileStorage, MapOfActivitiesAPIContext context, ITokenService tokenService)
        {
            _fileStorage = fileStorage;
            _context = context;
            _tokenService = tokenService;
        }
        //   [Authorize(Roles = ApplicationUserRoles.User)]
        [HttpGet]
        public IEnumerable<Event> Get()
        {
            return _context.Events;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            if (_context.Events == null)
            {
                return NotFound();
            }
            var confectioner = _context.Events.Include(x => x.Type).Include(x => x.User).Where(x => x.Id == id).FirstOrDefault();
            if (confectioner == null)
            {
                return NotFound();
            }

            return confectioner;
        }

        [HttpGet]
        [Route("user-events/{userId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetUserEvents(int userId)
        {
            if (_context.Events == null)
            {
                return NotFound();
            }
            // Отримання списку EventId з відвідувань користувача
            var userVisitingsEventIds = await _context.Visitings
                .Where(x => x.UserId == userId)
                .Select(v => v.EventId) // Вибірка EventId з відвідувань
                .ToListAsync();

            // Отримання подій, EventId яких входять у список userVisitingsEventIds
            var userEvents = await _context.Events
                .Where(x => userVisitingsEventIds.Contains(x.Id)) // Фільтрація за EventId
                .ToListAsync();


            return userEvents;
        }

        [HttpGet]
        [Route("author-events/{userId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetAuthorEvents(int userId)
        {
            if (_context.Events == null)
            {
                return NotFound();
            }

            // Отримання подій за userId
            var userEvents = await _context.Events
                .Where(x => x.UserId == userId).ToListAsync(); // Фільтрація за userId

            return userEvents;
        }

        [HttpGet("filter")]
        public IEnumerable<EventDTO> GetEventsByFilter([FromQuery] string? searchName = null,
            [FromQuery] string? userPoint = null,
            [FromQuery] double? distance = null,
            [FromQuery] List<int>? types = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            var points = _context.Events.Include(x => x.Type).Include(x => x.User).AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                points = points.AsEnumerable().Where(p => p.Name.ToLower().Contains(searchName.ToLower())).AsQueryable();
            }

            if (!string.IsNullOrEmpty(userPoint) && distance.HasValue)
            {
                var allPoints = points.ToList();
                points = allPoints.Where(p => CalculateDistance(p.Coordinates, userPoint) <= distance.Value).AsQueryable();
            }

            if (types != null && types.Count > 0)
            {
                points = points.Where(p => types.Contains(p.TypeId));
            }


            points = points.Where(p => !p.StartTime.HasValue || !endTime.HasValue || p.StartTime <= endTime.Value);

            points = points.Where(p => !p.EndTime.HasValue || !startTime.HasValue || p.EndTime >= startTime.Value);

            var simpleEvents = points.Select(p => new EventDTO
            {
                Id = p.Id,
                Name = p.Name,
                Type = new TypeDTO
                {
                    TypeId = p.Type.Id,
                    TypeIcon = p.Type.ImageURL,
                    TypeName = p.Type.Name,
                },
                Coordinates = p.Coordinates,
                StartTime = p.StartTime,
                EndTime = p.EndTime
            });

            return simpleEvents.ToList();
            
        }
        private double CalculateDistance(string coordinates1, string coordinates2)
        {
            var (lat1, lon1) = ParseCoordinates(coordinates1);
            var (lat2, lon2) = ParseCoordinates(coordinates2);

            const double EarthRadius = 6371;

            var lat1Rad = DegreeToRadian(lat1);
            var lon1Rad = DegreeToRadian(lon1);
            var lat2Rad = DegreeToRadian(lat2);
            var lon2Rad = DegreeToRadian(lon2);

            var dLat = lat2Rad - lat1Rad;
            var dLon = lon2Rad - lon1Rad;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = EarthRadius * c;
            distance *= 1000;
            return distance;
        }


        private (double, double) ParseCoordinates(string coordinates)
            {

                coordinates = coordinates.Trim().Replace(" ", "");


                var parts = coordinates.Split(',');

                if (parts.Length == 2 &&
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                {
                    return (lat, lon);
                }

                throw new ArgumentException("Invalid coordinates format");
            }


            private double DegreeToRadian(double degree)
            {
                return degree * Math.PI / 180.0;
            }

        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutEvent(int id, Event myEvent)
        //{
        //    if (id != myEvent.Id)
        //    {
        //        return BadRequest();
        //    }
        //    else { }

        //    _context.Entry(myEvent).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if ((_context.Events?.Any(e => e.Id == id)).GetValueOrDefault())
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}
        [Authorize(Roles = ApplicationUserRoles.User + "," + ApplicationUserRoles.Admin)]
        //[Authorize(Roles = ApplicationUserRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEvent(int id, EventView viewEvent)
        {
            if (id != viewEvent.Id)
            {
                return BadRequest();
            }
            else { }

            var myEvent = await _context.Events.FindAsync(id);
            if(viewEvent.DataUrl != "")
            {
                _fileStorage.Delete(myEvent.ImageName);
                myEvent.ImageName = await _fileStorage.Upload(viewEvent.DataUrl);
            }
            myEvent.Name = viewEvent.Name;
            myEvent.TypeId = viewEvent.TypeId;
            myEvent.StartTime = viewEvent.StartTime;
            myEvent.EndTime = viewEvent.EndTime;
            myEvent.Description = viewEvent.Description;
            myEvent.Coordinates = viewEvent.Coordinates;
            _context.Entry(myEvent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if ((_context.Events?.Any(e => e.Id == id)).GetValueOrDefault())
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        [Authorize(Roles = ApplicationUserRoles.User + "," + ApplicationUserRoles.Admin)]
        [HttpPost]
            public async Task<ActionResult<Event>> PostEvent(EventView viewEvent)
            {
                if (_context.Events == null)
                {
                    return Problem("Entity set 'MapOfActivitiesAPIContext.Events'  is null.");
                }
             
                Event myEvent = new Event();
                myEvent.ImageName = await _fileStorage.Upload(viewEvent.DataUrl);
                myEvent.Name = viewEvent.Name;
                myEvent.TypeId = viewEvent.TypeId;
                myEvent.StartTime = viewEvent.StartTime;
                myEvent.EndTime = viewEvent.EndTime;
                myEvent.Description = viewEvent.Description;
                myEvent.Coordinates = viewEvent.Coordinates;

    
            var token = await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "access_token");
            Console.WriteLine( "token is " + token);
            var principal = _tokenService.GetPrincipalFromExpiredToken(token);
            string email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(c => c.Email == email);
            myEvent.User = user;
                
            _context.Events.Add(myEvent);
                await _context.SaveChangesAsync();

                return NoContent();
            }

        //[HttpGet("{fileName}")]
        //public async Task<ActionResult<string>> GetImage(string fileName)
        //{
        //    return await _fileStorage.Get(fileName); 
        //}
        [Authorize(Roles = ApplicationUserRoles.User + "," + ApplicationUserRoles.Admin)]
        [HttpDelete("{id}")]
           public async Task<IActionResult> DeleteEvent(int id)
            {
                if (_context.Events == null)
                {
                    return NotFound();
                }
                var myEvent = await _context.Events.FindAsync(id);
                if (myEvent == null)
                {
                    return NotFound();
                }
                _fileStorage.Delete(myEvent.ImageName);

                 var complaintsToDelete = _context.Complaints.Where(c => c.EventId == myEvent.Id);
                _context.Complaints.RemoveRange(complaintsToDelete);
    
                _context.Events.Remove(myEvent);
                await _context.SaveChangesAsync();

                return NoContent();
            }
        }
    }
