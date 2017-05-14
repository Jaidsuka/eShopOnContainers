﻿namespace Ordering.API.Application.DomainEventHandlers.OrderStartedEvent
{
    using MediatR;
    using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
    using Microsoft.Extensions.Logging;
    using Domain.Events;
    using System;
    using System.Threading.Tasks;
    using Ordering.API.Application.IntegrationCommands.Commands;
    using Ordering.API.Application.IntegrationEvents;

    public class UpdateOrderWhenOrderStockMethodVerifiedDomainEventHandler
                   : IAsyncNotificationHandler<OrderStockMethodVerifiedDomainEvent>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILoggerFactory _logger;
        private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;

        public UpdateOrderWhenOrderStockMethodVerifiedDomainEventHandler(
            IOrderRepository orderRepository, ILoggerFactory logger,
            IOrderingIntegrationEventService orderingIntegrationEventService)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderingIntegrationEventService = orderingIntegrationEventService;
        }

        public async Task Handle(OrderStockMethodVerifiedDomainEvent orderStockMethodVerifiedDomainEvent)
        {
            var orderToUpdate = await _orderRepository.GetAsync(orderStockMethodVerifiedDomainEvent.OrderId);
            orderToUpdate.SetOrderStatusId(orderStockMethodVerifiedDomainEvent.OrderStatus.Id);
                             
            _orderRepository.Update(orderToUpdate);

            await _orderRepository.UnitOfWork
                .SaveEntitiesAsync();

            _logger.CreateLogger(nameof(UpdateOrderWhenOrderStockMethodVerifiedDomainEventHandler))
                .LogTrace($"Order with Id: {orderStockMethodVerifiedDomainEvent.OrderId} has been successfully updated with " +
                          $"a status order id: { orderStockMethodVerifiedDomainEvent.OrderStatus.Id }");

            var payOrderCommandMsg = new PayOrderCommandMsg(orderToUpdate.Id);
            await _orderingIntegrationEventService.SaveEventAndOrderingContextChangesAsync(payOrderCommandMsg);
            await _orderingIntegrationEventService.PublishThroughEventBusAsync(payOrderCommandMsg);
        }
    }  
}