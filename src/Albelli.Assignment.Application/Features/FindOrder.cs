using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Albelli.Assignment.Domain.Models;
using Albelli.Assignment.Application.DataContext;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Albelli.Assignment.Application.Features
{
    public static class FindOrder
    {
        public class Request : IRequest<OrderFull>
        {
            public Guid OrderID { get; set; }
        }

        public class Handler : IRequestHandler<Request, OrderFull>
        {
            private readonly ApplicationDataContext dbContext;
            private readonly ILogger<CreateOrder.Handler> logger;

            public Handler(ApplicationDataContext dataContext, ILogger<CreateOrder.Handler> logger)
            {
                this.dbContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
                this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task<OrderFull> Handle(Request request, CancellationToken token = default)
            {
                var dbOrderFound = await dbContext.Orders
                    .Where(p => p.Id == request.OrderID)
                    .SingleOrDefaultAsync(token);
                if (dbOrderFound == null)
                    throw new InvalidOperationException($"Order with ID {request.OrderID} not found");

                var dbOrderEntriesFound = await dbContext.OrderEntries
                    .Where(p => p.OrderId == request.OrderID)
                    .Include(p => p.ProductType)
                    .Select(p => new OrderEntry
                    {
                        ProductType = p.ProductType.Code,
                        Quantity = p.Quantity
                    })
                    .ToListAsync(token);

                var result = new OrderFull
                {
                    OrderID = dbOrderFound.Id,
                    MinBinWidth = dbOrderFound.MinBinWidth,
                    OrderEntries = dbOrderEntriesFound
                };

                return result;
            }
        }
    }
}
