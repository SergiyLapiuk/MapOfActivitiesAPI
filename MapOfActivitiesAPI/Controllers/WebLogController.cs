using MapOfActivitiesAPI.Interfaces;
using MapOfActivitiesAPI.Models;
using MapOfActivitiesAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapOfActivitiesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebLogController : Controller
    {
        private MapOfActivitiesAPIContext _context;

        public WebLogController(MapOfActivitiesAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("all-web-logs")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public IEnumerable<WebLog> Get()
        {
            return _context.WebLogs.OrderByDescending(log => log.Time);
        }
    }
}
