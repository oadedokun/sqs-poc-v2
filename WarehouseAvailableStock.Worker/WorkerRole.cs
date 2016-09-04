using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Azure.Documents.Client;
using StockQuantity.Data;

namespace WarehouseAvailableStock.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        
        private volatile bool _onStopCalled;
        private volatile bool _returnedFromRunMethod;
        private WarehouseAvailableStockChangedPublisher _publisher;
        private IRegionStockAggregateStore _stockQuantityAggregateStore;
        private TopicClient _topicClient;
        private ConnectionPolicy _connectionPolicy;
        private TelemetryClient _telemetryClient;
        private int _skuVariantMapBatchSize;
        public override void Run()
        {
            Trace.WriteLine("Starting processing of messages");
            
            while (true)
            {
                try
                {
                    if (_onStopCalled)
                    {
                        Trace.TraceInformation("onStopCalled Warehouse Available Stock");
                        _returnedFromRunMethod = true;
                        return;
                    }
                    
                   _publisher.PublishWarehouseAvailableStockChanged();
                }
                catch (Exception ex)
                {
                    string err = ex.Message;
                    if (ex.InnerException != null)
                    {
                        err += " Inner Exception: " + ex.InnerException.Message;
                    }
                    Trace.TraceError(err, ex);
                }
            }
        }

        public override bool OnStart()
        {
            _telemetryClient = new TelemetryClient();
            _telemetryClient.InstrumentationKey = RoleEnvironment.GetConfigurationSettingValue("APPINSIGHTS_INSTRUMENTATIONKEY");
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 12;

            _connectionPolicy = new ConnectionPolicy();
            _connectionPolicy.PreferredLocations.Add("North Europe"); // first preference
            _connectionPolicy.PreferredLocations.Add("West Europe"); // second preference 

            var docDbName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.DBName");
            var docDbsqColName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.DBCollectionName");
            var docDbsvColName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.SkuVariantMap.DBCollectionName");
            var docDbEndpointName = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.EUN.AccountEndpoint");
            var docDbEndpointKey = CloudConfigurationManager.GetSetting("Microsoft.DocumentDB.StockQuantity.EUN.AccountKey");
            _skuVariantMapBatchSize = Convert.ToInt16(CloudConfigurationManager.GetSetting("SkuVariantMapBatchSize"));
            int publishBatchSize = Convert.ToInt16(CloudConfigurationManager.GetSetting("PublishBatchSize"));
            CreateServiceBusMessagingEntities();
            _stockQuantityAggregateStore = new RegionStockAggregateDocumentStore(docDbName, docDbsqColName, docDbsvColName, docDbEndpointName, docDbEndpointKey, _connectionPolicy);
            _publisher = new WarehouseAvailableStockChangedPublisher(_topicClient, _stockQuantityAggregateStore, _skuVariantMapBatchSize, publishBatchSize, _telemetryClient);

            return base.OnStart();
        }

        public override void OnStop()
        {
            _onStopCalled = true;
            while (_returnedFromRunMethod == false)
            {
                Thread.Sleep(1000);
            }

            _topicClient.Close();
            _stockQuantityAggregateStore.Dispose();

            _telemetryClient.Flush();

            Thread.Sleep(1000);
            base.OnStop();
        }

        private void CreateServiceBusMessagingEntities()
        {
            var wasServiceBusConnectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock");
            var wasTopicPath = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.TopicPath");
            var stockQuantitySubscriptionName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.StockQuantitySubscription");
            var variantSkuMapSubscriptionName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.VariantSkuMapSubscription");
            //var restrictionSubscriptionName = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString.WarehouseAvailableStock.RestrictionSubscriptionName");
            _topicClient = TopicClient.CreateFromConnectionString(wasServiceBusConnectionString, wasTopicPath);

            // Set up Subscription Rules & Filters
            var wasRuleDescription = new RuleDescription("IsWarehouseStockChange");
            wasRuleDescription.Filter = new SqlFilter("IsWarehouseStockChange = 1");

            var variantSkuRuleDescription = new RuleDescription("IsVariantSku");
            variantSkuRuleDescription.Filter = new SqlFilter("IsVariantSku = 1");

            //var rextdRuleDescription = new RuleDescription("IsRestriction");
            //rextdRuleDescription.Filter = new SqlFilter("IsRestriction = 1");

            var stockQuantitySubscriptionClient = SubscriptionClient.CreateFromConnectionString(wasServiceBusConnectionString, wasTopicPath, stockQuantitySubscriptionName);
            stockQuantitySubscriptionClient.RemoveRule(RuleDescription.DefaultRuleName);
            stockQuantitySubscriptionClient.RemoveRule(wasRuleDescription.Name);

            stockQuantitySubscriptionClient.AddRule(wasRuleDescription);

            var variantSkuMapSubscriptionClient = SubscriptionClient.CreateFromConnectionString(wasServiceBusConnectionString, wasTopicPath, variantSkuMapSubscriptionName);
            variantSkuMapSubscriptionClient.RemoveRule(RuleDescription.DefaultRuleName);
            variantSkuMapSubscriptionClient.RemoveRule(variantSkuRuleDescription.Name);

            variantSkuMapSubscriptionClient.AddRule(variantSkuRuleDescription);

            //var restrictionSubscriptionClient = SubscriptionClient.CreateFromConnectionString(wasServiceBusConnectionString, wasTopicPath, restrictionSubscriptionName);
            //restrictionSubscriptionClient.RemoveRule(RuleDescription.DefaultRuleName);
            //restrictionSubscriptionClient.RemoveRule(rextdRuleDescription.Name);

            //restrictionSubscriptionClient.AddRule(rextdRuleDescription);

        }
    }
}
