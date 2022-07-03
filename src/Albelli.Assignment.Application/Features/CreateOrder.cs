using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Albelli.Assignment.Domain.Models;
using Albelli.Assignment.Application.DataContext;
using DBE = Albelli.Assignment.Application.DataContext.Entities;

namespace Albelli.Assignment.Application.Features
{
    public static class CreateOrder
    {
        public class Request : IRequest<Response>
        {
            public Order Order { get; set; }
        }

        public class Response
        {
            public decimal MinBinWidth { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly ApplicationDataContext dbContext;
            private readonly ILogger<CreateOrder.Handler> logger;

            public Handler(ApplicationDataContext dataContext, ILogger<CreateOrder.Handler> logger)
            {
                this.dbContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
                this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task<Response> Handle(Request request, CancellationToken token = default)
            {
                if (request.Order?.OrderEntries == null || !request.Order.OrderEntries.Any())
                    throw new InvalidOperationException("Order does not contain any entries");

                var order = request.Order;

                // Checking if the Order with supplied ID already exists in the database
                var dbExistingOrder = await dbContext.Orders
                    .Where(p => p.Id == order.OrderID)
                    .SingleOrDefaultAsync(token);
                if (dbExistingOrder != null)
                    throw new InvalidOperationException($"Order with ID {order.OrderID} already exists");

                // Mapping product type codes to ProductType entries
                var dbProductTypes = await dbContext.ProductTypes.ToArrayAsync(token);
                var productTypesMap = order.OrderEntries
                    .Select(p => new
                    {
                        Key = p.ProductType,
                        Value = dbProductTypes.SingleOrDefault(pt => pt.Code == p.ProductType)
                    })
                    .ToDictionary(p => p.Key, p => p.Value);

                // Checking if all ProductType entries exist in database
                var notFoundProductCodes = productTypesMap
                    .Where(p => p.Value == null)
                    .Select(p => p.Key);
                if (notFoundProductCodes.Any())
                    throw new InvalidOperationException($"Product codes ({string.Join(",", notFoundProductCodes)}) not found");

                var dbOrderEntries = order.OrderEntries
                    .Select(p => new DBE.OrderEntry
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.OrderID,
                        ProductType = productTypesMap[p.ProductType],
                        Quantity = p.Quantity
                    });

                var binWidth = order.OrderEntries
                    .Select(p => CalculateBinWidth(productTypesMap[p.ProductType], p.Quantity))
                    .Sum();

                var dbOrder = new DBE.Order
                {
                    Id = order.OrderID,
                    OrderEntries = new List<DataContext.Entities.OrderEntry>(dbOrderEntries),
                    MinBinWidth = binWidth
                };
                await dbContext.AddAsync(dbOrder, token);

                await dbContext.SaveChangesAsync(token);

                return new Response
                {
                    MinBinWidth = binWidth
                };
            }

            private decimal CalculateBinWidth(DBE.ProductType productType, int quantity)
            {
                return productType.WidthInBin * (((quantity - 1) / productType.MaxAmountInGroup) + 1);
            }
        }
    }
}
