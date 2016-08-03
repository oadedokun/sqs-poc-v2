using Newtonsoft.Json;

namespace StockQuantity.Data
{
    public class SkuVariantMap
    {
        public SkuVariantMap()
        {
            
        }

        [JsonProperty("id")]
        public string SKU { get; set; }

        [JsonProperty("variantId")]
        public int VariantId { get; set; }
    }
}