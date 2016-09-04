using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StockQuantity.Data;

namespace StockQuantity.Domain
{
    public class RegionRestriction
    {
        public RegionRestriction()
        {
            
        }

        public RegionRestriction(string warehouseId, string regionId, bool restricted)
        {
            WarehouseId = warehouseId;
            RegionId = regionId;
            Restricted = restricted;
        }

        [JsonProperty("warehouseId")]
        public string WarehouseId { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        
        [JsonProperty("restricted")]
        public bool Restricted { get; set; }
    }
}
