using System.Collections.Generic;
using System.Threading.Tasks;
using LostFound.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostFound.API.Controllers
{
    [ApiController]
    [Route("api/locations")]
    public class LocationsController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllLocations()
        {
            List<clsLocation> lstLocations = await clsLocation.GetAll();
            return Ok(lstLocations);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLocation(int id)
        {
            clsLocation? location = await clsLocation.Find(id);
            if (location == null)
                return NotFound();

            return Ok(location);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateLocation([FromBody] clsLocation location)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.Name))
                return BadRequest(new { message = "Location name is required." });

            clsLocation newLocation = new clsLocation
            {
                Name = location.Name.Trim()
            };

            bool IsSaved = await newLocation.Save();
            if (!IsSaved)
                return BadRequest(new { message = "Could not create location." });

            return CreatedAtAction(nameof(GetLocation), new { id = newLocation.LocationID }, newLocation);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] clsLocation location)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.Name))
                return BadRequest(new { message = "Location name is required." });

            clsLocation? existingLocation = await clsLocation.Find(id);
            if (existingLocation == null)
                return NotFound();

            existingLocation.Name = location.Name.Trim();
            bool IsSaved = await existingLocation.Save();
            if (!IsSaved)
                return BadRequest(new { message = "Could not update location." });

            return Ok(existingLocation);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            clsLocation? existingLocation = await clsLocation.Find(id);
            if (existingLocation == null)
                return NotFound();

            bool IsDeleted = await existingLocation.Delete();
            if (!IsDeleted)
                return BadRequest(new { message = "Could not delete location." });

            return NoContent();
        }
    }
}
