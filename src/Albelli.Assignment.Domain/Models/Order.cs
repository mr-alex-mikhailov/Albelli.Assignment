using System;
using System.Collections.Generic;

namespace Albelli.Assignment.Domain.Models
{
    public class Order
    {
        public Guid OrderID { get; set; }

        public List<OrderEntry> OrderEntries { get; set; }
    }
}
