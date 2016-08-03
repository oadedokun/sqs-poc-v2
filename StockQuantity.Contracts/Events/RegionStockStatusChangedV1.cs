using System;
using System.Runtime.Serialization;

namespace StockQuantity.Contracts.Events
{
    [DataContract]
    public class RegionStockStatusChangedV1 : IRegionStockStatusChangedV1
    {
        public RegionStockStatusChangedV1(string region, int variantId, StockStatus status, DateTime version)
        {
            Region = region;
            VariantId = variantId;
            Status = status;
            Version = version;
        }

        [DataMember]
        public string Region { get; set; }

        [DataMember]
        public int VariantId { get; set; }

        [DataMember]
        public StockStatus Status { get; set; }

        [DataMember]
        public DateTime Version { get; set; }
    }
}
