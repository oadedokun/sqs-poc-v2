using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace StockQuantity.Domain
{
    public class StockQuantityAggregate : IStockQuantityAggregate
    {
        public StockQuantityAggregate()
        {
            
        }

        public StockQuantityAggregate(int variantId, List<WarehouseAvailableStock> warehouseAvailableStocks, List<RegionStock> regionStocks, string version)
        {
            VariantId = variantId;
            WarehouseAvailableStocks = warehouseAvailableStocks ?? new List<WarehouseAvailableStock>();
            RegionStocks = regionStocks ?? new List<RegionStock>();
            Version = version;
        }
        
        public int VariantId { get; }
        public List<WarehouseAvailableStock> WarehouseAvailableStocks { get; }
        public List<RegionStock> RegionStocks { get; }
        public string Version { get; }
        public void ApplyStockChanges(WarehouseAvailableStock warehouseAvailableStock)
        {
            var availableStock =
                WarehouseAvailableStocks.SingleOrDefault(
                    x => x.Sku.Equals(warehouseAvailableStock.Sku, StringComparison.OrdinalIgnoreCase) &&
                         x.FulfilmentCentre.Equals(warehouseAvailableStock.FulfilmentCentre,
                             StringComparison.OrdinalIgnoreCase));

            if (availableStock == null)
            {
                WarehouseAvailableStocks.Add(warehouseAvailableStock);
            }
            else
            {
                availableStock.ApplyStockChanges(warehouseAvailableStock);
            }

            var regionStock = RegionStocks.FirstOrDefault();

            // get regions affected - map regions to warehouse
            if (regionStock == null)
            {
                regionStock = new RegionStock("US", VariantId, 0, null, new WarehouseAvailableStock [] { });
                regionStock.ApplyStockChanges(warehouseAvailableStock);
                RegionStocks.Add(regionStock);
            }
            else
            {
                regionStock.ApplyStockChanges(warehouseAvailableStock);
            }
        }

    }
}