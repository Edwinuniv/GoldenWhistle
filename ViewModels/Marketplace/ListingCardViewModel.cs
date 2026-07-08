namespace GoldenWhistle.ViewModels.Marketplace
{
    public class ListingCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public double SellerRating { get; set; }
        public bool IsVerified { get; set; }
        public string? ImageUrl { get; set; }
    }
}
