using StockQuantity.Contracts.Events;

namespace StockQuantity.Worker.Messaging
{
    public interface IMessageHandler<in T> where T : IMessageV1
    {
        void OnMessage(T message);
    }
}
