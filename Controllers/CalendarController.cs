using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Camstep.Data;
using Camstep.Models;
using Camstep.Services;

namespace Camstep.Controllers;

public class CalendarController : AppController
{
    private readonly AppDbContext _db;

    public CalendarController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? year, int? month, int? day, string? show, string? view)
    {
        var userId = CurrentUserId;
        var today = DateTime.Today;

        int selectedYear = year ?? today.Year;
        int selectedMonth = month ?? today.Month;

        if (selectedMonth < 1 || selectedMonth > 12 || selectedYear < 2000 || selectedYear > 2100)
        {
            selectedMonth = today.Month;
            selectedYear = today.Year;
        }

        var currentMonth = new DateTime(selectedYear, selectedMonth, 1);
        int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);

        int selectedDay = day ?? (
            selectedYear == today.Year && selectedMonth == today.Month
                ? today.Day
                : 1
        );

        if (selectedDay < 1 || selectedDay > daysInMonth)
            selectedDay = 1;

        var selectedDate = new DateTime(selectedYear, selectedMonth, selectedDay);
        var weekStart = selectedDate.AddDays(-(((int)selectedDate.DayOfWeek + 6) % 7));

        var rangeStart = weekStart < currentMonth ? weekStart : currentMonth;
        var monthEnd = currentMonth.AddMonths(1);
        var rangeEnd = weekStart.AddDays(7) > monthEnd ? weekStart.AddDays(7) : monthEnd;
        var upcomingEnd = today.AddDays(8);

        var tasks = await _db.Tasks
            .Where(t => t.UserId == userId && t.DueDate >= rangeStart && t.DueDate < rangeEnd)
            .ToListAsync();

        var events = await _db.CalendarEvents
            .Where(e => e.UserId == userId && e.StartsAt >= rangeStart && e.StartsAt < rangeEnd)
            .ToListAsync();

        var upcomingTasks = await _db.Tasks
            .Where(t => t.UserId == userId && !t.IsCompleted && t.DueDate < upcomingEnd)
            .ToListAsync();

        var upcomingEvents = await _db.CalendarEvents
            .Where(e => e.UserId == userId && e.StartsAt >= today && e.StartsAt < upcomingEnd)
            .ToListAsync();

        var upcoming = upcomingTasks
            .Select(t => new AgendaItem
            {
                When = t.DueDate,
                Title = t.Title,
                Kind = "task",
                Detail = "Tarea · " + t.Priority,
                Overdue = t.IsOverdue(today)
            })
            .Concat(upcomingEvents.Select(e => new AgendaItem
            {
                When = e.StartsAt,
                Title = e.Title,
                Kind = "event",
                Detail = e.AllDay ? "Evento · Todo el día" : "Evento · " + Format.Time(e.StartsAt)
            }))
            .OrderBy(a => a.When)
            .Take(12)
            .ToList();

        var model = new CalendarViewModel
        {
            CurrentMonth = currentMonth,
            Today = today,
            SelectedDate = selectedDate,
            View = view is "week" or "day" ? view : "month",
            WeekStart = weekStart,
            FirstDayOffset = ((int)currentMonth.DayOfWeek + 6) % 7,
            DaysInMonth = daysInMonth,
            Show = show is "tasks" or "events" ? show : "all",
            Tasks = tasks,
            Events = events,
            Upcoming = upcoming
        };

        return View(model);
    }
}
