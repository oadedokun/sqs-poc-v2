using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StockQuantity.Data;

namespace StockQuantity.Domain
{
    public class RegionStockAggregate : IRegionStockAggregate
    {
        private readonly List<WarehouseRegion> _warehouseRegions = new List<WarehouseRegion>();
        private const int LowInStockThreshold = 10;

        public RegionStockAggregate(int variantId, List<WarehouseAvailableStockItem> warehouseAvailableStocks, List<RegionStockItem> regionStocks, List<RegionRestriction> regionRestrictions, string version)
        {
            VariantId = variantId;
            WarehouseAvailableStocks = warehouseAvailableStocks ?? new List<WarehouseAvailableStockItem>();
            RegionStocks = regionStocks ?? new List<RegionStockItem>();
            RegionRestrictions = regionRestrictions ?? new List<RegionRestriction>();
            Version = version;
            ConfigureWarehouseRegions();
            InitialiseRegionRestrictions();
        }
        
        public int VariantId { get; }
        public List<WarehouseAvailableStockItem> WarehouseAvailableStocks { get; }
        public List<RegionStockItem> RegionStocks { get; }
        public List<RegionRestriction> RegionRestrictions { get; }

        public string Version { get; }
        public void ApplyStockChanges(WarehouseAvailableStockItem warehouseAvailableStock)
        {
            var availableStock =
                WarehouseAvailableStocks.SingleOrDefault(
                    x => x.Sku.Equals(warehouseAvailableStock.Sku, StringComparison.OrdinalIgnoreCase) &&
                         x.FulfilmentCentre.Equals(warehouseAvailableStock.FulfilmentCentre,
                             StringComparison.OrdinalIgnoreCase));

            if (availableStock == null)
            {
                WarehouseAvailableStocks.Add(warehouseAvailableStock);
            }
            else
            {
                availableStock.ApplyStockChanges(warehouseAvailableStock);
            }

            ApplyRegionStockChanges(warehouseAvailableStock);
        }

        public void ApplyRestrictionAttributes(string[] attributes)
        {
            if (!RegionRestrictions.Any())
            {
                InitialiseRegionRestrictions();
            }

            foreach (var rr in RegionRestrictions)
            {
                foreach (var attribute in attributes)
                {
                    rr.Restricted = _warehouseRegions.Any(x => x.Restrictions.Any(ra => ra == attribute) && x.RegionId == rr.RegionId);
                }
            }
        }

        private void ApplyRegionStockChanges(WarehouseAvailableStockItem warehouseAvailableStock)
        {
            var wrs = _warehouseRegions.Where(x => x.WarehouseId == warehouseAvailableStock.FulfilmentCentre);
            foreach (var wr in wrs)
            {
                var rs = RegionStocks.SingleOrDefault(x => x.Region == wr.RegionId);
                // get regions affected - map regions to warehouse
                if (rs == null)
                {
                    rs = new RegionStockItem(wr.RegionId, 0, new RegionStockItemStatus(LowInStockThreshold, StockStatus.OutOfStock), new WarehouseAvailableStockItem[] { }, DateTime.UtcNow);
                    rs.ApplyStockChanges(warehouseAvailableStock);
                    RegionStocks.Add(rs);
                }
                else
                {
                    rs.ApplyStockChanges(warehouseAvailableStock);
                }
            }
        }

        private void InitialiseRegionRestrictions()
        {
            foreach (var wr in _warehouseRegions)
            {
                var rs = RegionRestrictions.SingleOrDefault(x => x.RegionId == wr.RegionId);
                if (rs == null)
                {
                    var regionRestriction = new RegionRestriction(wr.WarehouseId, wr.RegionId, false);
                    RegionRestrictions.Add(regionRestriction);
                }
            }
            
        }

        private void ConfigureWarehouseRegions()
        {
            _warehouseRegions.Add(new WarehouseRegion("FC01", "RoW", new[] { "HAZMAT" }));
            _warehouseRegions.Add(new WarehouseRegion("FC01", "UK", new [] {""}));
            _warehouseRegions.Add(new WarehouseRegion("FC01", "US", new[] { "HAZMAT", "LEVIS" }));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "FR", new[] { "" }));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "IT", new[] { "" }));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "ES", new[] { "" }));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "DE", new[] { ""}));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "PO", new[] { "" }));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "AUS", new[] { "" }));
            _warehouseRegions.Add(new WarehouseRegion("FC04", "US", new[] { "HAZMAT", "LEVIS" }));
        }

    }
}