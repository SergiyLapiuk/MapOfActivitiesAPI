using MapOfActivitiesAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Type = MapOfActivitiesAPI.Models.Type;

namespace MapOfActivitiesAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TypesController : ControllerBase
    {
        private MapOfActivitiesAPIContext _context;

        public TypesController(MapOfActivitiesAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IEnumerable<Type> Get()
        {
            return _context.Types.ToList();
        }

        [HttpGet("{id}")]
        public ActionResult<Type> Get(int id)
        {
            var type = _context.Types.FirstOrDefault(x => x.Id == id);

            if (type == null)
            {
                return NotFound();
            }

            return type;
        }

        [HttpGet("GetAllTypesAsTree")]
        public ActionResult<IEnumerable<Type>> GetAllTypesAsTree()
        {
            var types = _context.Types.ToList();
            var rootTypes = types.Where(t => t.ParentTypeId == 0).ToList();

            foreach (var rootType in rootTypes)
            {
                AddChildren(rootType, types);
            }

            return rootTypes;
        }

        [Authorize(Roles = ApplicationUserRoles.Admin)]
        private void AddChildren(Type parentType, List<Type> allTypes)
        {
            parentType.Children = allTypes.Where(t => t.ParentTypeId == parentType.Id).ToList();

            foreach (var child in parentType.Children)
            {
                AddChildren(child, allTypes);
            }
        }


        [Authorize(Roles = ApplicationUserRoles.Admin)]
        [HttpPost]
        public ActionResult<Type> Post([FromBody] Type type)
        {
            if (type == null)
            {
                return BadRequest("Type object is null");
            }

            _context.Types.Add(type);
            _context.SaveChanges();

            return CreatedAtAction("Get", new { id = type.Id }, type);
        }
        [HttpPost("CreateType")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public ActionResult<Type> CreateType(string name, int parentTypeId)
        {
            var type = new Type { Name = name, ParentTypeId = parentTypeId };
            _context.Types.Add(type);
            _context.SaveChanges();

            return CreatedAtAction("Get", new { id = type.Id }, type);
        }

        [HttpPut("EditType")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public IActionResult EditType(int id, string name, string imageURL)
        {
            var type = _context.Types.Find(id);

            if (type == null)
            {
                return NotFound();
            }

            type.Name = name;
            type.ImageURL = imageURL;

            _context.Types.Update(type);
            _context.SaveChanges();

            return NoContent();
        }
        [HttpPut("{id}")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public async Task<ActionResult<Type>> PutEvent(int id, Type type)
        {
            if (id != type.Id)
            {
                return BadRequest("Type ID mismatch");
            }

            _context.Entry(type).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Types.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return type; 
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = ApplicationUserRoles.Admin)]
        public IActionResult Delete(int id)
        {
            var type = _context.Types.Find(id);

            if (type == null)
            {
                return NotFound();
            }

            _context.Types.Remove(type);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
