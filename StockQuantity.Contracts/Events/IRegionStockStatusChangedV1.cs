using System;

namespace StockQuantity.Contracts.Events
{
    public interface IRegionStockStatusChangedV1 : IMessageV1
    {
        string Region { get; }
        int VariantId { get; }
        StockStatus Status { get; }
        DateTime Version { get; }
    }
}
