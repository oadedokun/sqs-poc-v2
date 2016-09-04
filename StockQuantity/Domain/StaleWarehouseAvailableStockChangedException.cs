using System;

namespace StockQuantity.Domain
{
    public class StaleWarehouseAvailableStockChangedException : Exception
    {
        public StaleWarehouseAvailableStockChangedException(string message):base(message)
        {
        }
    }
}
