using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Albelli.Assignment.Application.DataContext.Entities
{
    public class OrderEntry
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; }

        public int ProductTypeId { get; set; }

        [ForeignKey(nameof(ProductTypeId))]
        public ProductType ProductType { get; set; }

        public int Quantity { get; set; }
    }
}
