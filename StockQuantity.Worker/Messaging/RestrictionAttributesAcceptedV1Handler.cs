using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Microsoft.ApplicationInsights;
using StockQuantity.Contracts.Events;
using StockQuantity.Data;
using StockQuantity.Domain;

namespace StockQuantity.Worker.Messaging
{
    public class RestrictionAttributesAcceptedV1Handler : IMessageHandler<IRestrictionAttributesAcceptedV1>
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IRegionStockAggregateStore _regionStockAggregateStore;
        private readonly SkuVariantCacheManager _skuVariantCacheManager;
        private static string CORRELATION_SLOT = "CORRELATION-ID";

        public RestrictionAttributesAcceptedV1Handler(IRegionStockAggregateStore regionStockAggregateStore, SkuVariantCacheManager skuVariantCacheManager, TelemetryClient telemetryClient)
        {
            if (regionStockAggregateStore == null)
            {
                throw new ArgumentNullException();
            }

            _telemetryClient = telemetryClient;
            _regionStockAggregateStore = regionStockAggregateStore;
            _skuVariantCacheManager = skuVariantCacheManager;
        }
        public void OnMessage(IRestrictionAttributesAcceptedV1 message)
        {
            var requestTelemetry = RequestTelemetryHelper.Start("Restriction Attributes Accepted Processing Rate", DateTime.UtcNow);
            CallContext.LogicalSetData(CORRELATION_SLOT, requestTelemetry.Id);
            Stopwatch requestTimer = Stopwatch.StartNew();

            if (message == null)
            {
                throw new ArgumentNullException();
            }

            var skuVariant = _skuVariantCacheManager.GetItemByKey(message.Sku);

            if (skuVariant == null)
            {
                throw new Exception($"Sku Variant Not Found For Restriction Attribute Accepted with Sku: {message.Sku}");
            }

            var regionStock = _regionStockAggregateStore.GetRegionStockByVariantId(skuVariant.VariantId);
            if (regionStock != null)
            {
                IRegionStockAggregate regionStockAggregate = new RegionStockAggregate(regionStock.VariantId,
                    regionStock.WarehouseAvailableStocks.ToList(),
                    regionStock.RegionStocks.ToList(), null, regionStock.Version);

                regionStockAggregate.ApplyRestrictionAttributes(message.Attributes);

                _regionStockAggregateStore.UpdateRegionStock(RegionStockDocument.CreateFrom(regionStockAggregate));

            }
            else
            {
                throw new RegionStockAggregateNotFoundException($"Failed retrieving Region Stock Aggregate for Sku {message.Sku}");
            }

            RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, true);
        }
    }
}