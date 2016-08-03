using System;

namespace StockQuantity.Contracts.Events
{
    public interface IRegionStockChangedV1 : IMessageV1
    {
        string Region { get; }
        int VariantId { get; }
        StockStatus Status { get; }
        int Quantity { get; }
        DateTime Version { get; }
    }
}