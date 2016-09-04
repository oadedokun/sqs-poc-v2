using System;

namespace StockQuantity.Data
{
    public class DuplicateResourceKeyException : Exception
    {
        public DuplicateResourceKeyException(string message) : base(message)
        {
        }
        public DuplicateResourceKeyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}