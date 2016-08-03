using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StockQuantity.Domain
{
    public class RegionStock
    {
        public RegionStock()
        {
            
        }

        public RegionStock(string region, int quantity, RegionStockStatus status, IEnumerable<WarehouseAvailableStock> warehouseAvailableStocks)
        {
            Region = region;
            Quantity = quantity;
            Status = status;
            _warehouseAvailableStocks = warehouseAvailableStocks.ToList();
        }

        public RegionStock(string region, int variantId, int quantity, RegionStockStatus status, IEnumerable<WarehouseAvailableStock> warehouseAvailableStocks)
        {
            Region = region;
            VariantId = variantId;
            Quantity = quantity;
            Status = status;
            _warehouseAvailableStocks = warehouseAvailableStocks.ToList();
        }

        private List<WarehouseAvailableStock> _warehouseAvailableStocks;

        [JsonProperty("regionId")]
        public string Region { get; }

        [JsonProperty("variantId")]
        public int VariantId { get; }

        [JsonProperty("quantity")]
        public int Quantity { get; private set; }

        [JsonProperty("status")]
        public RegionStockStatus Status { get; private set; }

        [JsonProperty("version")]
        public DateTime Version { get; private set; }
        
        [JsonIgnore]
        public IReadOnlyList<WarehouseAvailableStock> WarehouseAvailableStocks => _warehouseAvailableStocks;

        public RegionStock ApplyStockChanges(WarehouseAvailableStock warehouseAvailableAvailableStock)
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
                _warehouseAvailableStocks = new List<WarehouseAvailableStock> { warehouseAvailableAvailableStock };
            }

            Quantity = WarehouseAvailableStocks.Sum(x => x.Pickable - (x.Allocated + x.Reserved));

            if (Status == null)
            {
                Status = new RegionStockStatus(10, StockStatus.OutOfStock);
            }

            Status.Evaluate(Quantity);

            Version = warehouseAvailableAvailableStock.Version;

            return this;
        }
    }
}
