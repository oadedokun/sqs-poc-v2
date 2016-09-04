using System.Runtime.Serialization;

namespace StockQuantity.Contracts.Events
{
    [DataContract]
    public class VariantCopyCompletedV1 : IVariantCopyCompletedV1
    {
        public VariantCopyCompletedV1()
        {
            
        }

        public VariantCopyCompletedV1(int variantId, string sku)
        {
            VariantId = variantId;
            SKU = sku;
        }

        [DataMember]
        public int VariantId { get; set; }

        [DataMember]
        public string SKU { get; set; }
    }
}
