using System.Collections.Generic;

namespace Albelli.Assignment.Application.DataContext.Entities
{
    public class ProductType
    {
        public int Id { get; set; }

        public string Code { get; set; }

        //public string Name { get; set; }

        public decimal WidthInBin { get; set; }

        public int MaxAmountInGroup { get; set; }

        public virtual List<OrderEntry> OrderEntries { get; set; }
    }
}
