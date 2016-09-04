using System;

namespace StockQuantity.Domain
{
    public class InvalidWarehouseAvailableStockException : Exception
    {
        public InvalidWarehouseAvailableStockException(string message):base(message)
        {
        }
    }
}
