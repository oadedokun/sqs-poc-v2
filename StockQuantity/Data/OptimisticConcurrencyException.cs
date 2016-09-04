using System;

namespace StockQuantity.Data
{
    public class OptimisticConcurrencyException : Exception
    {
        public OptimisticConcurrencyException(string message) : base(message)
        {
        }

        public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}