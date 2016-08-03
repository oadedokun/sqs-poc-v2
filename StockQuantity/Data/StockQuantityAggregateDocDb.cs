using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using StockQuantity.Domain;

namespace StockQuantity.Data
{
    public class StockQuantityAggregateDocDb : IStockQuantityAggregateStore
    {
        private readonly DocumentClient _documentClient;
        private readonly string _dbName;
        private readonly string _stockQuantityCollectionName;
        private readonly string _skuVariantMapCollectionName;
        private bool _disposed = false;
        
        public StockQuantityAggregateDocDb(string dbName, string stockQuantityCollectionName, string skuVariantMapCollectionName, string endpointUri, string primaryKey, ConnectionPolicy connectionPolicy)
        {
            _documentClient = new DocumentClient(new Uri(endpointUri), primaryKey, connectionPolicy);
            _dbName = dbName;
            _stockQuantityCollectionName = stockQuantityCollectionName;
            _skuVariantMapCollectionName = skuVariantMapCollectionName;
            Initialize();
        }
        public StockQuantity GetStockQuantityByVariantId(int variantId)
        {
            var response = _documentClient.CreateDocumentQuery<StockQuantity>(UriFactory.CreateDocumentCollectionUri(_dbName, _stockQuantityCollectionName))
                            .Where(sq => sq.VariantId == variantId).AsEnumerable().SingleOrDefault();

            return response;
        }
        public async Task CreateStockQuantity(StockQuantity stockQuantity)
        {
            await _documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _stockQuantityCollectionName), stockQuantity);
        }

        public SkuVariantMap GetSkuVariantMap(string sku)
        {
            var response = _documentClient.CreateDocumentQuery<SkuVariantMap>(UriFactory.CreateDocumentCollectionUri(_dbName, _skuVariantMapCollectionName))
                            .Where(sv => sv.SKU == sku).AsEnumerable().SingleOrDefault();

           return response;
        }

        public async Task UpdateStockQuantity(StockQuantity stockQuantity)
        {
            var requestOptions = new RequestOptions()
            {
                AccessCondition = new AccessCondition()
                {
                    Type = AccessConditionType.IfMatch,
                    Condition = stockQuantity.Version
                }
            };
            await _documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_dbName, _stockQuantityCollectionName, stockQuantity.Id), stockQuantity, requestOptions);
        }

        public async Task Persist(IStockQuantityAggregate stockQuantityAggregate)
        {
            if (string.IsNullOrEmpty(stockQuantityAggregate.Version))
            {
                await CreateStockQuantity(StockQuantity.CreateFrom(stockQuantityAggregate));
            }
            else
            {
                await UpdateStockQuantity(StockQuantity.CreateFrom(stockQuantityAggregate));
            }
        }

        public IReadOnlyList<SkuVariantMap> GetSkuVariantMap(int batchSize)
        {
            var qry = $"SELECT TOP {batchSize} * FROM c";
            return _documentClient.CreateDocumentQuery<SkuVariantMap>(UriFactory.CreateDocumentCollectionUri(_dbName, _skuVariantMapCollectionName), qry)
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

        public async Task CreateSkuVariantMap(SkuVariantMap skuVariantMap)
        {
            await _documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_dbName, _skuVariantMapCollectionName), skuVariantMap);
        }
    }
}
