using System;
using System.Collections.Generic;

namespace ExpenseTracker.Models
{
    public class ReportsViewModel
    {
        public AccountType SelectedAccount { get; set; } = AccountType.Personal;
        public string SelectedPeriod { get; set; } = "1m";
        public string PeriodLabel { get; set; } = "Last 1 Month";
        public DateTime? StartDate { get; set; }
        public DateTime EndDate { get; set; } = DateTime.Today;

        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;

        public List<Transaction> Transactions { get; set; } = new();

        public string AccountName => SelectedAccount == AccountType.Personal ? "Personal" : "Business";
    }
}
