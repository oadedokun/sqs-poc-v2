using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Microsoft.ApplicationInsights;
using Microsoft.ServiceBus.Messaging;
using StockQuantity.Contracts.Events;
using StockQuantity.Data;
using StockQuantity.Domain;

namespace StockQuantity.Worker.Messaging
{
    public class WarehouseAvailableStockChangedV1Handler : IMessageHandler<IWarehouseAvailableStockChangedV1>
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly TopicClient _stockQuantityTopicClient;
        private readonly IStockQuantityAggregateStore _stockQuantityStore;
        private static string CORRELATION_SLOT = "CORRELATION-ID";

        public WarehouseAvailableStockChangedV1Handler(TopicClient stockQuantityTopicClient, IStockQuantityAggregateStore stockQuantityStore, TelemetryClient telemetryClient)
        {
            
            if (stockQuantityTopicClient == null)
            {
                throw new ArgumentNullException();
            }

            if (stockQuantityStore == null)
            {
                throw new ArgumentNullException();
            }

            if (stockQuantityTopicClient == null)
            {
                throw new ArgumentNullException();
            }

            _telemetryClient = telemetryClient;
            _stockQuantityTopicClient = stockQuantityTopicClient;
            _stockQuantityStore = stockQuantityStore;
        }

        public void OnMessage(IWarehouseAvailableStockChangedV1 message)
        {
            var requestTelemetry = RequestTelemetryHelper.Start("Warehouse Stock Changed Processing Rate", DateTime.UtcNow);
            Stopwatch requestTimer = Stopwatch.StartNew();
            CallContext.LogicalSetData(CORRELATION_SLOT, requestTelemetry.Id);

            try
            {

                if (message == null)
                {
                    throw new ArgumentNullException();
                }

                var stockQuantityAggregate = CreateStockQuantityAggregateBySku(message.Sku, _stockQuantityStore);
                stockQuantityAggregate.ApplyStockChanges(new Domain.WarehouseAvailableStock(message.FulfilmentCentre, message.Sku, message.Pickable, message.Reserved, message.Allocated, message.Version));
                _stockQuantityStore.Persist(stockQuantityAggregate);
                PublishRegionStockChanged(stockQuantityAggregate);
                RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, true);
            }
            catch (Exception ex)
            {
                string err = ex.Message;
                if (ex.InnerException != null)
                {
                    err += " Inner Exception: " + ex.InnerException.Message;
                }
                Trace.TraceError(err, ex);
                RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, false);
            }
        }

        private StockQuantityAggregate CreateStockQuantityAggregateBySku(string sku, IStockQuantityAggregateStore stockQuantityStore)
        {
            var skuVariantMap = stockQuantityStore.GetSkuVariantMap(sku);
            if (skuVariantMap == null)
            {
                throw new Exception("Sku Variant Map Not Found");
            }

            var stockQuantity = stockQuantityStore.GetStockQuantityByVariantId(skuVariantMap.VariantId);

            if (stockQuantity != null)
            {
                return new StockQuantityAggregate(stockQuantity.VariantId, stockQuantity.WarehouseAvailableStocks.ToList(),
                    stockQuantity.RegionStocks.ToList(), stockQuantity.Version);
            }

            return new StockQuantityAggregate(skuVariantMap.VariantId, null, null, null);
        }

        private void PublishRegionStockChanged(IStockQuantityAggregate stockQuantityAggregate)
        {
            foreach (var regionStock in stockQuantityAggregate.RegionStocks)
            {
                if (regionStock.Status.IsChanged)
                {
                    var regionStockStatusChanged = new RegionStockStatusChangedV1(regionStock.Region, regionStock.VariantId, (Contracts.StockStatus)regionStock.Status.Value, regionStock.Version);

                    _stockQuantityTopicClient.Send(new BrokeredMessage(regionStockStatusChanged));
                }
            }
            
        }
    }
}
