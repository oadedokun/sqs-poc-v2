using System.Runtime.Serialization;

namespace StockQuantity.Contracts.Events
{
    [DataContract]
    public class RestrictionAttributesAcceptedV1 : IRestrictionAttributesAcceptedV1
    {
        public RestrictionAttributesAcceptedV1()
        {
            
        }

        public RestrictionAttributesAcceptedV1(string sku, string[] attributes)
        {
            Sku = sku;
            Attributes = attributes;
        }

        [DataMember]
        public string Sku { get; set; }

        [DataMember]
        public string[] Attributes { get; set; }
    }
}