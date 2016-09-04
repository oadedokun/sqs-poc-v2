using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StockQuantity.Domain
{
    public class RegionStockItemStatus
    {
        public RegionStockItemStatus()
        {
            
        }
        public RegionStockItemStatus(int lowInStockThreshold, StockStatus status)
        {
            _lowInStockThreshold = lowInStockThreshold;
            Value = status;
            IsChanged = false;
        }

        public RegionStockItemStatus(int lowInStockThreshold, StockStatus status, bool isChanged)
        {
            _lowInStockThreshold = lowInStockThreshold;
            Value = status;
            IsChanged = isChanged;
        }

        private readonly int _lowInStockThreshold;

        [JsonConverter(typeof(StringEnumConverter))]
        public StockStatus Value { get; private set; }

        [JsonIgnore]
        public bool IsChanged { get; private set; }
        public RegionStockItemStatus Evaluate(int quantity)
        {
            var stockStatus = quantity > 0 && (quantity < _lowInStockThreshold) ? StockStatus.LowInStock
                : quantity > 0 && (quantity >= _lowInStockThreshold) ? StockStatus.InStock
                : StockStatus.OutOfStock;

            IsChanged = Value != stockStatus;

            Value = stockStatus;

            return this;
        }

    }
}
