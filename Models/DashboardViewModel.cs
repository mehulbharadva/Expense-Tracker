namespace ExpenseTracker.Models
{
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance { get; set; }

        public AccountType SelectedAccount { get; set; }

        public decimal ThisMonthIncome { get; set; }
        public decimal LastMonthIncome { get; set; }
        public decimal IncomeTrendPct { get; set; }
        public bool HasPriorIncome { get; set; }

        public decimal ThisMonthExpense { get; set; }
        public decimal LastMonthExpense { get; set; }
        public decimal ExpenseTrendPct { get; set; }
        public bool HasPriorExpense { get; set; }

        public decimal YCeiling { get; set; } = 3000m;
        public bool HasMonthlyData { get; set; }

        public List<MonthlyChartItem> MonthlyData { get; set; } = new();
        public List<CategoryExpenseItem> CategoryExpenses { get; set; } = new();
        public List<Transaction> RecentTransactions { get; set; } = new();

        public static string FormatShort(decimal val)
        {
            if (val <= 0) return "₹0";
            if (val >= 10000000) return $"₹{(val / 10000000m):0.#}Cr";
            if (val >= 100000) return $"₹{(val / 100000m):0.#}L";
            if (val >= 1000) return $"₹{(val / 1000m):0.#}k";
            return $"₹{val:N0}";
        }
    }

    public class MonthlyChartItem
    {
        public string MonthLabel { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public int IncomeBarHeight { get; set; }
        public int ExpenseBarHeight { get; set; }
    }

    public class CategoryExpenseItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
        public double SegmentLength { get; set; }
        public double StrokeOffset { get; set; }
    }
}
