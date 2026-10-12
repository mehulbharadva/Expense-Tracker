using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Reports
        public async Task<IActionResult> Index(int? account, string? period)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var (acc, validPeriod, periodLabel, startDate, endDate) = ParseFilters(account, period);

            var query = _db.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.AccountType == acc);

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var viewModel = new ReportsViewModel
            {
                SelectedAccount = acc,
                SelectedPeriod = validPeriod,
                PeriodLabel = periodLabel,
                StartDate = startDate,
                EndDate = endDate,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Transactions = transactions
            };

            return View(viewModel);
        }

        // GET: /Reports/DownloadCsv
        public async Task<IActionResult> DownloadCsv(int? account, string? period)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var (acc, validPeriod, periodLabel, startDate, _) = ParseFilters(account, period);

            var query = _db.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.AccountType == acc);

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var csvBuilder = new StringBuilder();
            // RFC 4180 header row
            csvBuilder.AppendLine("Date,Type,Category,Description,Amount");

            foreach (var t in transactions)
            {
                var date = t.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var type = t.Type.ToString();
                var category = EscapeCsv(t.Category?.Name ?? "Uncategorized");
                var description = EscapeCsv(t.Description ?? string.Empty);
                var amount = t.Amount.ToString("F2", CultureInfo.InvariantCulture);

                csvBuilder.AppendLine($"{date},{type},{category},{description},{amount}");
            }

            var periodSuffix = validPeriod switch
            {
                "1m" => "1Month",
                "2m" => "2Months",
                "3m" => "3Months",
                "6m" => "6Months",
                "all" => "AllHistory",
                _ => "Report"
            };

            var fileName = $"Transaction_Report_{acc}_{periodSuffix}.csv";
            var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static (AccountType acc, string period, string periodLabel, DateTime? startDate, DateTime endDate) ParseFilters(int? account, string? period)
        {
            var acc = (account.HasValue && Enum.IsDefined(typeof(AccountType), account.Value))
                ? (AccountType)account.Value
                : AccountType.Personal;

            var rawPeriod = (period ?? "1m").ToLowerInvariant().Trim();
            var today = DateTime.Today;
            DateTime? startDate;
            string periodLabel;
            string validPeriod;

            switch (rawPeriod)
            {
                case "2m":
                    validPeriod = "2m";
                    startDate = today.AddMonths(-2);
                    periodLabel = "Last 2 Months";
                    break;
                case "3m":
                    validPeriod = "3m";
                    startDate = today.AddMonths(-3);
                    periodLabel = "Last 3 Months";
                    break;
                case "6m":
                    validPeriod = "6m";
                    startDate = today.AddMonths(-6);
                    periodLabel = "Last 6 Months";
                    break;
                case "all":
                    validPeriod = "all";
                    startDate = null;
                    periodLabel = "All History";
                    break;
                case "1m":
                default:
                    validPeriod = "1m";
                    startDate = today.AddMonths(-1);
                    periodLabel = "Last 1 Month";
                    break;
            }

            return (acc, validPeriod, periodLabel, startDate, today);
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            // If the field contains comma, quote, or newline, escape quotes with double quotes and wrap in quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}
