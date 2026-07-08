namespace GoldenWhistle.ViewModels.Marketplace
{
    public class MarketplaceViewModel
    {
        public int TotalListings { get; set; }
        public List<ListingCardViewModel> Listings { get; set; } = new();
    }
}
