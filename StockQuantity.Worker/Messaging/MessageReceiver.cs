using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ServiceBus.Messaging;
using StockQuantity.Data;

namespace StockQuantity.Worker.Messaging
{
    public abstract class MessageReceiver
    {
        protected SkuVariantCacheManager SkuVariantCacheManager;
        protected readonly int MaximumConcurrency;
        protected readonly string WasServiceBusConnectionString;
        protected readonly string WasSubscriptionName;
        protected readonly string WasTopicPath;
        protected readonly TopicClient StockQuantityTopicClient;
        protected readonly IRegionStockAggregateStore StockQuantityAggregateStore;
        protected readonly TelemetryClient TelemetryClient;
        protected List<SubscriptionClient> SubscriptionClients;
        private bool _disposed;

        protected MessageReceiver(int maximumConcurrency, string wasServiceBusConnectionString, string wasSubscriptionName, string wasTopicPath, TopicClient stockQuantityTopicClient, IRegionStockAggregateStore stockQuantityAggregateStore, SkuVariantCacheManager skuVariantCacheManager, TelemetryClient telemetryClient)
        {
            SkuVariantCacheManager = skuVariantCacheManager;
            MaximumConcurrency = maximumConcurrency;
            WasServiceBusConnectionString = wasServiceBusConnectionString;
            WasSubscriptionName = wasSubscriptionName;
            WasTopicPath = wasTopicPath;
            StockQuantityTopicClient = stockQuantityTopicClient;
            StockQuantityAggregateStore = stockQuantityAggregateStore;
            TelemetryClient = telemetryClient;
        }

        public abstract void ReceiveMessages(CancellationToken token);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    SubscriptionClients?.ForEach(x => x.Close());
                }

                _disposed = true;
            }
        }
    }
}