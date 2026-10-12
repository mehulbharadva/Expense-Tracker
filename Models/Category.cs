using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseTracker.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public AccountType AccountType { get; set; }

        // If null => system/default category; otherwise belongs to the specific user
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
