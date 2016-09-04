using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ServiceBus.Messaging;
using StockQuantity.Data;

namespace StockQuantity.Worker.Messaging
{
    public class RestrictionAttributesAcceptedReceiver : MessageReceiver
    {
        public RestrictionAttributesAcceptedReceiver(int maximumConcurrency, string wasServiceBusConnectionString, string wasSubscriptionName, string wasTopicPath, TopicClient stockQuantityTopicClient, IRegionStockAggregateStore stockQuantityAggregateStore, SkuVariantCacheManager skuVariantCacheManager, TelemetryClient telemetryClient)
            : base(maximumConcurrency, wasServiceBusConnectionString, wasSubscriptionName, wasTopicPath, stockQuantityTopicClient, stockQuantityAggregateStore, skuVariantCacheManager, telemetryClient)
        {
        }
        public override void ReceiveMessages(CancellationToken token)
        {
            //SubscriptionClients = new List<SubscriptionClient>(MaximumConcurrency);
            //List<Task> receivers = new List<Task>(MaximumConcurrency);

            //for (int i = 0; i < MaximumConcurrency; i++)
            //{
            //    Task receiver = Task.Factory.StartNew(() =>
            //    {
            //        var subscriptionClient = SubscriptionClient.CreateFromConnectionString(WasServiceBusConnectionString, WasTopicPath, WasSubscriptionName);
            //        SubscriptionClients.Add(subscriptionClient);

            //        subscriptionClient.OnMessage(brokeredMessage =>
            //        {
            //            try
            //            {
            //                var message = brokeredMessage.GetBody<VariantCopyCompletedV1>();
            //                var variantCopyCompletedV1Handler =
            //                    new VariantCopyCompletedV1Handler(StockQuantityAggregateStore, TelemetryClient);
            //                variantCopyCompletedV1Handler.OnMessage(message);

            //                brokeredMessage.Complete();
            //            }
            //            catch (OptimisticConcurrencyException ex)
            //            {
            //                string err = ex.Message;
            //                if (ex.InnerException != null)
            //                {
            //                    err += " Inner Exception: " + ex.InnerException.Message;
            //                }
            //                Trace.TraceError(err, ex);

            //                //Retry transisent exceptions By Abandoning it
            //                brokeredMessage.Abandon();
            //                TelemetryClient.TrackEvent("OptimisticConcurrencyAbandonedForRetryEvents");
            //            }
            //            catch (ResourceNotFoundException ex)
            //            {
            //                string err = ex.Message;
            //                if (ex.InnerException != null)
            //                {
            //                    err += " Inner Exception: " + ex.InnerException.Message;
            //                }
            //                Trace.TraceError(err, ex);

            //                //Retry transisent exceptions By Abandoning it
            //                brokeredMessage.Abandon();
            //                TelemetryClient.TrackEvent("ResourceNotFoundAbandonedForRetryEvents");
            //            }
            //            catch (Exception ex)
            //            {
            //                string err = ex.Message;
            //                if (ex.InnerException != null)
            //                {
            //                    err += " Inner Exception: " + ex.InnerException.Message;
            //                }
            //                Trace.TraceError(err, ex);
            //                brokeredMessage.DeadLetter();
            //                TelemetryClient.TrackEvent("CriticallyFailedEvents");
            //            }
            //        });

            //    }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            //    receivers.Add(receiver);
            //}

            //Task.WhenAll(receivers).Wait(token);
        }
    }
}