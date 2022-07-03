using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediatR;
using Albelli.Assignment.Application.Features;
using Albelli.Assignment.API.Models;
using Albelli.Assignment.Domain.Models;

namespace Albelli.Assignment.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<OrdersController> logger;

        public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        {
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("FindById/{id}")]
        public Task<OrderFull> FindByIdAsync(Guid id, CancellationToken token = default)
        {
            var query = new FindOrder.Request
            {
                OrderID = id
            };

            return mediator.Send(query, token);
        }

        [HttpPost("Create")]
        public async Task<OrderCreatedResult> CreateAsync(Order order, CancellationToken token = default)
        {
            var request = new CreateOrder.Request
            {
                Order = order
            };
            var response = await mediator.Send(request, token);

            return new OrderCreatedResult
            {
                MinBinWidth = response.MinBinWidth
            };
        }
    }
}
