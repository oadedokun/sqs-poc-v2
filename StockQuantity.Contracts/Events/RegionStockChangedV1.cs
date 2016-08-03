using System;
using System.Runtime.Serialization;

namespace StockQuantity.Contracts.Events
{
    [DataContract]
    public class RegionStockChangedV1 : RegionStockStatusChangedV1, IRegionStockChangedV1
    {
        public RegionStockChangedV1(string region, int variantId, StockStatus status, int quantity, DateTime version) 
            : base(region, variantId, status, version)
        {
            Quantity = quantity;
        }

        [DataMember]
        public int Quantity { get; set; }
    }
}
