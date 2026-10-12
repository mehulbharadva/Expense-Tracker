using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? account)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var acc = (account.HasValue && Enum.IsDefined(typeof(AccountType), account.Value))
                ? (AccountType)account.Value
                : AccountType.Personal;

            // 1. Overall Totals for the selected AccountType
            var totalIncome = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var totalExpense = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var balance = totalIncome - totalExpense;

            // 2. Month-over-month trend data
            var today = DateTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = thisMonthStart.AddMonths(1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            var thisMonthIncome = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Income && t.TransactionDate >= thisMonthStart && t.TransactionDate < nextMonthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var lastMonthIncome = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Income && t.TransactionDate >= lastMonthStart && t.TransactionDate < thisMonthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var thisMonthExpense = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Expense && t.TransactionDate >= thisMonthStart && t.TransactionDate < nextMonthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var lastMonthExpense = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Expense && t.TransactionDate >= lastMonthStart && t.TransactionDate < thisMonthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            decimal incomeTrendPct = 0m;
            bool hasPriorIncome = false;
            if (lastMonthIncome > 0m)
            {
                incomeTrendPct = Math.Round(((thisMonthIncome - lastMonthIncome) / lastMonthIncome) * 100m, 1);
                hasPriorIncome = true;
            }

            decimal expenseTrendPct = 0m;
            bool hasPriorExpense = false;
            if (lastMonthExpense > 0m)
            {
                expenseTrendPct = Math.Round(((thisMonthExpense - lastMonthExpense) / lastMonthExpense) * 100m, 1);
                hasPriorExpense = true;
            }

            // 3. Monthly Income vs Expense (6-month rolling window)
            var latestTxDate = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => (DateTime?)t.TransactionDate)
                .FirstOrDefaultAsync();

            var refMonth = thisMonthStart;
            if (latestTxDate.HasValue)
            {
                var latestMonth = new DateTime(latestTxDate.Value.Year, latestTxDate.Value.Month, 1);
                if (latestMonth > refMonth || latestMonth < refMonth.AddMonths(-5))
                {
                    refMonth = latestMonth;
                }
            }

            var windowStart = refMonth.AddMonths(-5);
            var windowEnd = refMonth.AddMonths(1);

            var monthlyTxs = await _db.Transactions
                .Where(t => t.UserId == userId && t.AccountType == acc && t.TransactionDate >= windowStart && t.TransactionDate < windowEnd)
                .Select(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.Type, t.Amount })
                .ToListAsync();

            var monthlyData = new List<MonthlyChartItem>();
            for (int i = 5; i >= 0; i--)
            {
                var m = refMonth.AddMonths(-i);
                var inc = monthlyTxs
                    .Where(t => t.Year == m.Year && t.Month == m.Month && t.Type == TransactionType.Income)
                    .Sum(t => t.Amount);
                var exp = monthlyTxs
                    .Where(t => t.Year == m.Year && t.Month == m.Month && t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);

                monthlyData.Add(new MonthlyChartItem
                {
                    MonthLabel = m.ToString("MMM"),
                    Year = m.Year,
                    Month = m.Month,
                    Income = inc,
                    Expense = exp
                });
            }

            var maxMonthlyVal = monthlyData.Max(m => Math.Max(m.Income, m.Expense));
            decimal yCeiling = 3000m;
            bool hasMonthlyData = maxMonthlyVal > 0;
            if (hasMonthlyData)
            {
                double maxD = (double)maxMonthlyVal;
                double exp = Math.Floor(Math.Log10(maxD));
                double frac = maxD / Math.Pow(10, exp);
                double niceFrac;
                if (frac <= 1.0) niceFrac = 1.0;
                else if (frac <= 1.5) niceFrac = 1.5;
                else if (frac <= 2.0) niceFrac = 2.0;
                else if (frac <= 3.0) niceFrac = 3.0;
                else if (frac <= 5.0) niceFrac = 5.0;
                else if (frac <= 6.0) niceFrac = 6.0;
                else if (frac <= 8.0) niceFrac = 8.0;
                else niceFrac = 10.0;
                yCeiling = (decimal)(niceFrac * Math.Pow(10, exp));
                if (yCeiling < maxMonthlyVal) yCeiling = maxMonthlyVal * 1.1m;
            }

            foreach (var m in monthlyData)
            {
                var incH = yCeiling > 0 ? (int)Math.Round((m.Income / yCeiling) * 140m) : 0;
                if (m.Income > 0 && incH < 4) incH = 4;
                m.IncomeBarHeight = incH;

                var expH = yCeiling > 0 ? (int)Math.Round((m.Expense / yCeiling) * 140m) : 0;
                if (m.Expense > 0 && expH < 4) expH = 4;
                m.ExpenseBarHeight = expH;
            }

            // 4. Category-wise expense totals ("Where the money goes")
            var catExpensesRaw = await _db.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.AccountType == acc && t.Type == TransactionType.Expense)
                .GroupBy(t => new { t.CategoryId, CategoryName = t.Category != null ? t.Category.Name : "Other" })
                .Select(g => new
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(g => g.TotalAmount)
                .ToListAsync();

            var categoryColors = new[]
            {
                "#6E2237", // Wine
                "#B5872A", // Mustard / Gold
                "#3D7858", // Forest / Teal
                "#AC5F75", // Muted Rose
                "#D4A252", // Light Amber
                "#6F9D80", // Sage Green
                "#164E3A", // Deep Teal
                "#5A6B7C", // Warm Slate
                "#C86446", // Terracotta
                "#8E44AD"  // Plum
            };

            var categoryExpenses = new List<CategoryExpenseItem>();
            var catTotal = catExpensesRaw.Sum(c => c.TotalAmount);

            if (catExpensesRaw.Count > 6)
            {
                var top5 = catExpensesRaw.Take(5).ToList();
                var otherAmount = catExpensesRaw.Skip(5).Sum(c => c.TotalAmount);

                for (int i = 0; i < top5.Count; i++)
                {
                    var item = top5[i];
                    var pct = catTotal > 0 ? Math.Round((item.TotalAmount / catTotal) * 100m, 1) : 0m;
                    categoryExpenses.Add(new CategoryExpenseItem
                    {
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryName,
                        Amount = item.TotalAmount,
                        Percentage = pct,
                        Color = categoryColors[i % categoryColors.Length]
                    });
                }

                if (otherAmount > 0)
                {
                    var otherPct = catTotal > 0 ? Math.Round((otherAmount / catTotal) * 100m, 1) : 0m;
                    categoryExpenses.Add(new CategoryExpenseItem
                    {
                        CategoryId = 0,
                        CategoryName = "Other",
                        Amount = otherAmount,
                        Percentage = otherPct,
                        Color = categoryColors[5 % categoryColors.Length]
                    });
                }
            }
            else
            {
                for (int i = 0; i < catExpensesRaw.Count; i++)
                {
                    var item = catExpensesRaw[i];
                    var pct = catTotal > 0 ? Math.Round((item.TotalAmount / catTotal) * 100m, 1) : 0m;
                    categoryExpenses.Add(new CategoryExpenseItem
                    {
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryName,
                        Amount = item.TotalAmount,
                        Percentage = pct,
                        Color = categoryColors[i % categoryColors.Length]
                    });
                }
            }

            // Calculate SVG segment lengths and offsets for the donut chart (Circumference = 2 * PI * 62 = ~389.5575)
            const double Circumference = 2 * Math.PI * 62;
            double accumulated = 0;
            foreach (var item in categoryExpenses)
            {
                double segment = catTotal > 0 ? ((double)item.Amount / (double)catTotal) * Circumference : 0;
                item.SegmentLength = segment;
                item.StrokeOffset = -accumulated;
                accumulated += segment;
            }

            // 5. Recent Transactions
            var recent = await _db.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.AccountType == acc)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Take(5)
                .ToListAsync();

            // Populate ViewData for backward compatibility
            ViewData["TotalIncome"] = totalIncome;
            ViewData["TotalExpense"] = totalExpense;
            ViewData["Balance"] = balance;
            ViewData["RecentTransactions"] = recent;
            ViewData["SelectedAccount"] = acc;

            var model = new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = balance,
                SelectedAccount = acc,
                ThisMonthIncome = thisMonthIncome,
                LastMonthIncome = lastMonthIncome,
                IncomeTrendPct = incomeTrendPct,
                HasPriorIncome = hasPriorIncome,
                ThisMonthExpense = thisMonthExpense,
                LastMonthExpense = lastMonthExpense,
                ExpenseTrendPct = expenseTrendPct,
                HasPriorExpense = hasPriorExpense,
                YCeiling = yCeiling,
                HasMonthlyData = hasMonthlyData,
                MonthlyData = monthlyData,
                CategoryExpenses = categoryExpenses,
                RecentTransactions = recent
            };

            return View(model);
        }
    }
}
