namespace StockQuantity.Contracts.Events
{
    public interface IRestrictionAttributesAcceptedV1 : IMessageV1
    {
        string Sku { get; set; }
        string[] Attributes { get; set; }
    }
}