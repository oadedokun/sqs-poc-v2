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
    public class VariantCopyCompletedV1Handler : IMessageHandler<IVariantCopyCompletedV1>
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IRegionStockAggregateStore _regionStockAggregateStore;
        private static string CORRELATION_SLOT = "CORRELATION-ID";

        public VariantCopyCompletedV1Handler(IRegionStockAggregateStore regionStockAggregateStore, TelemetryClient telemetryClient)
        {
            if (regionStockAggregateStore == null)
            {
                throw new ArgumentNullException();
            }
            
            _telemetryClient = telemetryClient;
            _regionStockAggregateStore = regionStockAggregateStore;
        }
        public void OnMessage(IVariantCopyCompletedV1 message)
        {
            var requestTelemetry = RequestTelemetryHelper.Start("Variant Copy Completed Processing Rate", DateTime.UtcNow);
            CallContext.LogicalSetData(CORRELATION_SLOT, requestTelemetry.Id);
            Stopwatch requestTimer = Stopwatch.StartNew();

            if (message == null)
            {
                throw new ArgumentNullException();
            }

            var regionStock = _regionStockAggregateStore.GetRegionStockByVariantId(message.VariantId);
            IRegionStockAggregate regionStockAggregate;

            if (regionStock != null)
            {
                regionStockAggregate = new RegionStockAggregate(regionStock.VariantId,
                    regionStock.WarehouseAvailableStocks.ToList(), regionStock.RegionStocks.ToList(), null,
                    regionStock.Version);

                if (regionStockAggregate.WarehouseAvailableStocks.All(x => x.Sku != message.SKU))
                {
                    regionStockAggregate.ApplyStockChanges(new WarehouseAvailableStockItem("", message.SKU, 0, 0, 0, DateTime.UtcNow));
                }
            }
            else
            {
                regionStockAggregate = new RegionStockAggregate(message.VariantId,
                    new List<WarehouseAvailableStockItem>(),
                    new List<RegionStockItem>(),
                    null,
                    null);
                regionStockAggregate.ApplyStockChanges(new WarehouseAvailableStockItem("", message.SKU, 0, 0, 0, DateTime.UtcNow));
            }

            _regionStockAggregateStore.Persist(regionStockAggregate);
            　
            RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, true);
        }
    }
}