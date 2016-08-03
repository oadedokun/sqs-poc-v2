using System.Collections.Generic;

namespace StockQuantity.Domain
{
    public interface IStockQuantityAggregate
    {
        int VariantId { get; }
        List<WarehouseAvailableStock> WarehouseAvailableStocks { get; }
        List<RegionStock> RegionStocks { get; }
        string Version { get; }
        void ApplyStockChanges(WarehouseAvailableStock warehouseAvailableStock);
    }
}