using System;
using System.Runtime.Serialization;

namespace StockQuantity.Contracts.Events
{
    [DataContract]
    public class WarehouseAvailableStockChangedV1 : IWarehouseAvailableStockChangedV1
    {
        public WarehouseAvailableStockChangedV1(string fulfilmentCentre, string sku, int pickable, int reserved, int allocated, DateTime version)
        {
            FulfilmentCentre = fulfilmentCentre;
            Sku = sku;
            Pickable = pickable;
            Reserved = reserved;
            Allocated = allocated;
            Version = version;
        }

        [DataMember]
        public string FulfilmentCentre { get; set; }

        [DataMember]
        public string Sku { get; set; }

        [DataMember]
        public int Pickable { get; set; }

        [DataMember]
        public int Reserved { get; set; }

        [DataMember]
        public int Allocated { get; set; }

        [DataMember]
        public DateTime Version { get; set; }
    }
}
