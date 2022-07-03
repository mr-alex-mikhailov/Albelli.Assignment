using System;
using System.Collections.Generic;

namespace Albelli.Assignment.Application.DataContext.Entities
{
    public class Order
    {
        public Guid Id { get; set; }

        public decimal MinBinWidth { get; set; }

        public virtual List<OrderEntry> OrderEntries { get; set; }
    }
}
