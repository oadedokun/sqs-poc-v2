using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ServiceBus.Messaging;
using StockQuantity.Contracts.Events;
using StockQuantity.Data;
using StockQuantity.Domain;

namespace StockQuantity.Worker.Messaging
{
    public class WarehouseAvailableStockChangedReceiver : MessageReceiver
    {
        public WarehouseAvailableStockChangedReceiver(int maximumConcurrency, string wasServiceBusConnectionString, string wasSubscriptionName, string wasTopicPath, TopicClient stockQuantityTopicClient, IRegionStockAggregateStore stockQuantityAggregateStore, SkuVariantCacheManager skuVariantCacheManager, TelemetryClient telemetryClient)
            : base(maximumConcurrency, wasServiceBusConnectionString, wasSubscriptionName, wasTopicPath, stockQuantityTopicClient, stockQuantityAggregateStore, skuVariantCacheManager, telemetryClient)
        {
        }

        public override void ReceiveMessages(CancellationToken token)
        {
            SubscriptionClients = new List<SubscriptionClient>(MaximumConcurrency);
            List<Task> receivers = new List<Task>(MaximumConcurrency);

            for (int i = 0; i < MaximumConcurrency; i++)
            {
                Task receiver = Task.Factory.StartNew(() =>
                {
                    var subscriptionClient = SubscriptionClient.CreateFromConnectionString(WasServiceBusConnectionString, WasTopicPath, WasSubscriptionName);
                    SubscriptionClients.Add(subscriptionClient);

                    subscriptionClient.OnMessage(brokeredMessage =>
                    {
                        try
                        {
                            var message = brokeredMessage.GetBody<WarehouseAvailableStockChangedV1>();
                            var warehouseAvailableStockChangedV1Handler =
                                new WarehouseAvailableStockChangedV1Handler(StockQuantityTopicClient, StockQuantityAggregateStore, SkuVariantCacheManager, TelemetryClient);
                            warehouseAvailableStockChangedV1Handler.OnMessage(message);

                            brokeredMessage.Complete();
                        }
                        catch (StaleWarehouseAvailableStockChangedException ex)
                        {
                            string err = ex.Message;
                            if (ex.InnerException != null)
                            {
                                err += " Inner Exception: " + ex.InnerException.Message;
                            }
                            Trace.TraceError(err, ex);

                            //Discard Stale Message By Completing it
                            brokeredMessage.Complete();
                            brokeredMessage.Properties.Add("IsDiscarded", 1);
                            StockQuantityTopicClient.Send(brokeredMessage);
                            TelemetryClient.TrackEvent("DiscardedEvents");
                        }
                        catch (OptimisticConcurrencyException ex)
                        {
                            string err = ex.Message;
                            if (ex.InnerException != null)
                            {
                                err += " Inner Exception: " + ex.InnerException.Message;
                            }
                            Trace.TraceError(err, ex);

                            //Retry transisent exceptions By Abandoning it
                            brokeredMessage.Abandon();
                            TelemetryClient.TrackEvent("OptimisticConcurrencyAbandonedForRetryEvents");
                        }
                        catch (ResourceNotFoundException ex)
                        {
                            string err = ex.Message;
                            if (ex.InnerException != null)
                            {
                                err += " Inner Exception: " + ex.InnerException.Message;
                            }
                            Trace.TraceError(err, ex);

                            //Retry transisent exceptions By Abandoning it
                            brokeredMessage.Abandon();
                            TelemetryClient.TrackEvent("ResourceNotFoundAbandonedForRetryEvents");
                        }
                        catch (Exception ex)
                        {
                            string err = ex.Message;
                            if (ex.InnerException != null)
                            {
                                err += " Inner Exception: " + ex.InnerException.Message;
                            }
                            Trace.TraceError(err, ex);
                            brokeredMessage.DeadLetter();
                            TelemetryClient.TrackEvent("CriticallyFailedEvents");
                        }
                    });

                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                receivers.Add(receiver);
            }

            Task.WhenAll(receivers).Wait(token);

        }
    }
}