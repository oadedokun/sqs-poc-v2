using System;
using System.Collections.Generic;
using NUnit.Framework;
using StockQuantity.Domain;

namespace StockQuantity.UnitTests.Domain
{
    [TestFixture]
    public class RegionStockTests
    {
        [Test]
        public void Given_Region_Is_OutofStock_When_Applying_Stock_Changes_Due_To_ITL_Notifications_Should_Return_A_Stock_Status_Not_Equal_To_OutofStock()
        {
            // Arrange
            var regionStock = new RegionStockItem("UK", 0, new RegionStockItemStatus(10, StockStatus.OutOfStock), new List<WarehouseAvailableStockItem>(), DateTime.UtcNow);
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "123", 10, 0, 0, DateTime.Now);
                        
            // Act
            regionStock.ApplyStockChanges(warehouseAvailableStock);

            // Assert
            Assert.AreEqual(regionStock.Status.Value, StockStatus.InStock);
            Assert.AreEqual(regionStock.Quantity, 10);
            Assert.AreEqual(regionStock.Version, warehouseAvailableStock.Version);
        }

        [Test]
        public void Given_Region_Is_InStock_When_Applying_Low_In_Stock_StockChange_Should_Return_A_Stock_Status_Is_Equal_To_Low_In_Stock()
        {
            // Arrange
            var regionStock = new RegionStockItem("UK", 10, new RegionStockItemStatus(10, StockStatus.InStock), new List<WarehouseAvailableStockItem> { new WarehouseAvailableStockItem("FC01", "123", 10, 0, 0, DateTime.UtcNow) }, DateTime.UtcNow);
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "123", 9, 1, 0, DateTime.Now);

            // Act
            regionStock.ApplyStockChanges(warehouseAvailableStock);

            // Assert
            Assert.AreEqual(regionStock.Status.Value, StockStatus.LowInStock);
            Assert.AreEqual(regionStock.Quantity, 9);
            Assert.AreEqual(regionStock.Version, warehouseAvailableStock.Version);
        }

        [Test]
        public void Given_Region_Is_InStock_When_Applying_Out_Of_Stock_StockChange_Should_Return_A_Stock_Status_Is_Equal_To_Out_Of_Stock()
        {
            // Arrange
            var regionStock = new RegionStockItem("UK", 10, new RegionStockItemStatus(10, StockStatus.InStock), new List<WarehouseAvailableStockItem> { new WarehouseAvailableStockItem("FC01", "123", 10, 0, 0, DateTime.UtcNow) }, DateTime.UtcNow);
            var warehouseAvailableStock = new WarehouseAvailableStockItem("FC01", "123", 0, 0, 0, DateTime.Now);

            // Act
            regionStock.ApplyStockChanges(warehouseAvailableStock);

            // Assert
            Assert.AreEqual(regionStock.Status.Value, StockStatus.OutOfStock);
            Assert.AreEqual(regionStock.Quantity, 0);
            Assert.AreEqual(regionStock.Version, warehouseAvailableStock.Version);
        }
    }
}
