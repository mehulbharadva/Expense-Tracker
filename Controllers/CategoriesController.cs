using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var categories = await _db.Categories
                .Where(c => c.IsDefault || c.UserId == userId)
                .OrderBy(c => c.AccountType).ThenBy(c => c.Type).ThenBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,AccountType")] Category category)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!ModelState.IsValid) return View(category);

            // Prevent duplicate custom categories for the same user/account/type
            var exists = await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == category.Name && c.Type == category.Type && c.AccountType == category.AccountType);
            if (exists)
            {
                ModelState.AddModelError(string.Empty, "You already have a category with the same name, type and account.");
                return View(category);
            }

            category.UserId = userId;
            category.IsDefault = false;
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();
            if (category.IsDefault || category.UserId != userId) return Forbid();
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Type,AccountType")] Category category)
        {
            if (id != category.Id) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!ModelState.IsValid) return View(category);

            var existing = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return NotFound();
            if (existing.IsDefault || existing.UserId != userId) return Forbid();

            // Prevent duplicate name for this user/account/type
            var duplicate = await _db.Categories.AnyAsync(c => c.Id != id && c.UserId == userId && c.Name == category.Name && c.Type == category.Type && c.AccountType == category.AccountType);
            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "You already have a category with the same name, type and account.");
                return View(category);
            }

            category.UserId = userId;
            category.IsDefault = false;

            _db.Update(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();
            if (category.IsDefault || category.UserId != userId) return Forbid();
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();
            if (category.IsDefault || category.UserId != userId) return Forbid();

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
