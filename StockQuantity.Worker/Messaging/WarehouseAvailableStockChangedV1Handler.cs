﻿using System;
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
        private readonly SkuVariantCacheManager _skuVariantCacheManager;
        private readonly TopicClient _stockQuantityTopicClient;
        private readonly IRegionStockAggregateStore _regionStockAggregateStore;
        private static string CORRELATION_SLOT = "CORRELATION-ID";

        public WarehouseAvailableStockChangedV1Handler(TopicClient stockQuantityTopicClient, IRegionStockAggregateStore regionStockAggregateStore, SkuVariantCacheManager skuVariantCacheManager, TelemetryClient telemetryClient)
        {
            
            if (stockQuantityTopicClient == null)
            {
                throw new ArgumentNullException();
            }

            if (regionStockAggregateStore == null)
            {
                throw new ArgumentNullException();
            }

            _telemetryClient = telemetryClient;
            _stockQuantityTopicClient = stockQuantityTopicClient;
            _skuVariantCacheManager = skuVariantCacheManager;
            _regionStockAggregateStore = regionStockAggregateStore;
        }

        public void OnMessage(IWarehouseAvailableStockChangedV1 message)
        {
            var requestTelemetry = RequestTelemetryHelper.Start("Warehouse Stock Changed Processing Rate", DateTime.UtcNow);
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

                regionStockAggregate.ApplyStockChanges(new WarehouseAvailableStockItem(message.FulfilmentCentre,
                    message.Sku, message.Pickable, message.Reserved, message.Allocated, message.Version));

                _regionStockAggregateStore.UpdateRegionStock(RegionStockDocument.CreateFrom(regionStockAggregate));

                PublishRegionStockChanged(regionStockAggregate);
            }
            else
            {
                throw new RegionStockAggregateNotFoundException($"Failed retrieving Region Stock Aggregate for Sku {message.Sku}");
            }

            RequestTelemetryHelper.Dispatch(_telemetryClient, requestTelemetry, requestTimer.Elapsed, true);
            
        }

        private void PublishRegionStockChanged(IRegionStockAggregate stockQuantityAggregate)
        {
            foreach (var regionStock in stockQuantityAggregate.RegionStocks)
            {
                if (regionStock.Status.IsChanged)
                {
                    var regionStockStatusChanged = new RegionStockStatusChangedV1(regionStock.Region, stockQuantityAggregate.VariantId, (Contracts.StockStatus)regionStock.Status.Value, regionStock.Version);
                    var brokeredMessage = new BrokeredMessage(regionStockStatusChanged);
                    brokeredMessage.TimeToLive = TimeSpan.FromMinutes(30);
                    _stockQuantityTopicClient.Send(brokeredMessage);
                }
            }
            
        }
    }
}
