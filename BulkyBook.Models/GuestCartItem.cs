using System;

namespace BulkyBook.Models
{
    public class GuestCartItem
    {
        public int ProductId { get; set; }
        public int Count { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}

