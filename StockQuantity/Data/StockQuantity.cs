using System.Collections.Generic;
using Newtonsoft.Json;
using StockQuantity.Domain;

namespace StockQuantity.Data
{
    public class StockQuantity
    {
        public StockQuantity()
        {
            
        }

        public StockQuantity(int variantId, IEnumerable<WarehouseAvailableStock> warehouseAvailableStocks, IEnumerable<RegionStock> regionStocks)
        {
            Id = variantId.ToString();
            VariantId = variantId;
            WarehouseAvailableStocks = warehouseAvailableStocks;
            RegionStocks = regionStocks;
        }

        public StockQuantity(int variantId, IEnumerable<WarehouseAvailableStock> warehouseAvailableStocks, IEnumerable<RegionStock> regionStocks, string version)
        {
            Id = variantId.ToString();
            VariantId = variantId;
            WarehouseAvailableStocks = warehouseAvailableStocks;
            RegionStocks = regionStocks;
            Version = version;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("variantId")]
        public int VariantId { get; set; }

        [JsonProperty("warehouseAvailableStocks")]
        public IEnumerable<WarehouseAvailableStock> WarehouseAvailableStocks { get; set; }

        [JsonProperty("regionStocks")]
        public IEnumerable<RegionStock> RegionStocks { get; set; }

        [JsonProperty("_etag")]
        public string Version { get; set; }

        public static StockQuantity CreateFrom(IStockQuantityAggregate stockQuantityAggregate)
        {
            return new StockQuantity(stockQuantityAggregate.VariantId, stockQuantityAggregate.WarehouseAvailableStocks, stockQuantityAggregate.RegionStocks, stockQuantityAggregate.Version);
        }
    }
}
