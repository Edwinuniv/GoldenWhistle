using System;

namespace GoldenWhistle.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public int ListingId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        public ApplicationUser Sender { get; set; } = null!;
        public JerseyListing Listing { get; set; } = null!;
    }
}