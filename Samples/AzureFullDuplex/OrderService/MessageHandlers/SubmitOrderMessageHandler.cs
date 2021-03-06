using System.Threading;
using MyMessages;
using NServiceBus;
using Order=MyMessages.Order;

namespace OrderService.MessageHandlers
{
    public class SubmitOrderMessageHandler : IHandleMessages<SubmitOrderRequest>
    {
        private readonly IBus bus;
        
        public SubmitOrderMessageHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(SubmitOrderRequest message)
        {
            var order = new Order
                            {
                                Id = message.Id, 
                                Quantity = message.Quantity,
                                Status = OrderStatus.Pending
                            };

            // Thread.Sleep(4000); //simulate processing
            
            var response = bus.CreateInstance<SubmitOrderResponse>(x=>x.Order = order);
            bus.Reply(response);
        }
    }
}