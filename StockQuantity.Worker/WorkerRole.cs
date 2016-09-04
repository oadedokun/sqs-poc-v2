using System;
using System.Net;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.Azure;
using Microsoft.Azure.Documents.Client;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using StockQuantity.Data;
using StockQuantity.Worker.Messaging;

namespace StockQuantity.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        private TelemetryClient _telemetryClient;
        private IRegionStockAggregateStore _stockQuantityAggregateStore;
        private SkuVariantCacheManager _skuVariantCacheManager;
        private TopicClient _topicClient;
        private ConnectionPolicy _connectionPolicy;
        private int _maximumConcurrency;
        private readonly ManualResetEvent _completeManualResetEvent = new ManualResetEvent(false);
        private WarehouseAvailableStockChangedReceiver _warehouseAvailableStockChangedReceiver;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken;
        private int _skuVariantMapBatchSize;

        public override void Run()
        {

             _cancellationToken = _cancellationTokenSource.Token;

            _warehouseAvailableStockChangedReceiver.ReceiveMessages(_cancellationToken);

            _completeManualResetEvent.WaitOne();
            
        }
        
        public override bool OnStart()
        {
            _telemetryClient = new TelemetryClient();
            _telemetryClient.InstrumentationKey = RoleEnvironment.GetConfigurationSettingValue("APPINSIGHTS_INSTRUMENTATIONKEY");
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount*12;

            _connectionPolicy = new ConnectionPolicy();
            _connectionPolicy.PreferredLocations.Add("North Europe"); // first preference
            _connectionPolicy.PreferredLocations.Add("West Europe"); // second preference 
            var sqServiceBusConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.StockQuantity");

            var docDbName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.DBName");
            var docDbsqColName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.DBCollectionName");
            var docDbsvColName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.SkuVariantMap.DBCollectionName");
            var docDbEndpointName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.EUN.AccountEndpoint");
            var docDbEndpointKey = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.EUN.AccountKey");

            _skuVariantMapBatchSize = Convert.ToInt16(CloudConfigurationManager.GetSetting("SkuVariantMapBatchSize"));

            _topicClient = TopicClient.CreateFromConnectionString(sqServiceBusConnectionString);

            _stockQuantityAggregateStore = new RegionStockAggregateDocumentStore(docDbName, docDbsqColName, docDbsvColName, docDbEndpointName, docDbEndpointKey, _connectionPolicy);

            InitialiseSkuVariantMapCacheManager();

            CreateServiceBusMessagingEntities();

            return base.OnStart();
        }

        private void InitialiseSkuVariantMapCacheManager()
        {
            _skuVariantCacheManager = new SkuVariantCacheManager();
            _skuVariantCacheManager.Initialise(_stockQuantityAggregateStore.GetSkuVariantMap(1000));
        }

        public override void OnStop()
        {
            // Close the connection to Service Bus Queue
            _cancellationTokenSource.Cancel();

            Thread.Sleep(1000 * _maximumConcurrency);

            _cancellationTokenSource.Dispose();

            _completeManualResetEvent.Set();

           //_subscriptionClient.Close();
           _warehouseAvailableStockChangedReceiver.Dispose();

            _topicClient.Close();

            _stockQuantityAggregateStore.Dispose();

            _telemetryClient.Flush();

            Thread.Sleep(1000);

            base.OnStop();
        }

        private void CreateServiceBusMessagingEntities()
        {
            var wasServiceBusConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock");
            var wasSubscriptionName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.StockQuantitySubscription");
            //var vccSubscriptionName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.VariantSkuMapSubscription");
            var wasTopicPath = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.TopicPath");

            var stockQuantityServiceBusConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.StockQuantity");
            var discardedEventsSubscriptionName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.StockQuantity.DiscardedEventsSubscription");
            var stockQuantityTopicPath = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.StockQuantity.TopicPath");

            var discardedEventSubscriptionClient = SubscriptionClient.CreateFromConnectionString(stockQuantityServiceBusConnectionString, stockQuantityTopicPath, discardedEventsSubscriptionName);

            // Set up Subscription Rules & Filters
            var isDiscardedRuleDescription = new RuleDescription("IsDiscarded");
            isDiscardedRuleDescription.Filter = new SqlFilter("IsDiscarded = 1");

            discardedEventSubscriptionClient.RemoveRule(RuleDescription.DefaultRuleName);
            discardedEventSubscriptionClient.RemoveRule(isDiscardedRuleDescription.Name);

            discardedEventSubscriptionClient.AddRule(isDiscardedRuleDescription);

            _maximumConcurrency = Convert.ToInt16(CloudConfigurationManager.GetSetting("MaximumConcurrency"));

            _warehouseAvailableStockChangedReceiver = new WarehouseAvailableStockChangedReceiver(_maximumConcurrency, wasServiceBusConnectionString, wasSubscriptionName, wasTopicPath, _topicClient, _stockQuantityAggregateStore, _skuVariantCacheManager, _telemetryClient);

            //_variantCopyCompletedReceiver = new VariantCopyCompletedReceiver(3, wasServiceBusConnectionString, vccSubscriptionName, wasTopicPath, _topicClient, _stockQuantityAggregateStore, _telemetryClient);

        }
    }
}
