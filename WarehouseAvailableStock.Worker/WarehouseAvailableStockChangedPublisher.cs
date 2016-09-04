using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
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
        private readonly IRegionStockAggregateStore _stockQuantityStore;
        private readonly int _skuVariantBatchSize;
        private readonly int _publishBatchSize;
        private readonly TelemetryClient _telemetryClient;
        private IReadOnlyList<SkuVariantMapDocument> _skuVariantMap;
        private StockStatus _stockStatus;
        private static string CORRELATION_SLOT = "CORRELATION-ID";

        public WarehouseAvailableStockChangedPublisher(TopicClient warehouseAvailableStockTopicClient, IRegionStockAggregateStore stockQuantityStore, int skuVariantBatchSize, int publishBatchSize, TelemetryClient telemetryClient)
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
            _skuVariantBatchSize = skuVariantBatchSize;
            _publishBatchSize = publishBatchSize;
            _telemetryClient = telemetryClient;
        }

        private void InitialiseSkuVariantCache()
        {
            _skuVariantMap = _stockQuantityStore.GetSkuVariantMap(_skuVariantBatchSize);
            _stockStatus = StockStatus.InStock;
        }
        
        public void PublishWarehouseAvailableStockChanged()
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

                var fc01BrokeredMessages =
                    _skuVariantMap.Select(x => new BrokeredMessage(new WarehouseAvailableStockChangedV1("FC01", x.SKU, pickable, reserved, allocated,
                        DateTime.UtcNow))).ToList();

                var fc04BrokeredMessages =
                    _skuVariantMap.Select(x => new BrokeredMessage(new WarehouseAvailableStockChangedV1("FC04", x.SKU, pickable, reserved, allocated,
                        DateTime.UtcNow))).ToList();

                for (var index = _publishBatchSize; index <= fc01BrokeredMessages.Count; index=+_publishBatchSize)
                {
                    var batchedBrokeredMessages = fc01BrokeredMessages.GetRange(index, _publishBatchSize);
                    if (batchedBrokeredMessages.Any())
                    {
                        _warehouseAvailableStockTopicClient.SendBatch(batchedBrokeredMessages);
                        Thread.Sleep(1000);
                        RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, true);
                    }
                }

                for (var index = _publishBatchSize; index <= fc04BrokeredMessages.Count; index = +_publishBatchSize)
                {
                    var batchedBrokeredMessages = fc04BrokeredMessages.GetRange(index, _publishBatchSize);
                    if (batchedBrokeredMessages.Any())
                    {
                        _warehouseAvailableStockTopicClient.SendBatch(batchedBrokeredMessages);
                        Thread.Sleep(1000);
                        RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, true);
                    }
                }
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

        public void PublishVariantCopyCompleted()
        {
            foreach (var skuVariantMap in _skuVariantMap)
            {
                var brokeredMessage = new BrokeredMessage(new VariantCopyCompletedV1(skuVariantMap.VariantId, skuVariantMap.SKU));
                brokeredMessage.Properties.Add("IsVariantSku", 1);
                _warehouseAvailableStockTopicClient.Send(brokeredMessage);
            }
        }

        public void PublishRestrictionAttributesAccepted()
        {
            foreach (var skuVariantMap in _skuVariantMap)
            {
                var brokeredMessage = new BrokeredMessage(new RestrictionAttributesAcceptedV1(skuVariantMap.SKU, new[] {"HAZMAT"}));
                brokeredMessage.Properties.Add("IsVariantSku", 1);
                _warehouseAvailableStockTopicClient.Send(brokeredMessage);
            }
        }
    }
}
