using System;
using Microsoft.Azure.Documents.Client;
using NUnit.Framework;
using System.Configuration;
using System.Linq;
using Castle.Core.Internal;
using Microsoft.Azure.Documents;
using StockQuantity.Data;
using StockQuantity.Domain;

namespace StockQuantity.UnitTests.Data
{
    [TestFixture]
    public class StockQuantityDocumentDbTests
    {
        private RegionStockAggregateDocumentStore _stockQuantityDocumentDb;
        private string _dbName;
        private string _dbStockQuantityCollectionName;
        private string _dbSkuVariantMapCollectionName;
        private string _accountEndPoint;
        private string _accountKey;
        private ConnectionPolicy _connectionPolicy;

        [SetUp]
        public void SetUp()
        {
            _dbName = ConfigurationManager.AppSettings["Microsoft.DocumentDB.StockQuantity.DBName"];
            _dbStockQuantityCollectionName = ConfigurationManager.AppSettings["Microsoft.DocumentDB.StockQuantity.DBCollectionName"];
            _dbSkuVariantMapCollectionName = ConfigurationManager.AppSettings["Microsoft.DocumentDB.SkuVariantMap.DBCollectionName"];

            _accountEndPoint = ConfigurationManager.AppSettings["Microsoft.DocumentDB.StockQuantity.EUN.AccountEndpoint"];
            _accountKey = ConfigurationManager.AppSettings["Microsoft.DocumentDB.StockQuantity.EUN.AccountKey"];

            _connectionPolicy = new ConnectionPolicy();
            _connectionPolicy.PreferredLocations.Add("North Europe"); // first preference
            _connectionPolicy.PreferredLocations.Add("West Europe"); // second preference 
            _stockQuantityDocumentDb = new RegionStockAggregateDocumentStore(_dbName, _dbStockQuantityCollectionName, _dbSkuVariantMapCollectionName, _accountEndPoint, _accountKey, _connectionPolicy);
        }

        [Test]
        public void ShouldCreateStockQuantityDocument()
        {
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "ABC", 20, 0, 0, DateTime.UtcNow);
            var stockQuantity = new RegionStockDocument(1234, new[] { warehouseAvailableStock }, new[] { new RegionStockItem("US", 20, new RegionStockItemStatus(10, StockStatus.InStock), new[] { warehouseAvailableStock }, DateTime.UtcNow) });
            _stockQuantityDocumentDb.CreateRegionStock(stockQuantity).Wait();
        }

        [Test]
        public void ShouldCreateSkuVariantMap()
        {
            var skuVariantMap = new SkuVariantMapDocument
            {
                SKU = "ABC",
                VariantId = 1
            };
            
            _stockQuantityDocumentDb.CreateSkuVariantMap(skuVariantMap).Wait();
        }

