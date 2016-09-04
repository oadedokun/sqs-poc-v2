using System;

namespace StockQuantity.Domain
{
    public class RegionStockAggregateNotFoundException : Exception
    {
        public RegionStockAggregateNotFoundException()
        {
            
        }
        public RegionStockAggregateNotFoundException(string message):base(message)
        {
        }
    }
}