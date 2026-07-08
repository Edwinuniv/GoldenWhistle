using System;

namespace GoldenWhistle.Models
{
    public class PubLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public bool IsOpen { get; set; }
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public int Screens { get; set; }
        public bool FreeEntry { get; set; }
        public bool HdScreens { get; set; }
        public bool IsApproved { get; set; }
        public string? ImageUrl { get; set; }
    }
}