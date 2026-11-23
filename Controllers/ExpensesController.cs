using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class ExpensesController : AppController
{
    private readonly AppDbContext _db;
    public ExpensesController(AppDbContext db) => _db = db;

    public IActionResult Index() => RedirectToAction("Index", "Finance", new { type = "expense" });

    public async Task<IActionResult> Create(string? returnUrl)
    {
        await LoadCategories();
        ViewBag.ReturnUrl = returnUrl;
        return View(new Expense());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Expense item, string? returnUrl)
    {
        await Validate(item, null);
        if (!ModelState.IsValid)
        {
            await LoadCategories();
            ViewBag.ReturnUrl = returnUrl;
            return View(item);
        }

        _db.Expenses.Add(new Expense
        {
            UserId = CurrentUserId,
            Description = item.Description.Trim(),
            Amount = item.Amount,
            Category = item.Category,
            Date = item.Date.Date
        });
        await _db.SaveChangesAsync();

        TempData["Notice"] = "Gasto registrado.";
        return RedirectBack(returnUrl, "Index", "Finance");
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var item = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        if (item == null) return NotFound();

        await LoadCategories(item.Category);
        ViewBag.ReturnUrl = returnUrl;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Expense item, string? returnUrl)
    {
        var existing = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == item.Id && x.UserId == CurrentUserId);
        if (existing == null) return NotFound();

        await Validate(item, existing.Category);
        if (!ModelState.IsValid)
        {
            await LoadCategories(existing.Category);
            ViewBag.ReturnUrl = returnUrl;
            return View(item);
        }

        existing.Description = item.Description.Trim();
        existing.Amount = item.Amount;
        existing.Category = item.Category;
        existing.Date = item.Date.Date;

        await _db.SaveChangesAsync();

        TempData["Notice"] = "Gasto actualizado.";
        return RedirectBack(returnUrl, "Index", "Finance");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var item = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);

        if (item != null)
        {
            _db.Expenses.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Notice"] = "Gasto eliminado.";
        }

        return RedirectBack(returnUrl, "Index", "Finance");
    }

    private async Task LoadCategories(string? keep = null)
    {
        var list = await Categories.ExpenseAsync(_db, CurrentUserId);
        if (keep != null && !list.Contains(keep)) list.Insert(0, keep);
        ViewBag.Categories = list;
    }

    private async Task Validate(Expense item, string? currentCategory)
    {
        var allowed = await Categories.ExpenseAsync(_db, CurrentUserId);
        if (!allowed.Contains(item.Category) && item.Category != currentCategory)
            ModelState.AddModelError(nameof(item.Category), "Elige una categoría de la lista.");

        if (item.Date.Year < 2000 || item.Date.Year > 2100)
            ModelState.AddModelError(nameof(item.Date), "Elige una fecha entre los años 2000 y 2100.");
    }
}
