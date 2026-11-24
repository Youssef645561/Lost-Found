using System.Collections.Generic;
using System.Threading.Tasks;
using LostFound.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostFound.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            List<clsCategory> lstCategories = await clsCategory.GetAll();
            return Ok(lstCategories);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategory(int id)
        {
            clsCategory? category = await clsCategory.Find(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] clsCategory category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name))
                return BadRequest(new { message = "Category name is required." });

            clsCategory newCategory = new clsCategory
            {
                Name = category.Name.Trim()
            };

            bool IsSaved = await newCategory.Save();
            if (!IsSaved)
                return BadRequest(new { message = "Could not create category." });

            return CreatedAtAction(nameof(GetCategory), new { id = newCategory.CategoryID }, newCategory);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] clsCategory category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.Name))
                return BadRequest(new { message = "Category name is required." });

            clsCategory? existingCategory = await clsCategory.Find(id);
            if (existingCategory == null)
                return NotFound();

            existingCategory.Name = category.Name.Trim();
            bool IsSaved = await existingCategory.Save();
            if (!IsSaved)
                return BadRequest(new { message = "Could not update category." });

            return Ok(existingCategory);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            clsCategory? existingCategory = await clsCategory.Find(id);
            if (existingCategory == null)
                return NotFound();

            bool IsDeleted = await existingCategory.Delete();
            if (!IsDeleted)
                return BadRequest(new { message = "Could not delete category." });

            return NoContent();
        }
    }
}
