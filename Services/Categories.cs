using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;

namespace Camstep.Services;

public static class Categories
{
    public static async Task<List<string>> ExpenseAsync(AppDbContext db, int userId) =>
        await MergeAsync(db, userId, "expense", Expense.Categories);

    public static async Task<List<string>> IncomeAsync(AppDbContext db, int userId) =>
        await MergeAsync(db, userId, "income", Income.Categories);

    private static async Task<List<string>> MergeAsync(AppDbContext db, int userId, string kind, string[] defaults)
    {
        var custom = await db.UserCategories
            .Where(c => c.UserId == userId && c.Kind == kind)
            .Select(c => c.Name)
            .ToListAsync();

        return defaults.Where(d => d != "Otros")
            .Concat(custom.OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase))
            .Append("Otros")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
