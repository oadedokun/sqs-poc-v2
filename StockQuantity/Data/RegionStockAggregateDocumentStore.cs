using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using StockQuantity.Domain;

namespace StockQuantity.Data
{
    public class RegionStockAggregateDocumentStore : IRegionStockAggregateStore
    {
        private readonly DocumentClient _documentClient;
        private readonly string _dbName;
        private readonly string _stockQuantityCollectionName;
        private readonly string _skuVariantMapCollectionName;
        private bool _disposed = false;
        
        public RegionStockAggregateDocumentStore(string dbName, string stockQuantityCollectionName, string skuVariantMapCollectionName, string endpointUri, string primaryKey, ConnectionPolicy connectionPolicy)
        {
            _documentClient = new DocumentClient(new Uri(endpointUri), primaryKey, connectionPolicy);
            _dbName = dbName;
            _stockQuantityCollectionName = stockQuantityCollectionName;
            _skuVariantMapCollectionName = skuVariantMapCollectionName;
            Initialize();
        }
        public RegionStockDocument GetRegionStockByVariantId(int variantId)
        {
            var response = _documentClient.CreateDocumentQuery<RegionStockDocument>(UriFactory.CreateDocumentCollectionUri(_dbName, _stockQuantityCollectionName))
                            .Where(sq => sq.Id == variantId.ToString()).AsEnumerable().SingleOrDefault();

            return response;
        }

        //public RegionStockDocument GetRegionStockBySku(string sku)
        //{
        //    var response = _documentClient.CreateDocumentQuery<RegionStockDocument>(
        //        UriFactory.CreateDocumentCollectionUri(_dbName, _stockQuantityCollectionName))
        //        .Where(rs => rs.WarehouseAvailableStocks.Any(x => x.Sku == sku))
        //        .AsEnumerable()
        //        .Where();

        //    return response.FirstOrDefault();
        //}

        public async Task CreateRegionStock(RegionStockDocument regionStock)
        {
            try
            {
                await _documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _stockQuantityCollectionName), regionStock);
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                var message = $"{de.StatusCode} error occurred: {de.Message}, Message: {baseException.Message}";
                Trace.TraceError(message);
                if (de.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new DuplicateResourceKeyException(message, de);
                }

                throw;
            }
            
        }

        public SkuVariantMapDocument GetSkuVariantMap(string sku)
        {
            var response = _documentClient.CreateDocumentQuery<SkuVariantMapDocument>(UriFactory.CreateDocumentCollectionUri(_dbName, _skuVariantMapCollectionName))
                            .Where(sv => sv.SKU == sku).AsEnumerable().SingleOrDefault();

           return response;
        }

        public async Task UpdateRegionStock(RegionStockDocument regionStock)
        {
            try
            {
                var requestOptions = new RequestOptions()
                {
                    AccessCondition = new AccessCondition()
                    {
                        Type = AccessConditionType.IfMatch,
                        Condition = regionStock.Version
                    }
                };
                await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _stockQuantityCollectionName, regionStock.Id), regionStock, requestOptions);
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                var message = $"{de.StatusCode} error occurred: {de.Message}, Message: {baseException.Message}";
                Trace.TraceError(message);
                if (de.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    throw new OptimisticConcurrencyException(message, de);
                }

                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(message, de);
                }

                throw;
            }
            
        }

        public async Task UpdateVariantSku(SkuVariantMapDocument skuVariantMap)
        {
            try
            {
                var requestOptions = new RequestOptions()
                {
                    AccessCondition = new AccessCondition()
                    {
                        Type = AccessConditionType.IfMatch,
                        Condition = skuVariantMap.Version
                    }
                };
                await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _skuVariantMapCollectionName, skuVariantMap.SKU), skuVariantMap, requestOptions);
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                var message = $"{de.StatusCode} error occurred: {de.Message}, Message: {baseException.Message}";
                Trace.TraceError(message);
                if (de.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    throw new OptimisticConcurrencyException(message, de);
                }

                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ResourceNotFoundException(message, de);
                }

                throw;
            }

        }

        public async Task Persist(IRegionStockAggregate regionStockAggregate)
        {
            if (string.IsNullOrEmpty(regionStockAggregate.Version))
            {
                await CreateRegionStock(RegionStockDocument.CreateFrom(regionStockAggregate));
            }
            else
            {
                await UpdateRegionStock(RegionStockDocument.CreateFrom(regionStockAggregate));
            }
        }

        public IReadOnlyList<SkuVariantMapDocument> GetSkuVariantMap(int batchSize)
        {
            var qry = $"SELECT TOP {batchSize} * FROM c ORDER BY id";
            return _documentClient.CreateDocumentQuery<SkuVariantMapDocument>(UriFactory.CreateDocumentCollectionUri(_dbName, _skuVariantMapCollectionName), qry)
                            .AsEnumerable().ToList();
        }

        private async void Initialize()
        {
            try
            {
                await _documentClient.OpenAsync().ConfigureAwait(false);
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
        }

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
                    _documentClient.Dispose();
                }

                _disposed = true;
            }
        }

        public async Task CreateSkuVariantMap(SkuVariantMapDocument skuVariantMap)
        {
            await _documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _skuVariantMapCollectionName), skuVariantMap);
        }
    }
}
