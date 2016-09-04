using System.Collections.Generic;
using Newtonsoft.Json;
using StockQuantity.Domain;

namespace StockQuantity.Data
{
    public class RegionStockDocument
    {
        public RegionStockDocument()
        {
            
        }

        public RegionStockDocument(int variantId, IEnumerable<WarehouseAvailableStockItem> warehouseAvailableStocks, IEnumerable<RegionStockItem> regionStocks)
        {
            Id = variantId.ToString();
            VariantId = variantId;
            WarehouseAvailableStocks = warehouseAvailableStocks;
            RegionStocks = regionStocks;
        }

        public RegionStockDocument(int variantId, IEnumerable<WarehouseAvailableStockItem> warehouseAvailableStocks, IEnumerable<RegionStockItem> regionStocks, string version)
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
        public IEnumerable<WarehouseAvailableStockItem> WarehouseAvailableStocks { get; set; }

        [JsonProperty("regionStocks")]
        public IEnumerable<RegionStockItem> RegionStocks { get; set; }

        [JsonProperty("_etag")]
        public string Version { get; set; }

        public static RegionStockDocument CreateFrom(IRegionStockAggregate stockQuantityAggregate)
        {
            return new RegionStockDocument(stockQuantityAggregate.VariantId, stockQuantityAggregate.WarehouseAvailableStocks, stockQuantityAggregate.RegionStocks, stockQuantityAggregate.Version);
        }
    }
}
