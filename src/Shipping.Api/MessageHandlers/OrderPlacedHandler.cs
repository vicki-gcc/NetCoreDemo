﻿namespace Shipping.Api.MessageHandlers
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using EShop.Messages.Events;

    public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
    {
        public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
        {
            Console.WriteLine("A new order has been placed, prepare the inventory for shipping");
            await context.Publish(new OrderBilled() { ProductId = message.ProductId });
        }
    }
}
