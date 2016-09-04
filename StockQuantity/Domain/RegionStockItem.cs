using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StockQuantity.Domain
{
    public class RegionStockItem
    {
        public RegionStockItem()
        {
            
        }

        public RegionStockItem(string region, int quantity, RegionStockItemStatus status, IEnumerable<WarehouseAvailableStockItem> warehouseAvailableStocks, DateTime version)
        {
            Region = region;
            Quantity = quantity;
            Status = status;
            _warehouseAvailableStocks = warehouseAvailableStocks.ToList();
            Version = version;
        }

        private List<WarehouseAvailableStockItem> _warehouseAvailableStocks;

        [JsonProperty("regionId")]
        public string Region { get; }

        [JsonProperty("quantity")]
        public int Quantity { get; private set; }

        [JsonProperty("status")]
        public RegionStockItemStatus Status { get; private set; }

        [JsonProperty("version")]
        public DateTime Version { get; private set; }
        
        [JsonIgnore]
        public IReadOnlyList<WarehouseAvailableStockItem> WarehouseAvailableStocks => _warehouseAvailableStocks;

        public RegionStockItem ApplyStockChanges(WarehouseAvailableStockItem warehouseAvailableAvailableStock)
        {
            if (warehouseAvailableAvailableStock == null)
            {
                throw new ArgumentNullException();
            }

            if (_warehouseAvailableStocks != null)
            {
                var index =
                    _warehouseAvailableStocks.FindIndex(
                        (x) =>
                            (x.Sku == warehouseAvailableAvailableStock.Sku &&
                             x.FulfilmentCentre == warehouseAvailableAvailableStock.FulfilmentCentre));
                if (index > -1)
                {
                    _warehouseAvailableStocks.RemoveAt(index);
                    _warehouseAvailableStocks.Insert(index, warehouseAvailableAvailableStock);
                }
                else
                {
                    _warehouseAvailableStocks.Add(warehouseAvailableAvailableStock);
                }

            }
            else
            {
                _warehouseAvailableStocks = new List<WarehouseAvailableStockItem> { warehouseAvailableAvailableStock };
            }

            Quantity = WarehouseAvailableStocks.Sum(x => x.Pickable - (x.Allocated + x.Reserved));

            if (Status == null)
            {
                Status = new RegionStockItemStatus(10, StockStatus.OutOfStock);
            }

            Status.Evaluate(Quantity);

            Version = warehouseAvailableAvailableStock.Version;

            return this;
        }
    }
}
