using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Microsoft.ApplicationInsights;
using Microsoft.ServiceBus.Messaging;
using StockQuantity.Contracts;
using StockQuantity.Contracts.Events;
using StockQuantity.Data;

namespace WarehouseAvailableStock.Worker
{
    public class WarehouseAvailableStockChangedPublisher
    {
        private readonly TopicClient _warehouseAvailableStockTopicClient;
        private readonly IStockQuantityAggregateStore _stockQuantityStore;
        private readonly int _maximumConcurrency;
        private readonly TelemetryClient _telemetryClient;
        private IReadOnlyList<SkuVariantMap> _skuVariantMap;
        private StockStatus _stockStatus;
        private static string CORRELATION_SLOT = "CORRELATION-ID";

        public WarehouseAvailableStockChangedPublisher(TopicClient warehouseAvailableStockTopicClient, IStockQuantityAggregateStore stockQuantityStore, int batchSize, TelemetryClient telemetryClient)
        {
            if (warehouseAvailableStockTopicClient == null)
            {
                throw new ArgumentNullException();
            }

            if (stockQuantityStore == null)
            {
                throw new ArgumentNullException();
            }

            if (warehouseAvailableStockTopicClient == null)
            {
                throw new ArgumentNullException();
            }
            _warehouseAvailableStockTopicClient = warehouseAvailableStockTopicClient;
            _stockQuantityStore = stockQuantityStore;
            _maximumConcurrency = batchSize;
            _telemetryClient = telemetryClient;
        }

        private void InitialiseSkuVariantCache()
        {
            _skuVariantMap = _stockQuantityStore.GetSkuVariantMap(_maximumConcurrency);
            _stockStatus = StockStatus.InStock;
        }

        public void Publish()
        {
            Stopwatch requestTimer = Stopwatch.StartNew();
            var requestTelemetry = RequestTelemetryHelper.Start("Batch Publish Rate", DateTime.UtcNow);
            CallContext.LogicalSetData(CORRELATION_SLOT, requestTelemetry.Id);

            try
            {
                if (_skuVariantMap == null || !_skuVariantMap.Any())
                {
                    InitialiseSkuVariantCache();
                }

                if (_skuVariantMap == null || !_skuVariantMap.Any())
                {
                    throw new Exception("Failed to Initialise Sku-Variant Cache");
                }

                int pickable;
                int reserved;
                int allocated;

                switch (_stockStatus)
                {
                    case StockStatus.InStock:
                        pickable = 20;
                        reserved = 0;
                        allocated = 10;
                        _stockStatus = StockStatus.LowInStock;
                        break;
                    case StockStatus.LowInStock:
                        pickable = 30;
                        reserved = 1;
                        allocated = 20;
                        _stockStatus = StockStatus.OutOfStock;
                        break;
                    case StockStatus.OutOfStock:
                        pickable = 40;
                        reserved = 20;
                        allocated = 20;
                        _stockStatus = StockStatus.InStock;
                        break;
                    default:
                        pickable = 20;
                        reserved = 0;
                        allocated = 10;
                        break;
                }

                var brokeredMessages =
                    _skuVariantMap.Select(x => new BrokeredMessage(new WarehouseAvailableStockChangedV1("FC01", x.SKU, pickable, reserved, allocated,
                        DateTime.UtcNow))).ToList();

                requestTelemetry.Metrics.Add(new KeyValuePair<string, double>("WarehouseAvailableStockChangedV1 Message Count", brokeredMessages.Count));
                _warehouseAvailableStockTopicClient.SendBatch(brokeredMessages);
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
    }
}
