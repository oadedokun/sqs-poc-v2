using Newtonsoft.Json;

namespace StockQuantity.Data
{
    public class SkuVariantMapDocument
    {
        public SkuVariantMapDocument()
        {
            
        }

        [JsonProperty("id")]
        public string SKU { get; set; }

        [JsonProperty("variantId")]
        public int VariantId { get; set; }

        [JsonProperty("_etag")]
        public string Version { get; set; }
    }
}