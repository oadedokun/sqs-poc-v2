namespace StockQuantity.Contracts.Events
{
    public interface IVariantCopyCompletedV1 : IMessageV1
    {
        int VariantId { get; set; }
        string SKU { get; set; }
    }
}