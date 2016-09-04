using System.Collections.Generic;

namespace StockQuantity.Domain
{
    public interface IRegionStockAggregate
    {
        int VariantId { get; }
        List<WarehouseAvailableStockItem> WarehouseAvailableStocks { get; }
        List<RegionStockItem> RegionStocks { get; }
        string Version { get; }
        void ApplyStockChanges(WarehouseAvailableStockItem warehouseAvailableStock);
        void ApplyRestrictionAttributes(string[] attributes);
    }
}