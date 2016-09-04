using System;
using Newtonsoft.Json;

namespace StockQuantity.Domain
{
    public class WarehouseAvailableStockItem
    {
        public WarehouseAvailableStockItem(string fulfilmentCentre, string sku, int pickable, int reserved, int allocated, DateTime version)
        {
            FulfilmentCentre = fulfilmentCentre;
            Sku = sku;
            Pickable = pickable;
            Reserved = reserved;
            Allocated = allocated;
            Version = version;
        }

        [JsonProperty("warehouseId")]
        public string FulfilmentCentre { get; }

        [JsonProperty("sku")]
        public string Sku { get; }

        [JsonProperty("pickable")]
        public int Pickable { get; private set; }

        [JsonProperty("reserved")]
        public int Reserved { get; private set; }

        [JsonProperty("allocated")]
        public int Allocated { get; private set; }

        [JsonProperty("version")]
        public DateTime Version { get; private set; }

        public WarehouseAvailableStockItem ApplyStockChanges(WarehouseAvailableStockItem warehouseAvailableStock)
        {
            if (Sku.Equals(warehouseAvailableStock.Sku, StringComparison.OrdinalIgnoreCase) &&
                FulfilmentCentre.Equals(warehouseAvailableStock.FulfilmentCentre, StringComparison.OrdinalIgnoreCase))
            {
                if (Version > warehouseAvailableStock.Version)
                {
                    throw new StaleWarehouseAvailableStockChangedException(
                        $"Stale Warehouse Available Stock Change encountered for SKU {warehouseAvailableStock.Sku} in Warehouse {warehouseAvailableStock.FulfilmentCentre}");
                }

                Pickable = warehouseAvailableStock.Pickable;
                Reserved = warehouseAvailableStock.Reserved;
                Allocated = warehouseAvailableStock.Allocated;
                Version = warehouseAvailableStock.Version;
            }

            return this;
        }
    }

    
}
