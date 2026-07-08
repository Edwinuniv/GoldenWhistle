using System;

namespace GoldenWhistle.Models
{
    public class JerseyListing
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }

        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser Seller { get; set; } = null!;
        public double SellerRating { get; set; }
    }
}