using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using MediatR;
using Albelli.Assignment.Application.DataContext;
using Albelli.Assignment.Application.DataContext.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Albelli.Assignment.Application.Features
{
    public static class CreateOrder
    {
        public class Request : IRequest<Response>
        {
            public Guid OrderID { get; set; }

            public List<Domain.Models.OrderEntry> OrderEntries { get; set; }
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
                // Checking if the Order with supplied ID already exists in the database
                var dbExistingOrder = await dbContext.Orders
                    .Where(p => p.Id == request.OrderID)
                    .SingleOrDefaultAsync(token);
                if (dbExistingOrder != null)
                    throw new InvalidOperationException($"Order with ID {request.OrderID} already exists");

                // Mapping product type codes to ProductType entries
                var dbProductTypes = await dbContext.ProductTypes.ToArrayAsync(token);
                var productTypesMap = request.OrderEntries
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

                var dbOrderEntries = request.OrderEntries
                    .Select(p => new OrderEntry
                    {
                        Id = Guid.NewGuid(),
                        OrderId = request.OrderID,
                        ProductType = productTypesMap[p.ProductType],
                        Quantity = p.Quantity
                    });

                var binWidth = request.OrderEntries
                    .Select(p => CalculateBinWidth(productTypesMap[p.ProductType], p.Quantity))
                    .Sum();

                var dbOrder = new Order
                {
                    Id = request.OrderID,
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

            private decimal CalculateBinWidth(ProductType productType, int quantity)
            {
                return productType.WidthInBin * (((quantity - 1) / productType.MaxAmountInGroup) + 1);
            }
        }
    }
}