        [Test]
        public void ShouldCreateSkuVariantMapDocument()
        {
            var variantId = new Random(200);
            var sku = new Random(300);

            for (var i = 0; i <= 10000; i++)
            {
                var vId = variantId.Next();
                var sId = sku.Next();
                var skuVariantMap = new SkuVariantMapDocument
                {
                    VariantId = vId,
                    SKU = sId.ToString()
                };

                try
                {
                    _stockQuantityDocumentDb.CreateSkuVariantMap(skuVariantMap).Wait();
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
        }

        [Test]
        public void ShouldReplaceSkuVariantMap()
        {
            var result1 = _stockQuantityDocumentDb.GetSkuVariantMap(1);
            Assert.IsNotNull(result1.First());
            var result = result1.First();
            result.VariantId = 12;
            _stockQuantityDocumentDb.UpdateVariantSku(result).Wait();
            var result2 = _stockQuantityDocumentDb.GetSkuVariantMap("ABC");

            Assert.AreEqual(result.VariantId, result2.VariantId);
        }

        [Test]
        public void ShouldReplaceStockQuantity()
        {
            var pickable = new Random().Next(1, 200);
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC02", "ABC", pickable, 0, 0, DateTime.UtcNow);
            var result1 = _stockQuantityDocumentDb.GetRegionStockByVariantId(123);

            var list = result1.WarehouseAvailableStocks.ToList();
            list.Add(warehouseAvailableStock);


            result1.WarehouseAvailableStocks = list.AsEnumerable();

            result1.WarehouseAvailableStocks
                .ForEach(x => x.ApplyStockChanges(warehouseAvailableStock));
            result1.RegionStocks
                .ForEach(x => x.ApplyStockChanges(warehouseAvailableStock));

            _stockQuantityDocumentDb.UpdateRegionStock(result1).Wait();

            var result2 = _stockQuantityDocumentDb.GetRegionStockByVariantId(123);
                                      
            Assert.AreEqual(result1.WarehouseAvailableStocks.FirstOrDefault().Pickable, result2.WarehouseAvailableStocks.FirstOrDefault().Pickable);
        }

        [Test]
        public void ReplaceStockQuantity_ConcurrentWrites_ShouldThrowException()
        {
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "ABC", 25, 0, 0, DateTime.UtcNow);
            var result1 = _stockQuantityDocumentDb.GetRegionStockByVariantId(123);
            var result2 = _stockQuantityDocumentDb.GetRegionStockByVariantId(123);

            result1.WarehouseAvailableStocks
            .ForEach(x => x.ApplyStockChanges(warehouseAvailableStock));

            result1.RegionStocks
                .ForEach(x => x.ApplyStockChanges(warehouseAvailableStock));

            result2.WarehouseAvailableStocks
            .ForEach(x => x.ApplyStockChanges(warehouseAvailableStock));

            result2.RegionStocks
                .ForEach(x => x.ApplyStockChanges(warehouseAvailableStock));

            _stockQuantityDocumentDb.UpdateRegionStock(result1).Wait();

            Assert.Throws<OptimisticConcurrencyException>(() => _stockQuantityDocumentDb.UpdateRegionStock(result2).Wait());
        }

        [Test]
        public void WhenCreatingStockQuantityAndDuplicateExistsThenShouldThrowException()
        {
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "ABC", 20, 0, 0, DateTime.UtcNow);
            var stockQuantity = new RegionStockDocument(123, new[] { warehouseAvailableStock }, new[] { new RegionStockItem("US", 20, new RegionStockItemStatus(10, StockStatus.InStock), new[] { warehouseAvailableStock }, DateTime.UtcNow) });
            Assert.Throws<DocumentClientException>(() => _stockQuantityDocumentDb.CreateRegionStock(stockQuantity).Wait());
        }

        [Test]
        public void ShouldGetStockQuantity()
        {
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "ABC", 20, 0, 0, DateTime.UtcNow);
            var expectedStockQuantity = new RegionStockDocument(123, new[] { warehouseAvailableStock }, new[] { new RegionStockItem("US", 20, new RegionStockItemStatus(10, StockStatus.InStock), new[] { warehouseAvailableStock }, DateTime.UtcNow) });

            var actualStockQuantity = _stockQuantityDocumentDb.GetRegionStockByVariantId(expectedStockQuantity.VariantId);
            
            Assert.IsNotNull(actualStockQuantity);
            Assert.AreEqual(expectedStockQuantity.VariantId, actualStockQuantity.VariantId);
            Assert.AreEqual(expectedStockQuantity.Id, actualStockQuantity.Id);
        }

        //[Test]
        //public void ShouldGetStockQuantityBySku()
        //{
        //    var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "ABC", 20, 0, 0, DateTime.UtcNow);
        //    var expectedStockQuantity = new StockQuantity.Data.RegionStockDocument(123, new[] { warehouseAvailableStock }, new[] { new RegionStockItem("US", 20, 0, new RegionStockItemStatus(10, StockStatus.InStock), new[] { warehouseAvailableStock }, DateTime.UtcNow) });
        //    StockQuantity.Data.RegionStockDocument actualStockQuantity = null;

        //    actualStockQuantity = _stockQuantityDocumentDb.GetRegionStockBySku("ABC");

        //    Assert.IsNotNull(actualStockQuantity);
        //    Assert.AreEqual(expectedStockQuantity.VariantId, actualStockQuantity.VariantId);
        //    Assert.AreEqual(expectedStockQuantity.Id, actualStockQuantity.Id);
        //}

        [Test]
        public void ShouldGetSkuVariantMap()
        {
            //var sku = "974608667";
            var skuVariantMap = _stockQuantityDocumentDb.GetSkuVariantMap(20000);

            Assert.IsNotNull(skuVariantMap);
            //Assert.AreEqual(sku, skuVariantMap.SKU);
        }

        [Test]
        public void ShouldGetBatchedSkuVariantMap()
        {
            var skuVariantMaps = _stockQuantityDocumentDb.GetSkuVariantMap(500);

            Assert.IsNotNull(skuVariantMaps);
            Assert.AreEqual(500, skuVariantMaps.Count);
        }

        [TearDown]
        public void TearDown()
        {
            _stockQuantityDocumentDb?.Dispose();
        }
    }
}
