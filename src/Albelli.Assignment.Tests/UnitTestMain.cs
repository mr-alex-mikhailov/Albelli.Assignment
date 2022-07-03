using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Albelli.Assignment.Domain.Models;
using Albelli.Assignment.Application.Features;
using Albelli.Assignment.Application.DataContext;
using DBE = Albelli.Assignment.Application.DataContext.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;

namespace Albelli.Assignment.Tests
{
    public class UnitTestMain : IDisposable
    {
        private readonly Random random = new Random(DateTime.Now.Millisecond);
        private readonly ApplicationDataContext dbContext;
        private readonly DBE.ProductType[] dbProductTypes;

        private readonly Guid OrderIdNumber1 = Guid.NewGuid();
        private readonly Guid OrderIdNumber2 = Guid.NewGuid();


        public UnitTestMain()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDataContext>();
            optionsBuilder.UseInMemoryDatabase("Test");

            dbContext = new ApplicationDataContext(optionsBuilder.Options);

            dbProductTypes = dbContext.ProductTypes.ToArray();

            AddInitOrders();
        }

        [Fact]
        public async Task Test_CreateOrderHandler_ThrowWhenExists()
        {
            var request = GenerateNewOrderRequest(OrderIdNumber1, 3);
            var requestHandler = new CreateOrder.Handler(dbContext, NullLogger<CreateOrder.Handler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await requestHandler.Handle(request));
        }

        [Fact]
        public async Task Test_CreateOrderHandler_EqualWithDbData()
        {
            var orderIdGenerated = Guid.NewGuid();
            var request = GenerateNewOrderRequest(orderIdGenerated, 5);
            var requestHandler = new CreateOrder.Handler(dbContext, NullLogger<CreateOrder.Handler>.Instance);
            var response = await requestHandler.Handle(request);

            var dbOrderActual = await dbContext.Orders
                .Where(p => p.Id == orderIdGenerated)
                .SingleOrDefaultAsync();
            var dbOrderEntriesActual = await dbContext.OrderEntries
                .Where(p => p.OrderId == orderIdGenerated)
                .Select(p => new OrderEntry
                {
                    ProductType = p.ProductType.Code,
                    Quantity = p.Quantity
                })
                .ToArrayAsync();

            var expected = request.Order.OrderEntries
                .OrderBy(p => p.ProductType)
                .Select(p => $"{p.ProductType}:{p.Quantity}");
            var actual = dbOrderEntriesActual
                .OrderBy(p => p.ProductType)
                .Select(p => $"{p.ProductType}:{p.Quantity}");
            Assert.NotNull(dbOrderActual);
            Assert.Equal(request.Order.OrderID, dbOrderActual.Id);
            Assert.Equal(request.Order.OrderEntries.Sum(p => CalculateBinWidth(x => x.Code == p.ProductType, p.Quantity)), dbOrderActual.MinBinWidth);
            Assert.Equal(expected, actual);
        }

        private void AddInitOrders()
        {
            var entries1 = new List<DBE.OrderEntry>()
            {
                new DBE.OrderEntry { Id = Guid.NewGuid(), ProductTypeId = 1, Quantity = 3 }
            };
            var order1 = new DBE.Order
            {
                Id = OrderIdNumber1,
                OrderEntries = entries1,
                MinBinWidth = entries1.Select(p => CalculateBinWidth(x => x.Id == p.ProductTypeId, p.Quantity)).Sum()
            };

            var entries2 = new List<DBE.OrderEntry>()
            {
                new DBE.OrderEntry { Id = Guid.NewGuid(), ProductTypeId = 1, Quantity = 6 },
                new DBE.OrderEntry { Id = Guid.NewGuid(), ProductTypeId = 2, Quantity = 5 },
                new DBE.OrderEntry { Id = Guid.NewGuid(), ProductTypeId = 3, Quantity = 7 },
                new DBE.OrderEntry { Id = Guid.NewGuid(), ProductTypeId = 4, Quantity = 8 },
                new DBE.OrderEntry { Id = Guid.NewGuid(), ProductTypeId = 5, Quantity = 9 }
            };
            var order2 = new DBE.Order
            {
                Id = OrderIdNumber2,
                OrderEntries = entries2,
                MinBinWidth = entries2.Select(p => CalculateBinWidth(x => x.Id == p.ProductTypeId, p.Quantity)).Sum()
            };

            dbContext.AddRange(order1, order2);

            dbContext.SaveChanges();
        }

        private CreateOrder.Request GenerateNewOrderRequest(Guid orderId, int productCount)
        {
            var productCodes = dbProductTypes.Select(p => p.Code).ToList();
            if (productCodes.Count > productCount)
                productCount = productCodes.Count;

            var orderEntries = new List<OrderEntry>();
            for (var i = 0; i < productCount; i++)
            {
                var index = random.Next(productCodes.Count);
                var orderEntry = new OrderEntry
                {
                    ProductType = productCodes[index],
                    Quantity = random.Next(1, 51)
                };
                orderEntries.Add(orderEntry);

                productCodes.RemoveAt(index);
            }

            return new CreateOrder.Request
            {
                Order = new Order
                {
                    OrderID = orderId,
                    OrderEntries = orderEntries
                }
            };
        }

        private decimal CalculateBinWidth(Func<DBE.ProductType, bool> dbProductTypePredicate, int quantity)
        {
            var dbProductType = dbProductTypes.Single(dbProductTypePredicate);
            return dbProductType.WidthInBin * (((quantity - 1) / dbProductType.MaxAmountInGroup) + 1);
        }

        public void Dispose()
        {
            dbContext?.Dispose();
        }
    }
}
