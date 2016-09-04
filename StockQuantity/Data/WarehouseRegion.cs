using Newtonsoft.Json;

namespace StockQuantity.Data
{
    public class WarehouseRegion
    {
        public WarehouseRegion()
        {
            
        }

        public WarehouseRegion(string warehouseId, string regionId, string[] restrictions)
        {
            WarehouseId = warehouseId;
            RegionId = regionId;
            Restrictions = restrictions;
        }

        [JsonProperty("warehouseId")]
        public string WarehouseId { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; }

        [JsonProperty("restrictions")]
        public string[] Restrictions { get; set; }
    }
}