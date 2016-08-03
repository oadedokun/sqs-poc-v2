using System;

namespace StockQuantity.Contracts.Events
{
    public interface IWarehouseAvailableStockChangedV1 : IMessageV1
    {
        string FulfilmentCentre { get; }
        string Sku { get; }
        int Pickable { get; set; }
        int Reserved { get; set; }
        int Allocated { get; set; }
        DateTime Version { get; set; }
    }
}