using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockQuantity.Domain;

namespace StockQuantity.Data
{
    public interface IRegionStockAggregateStore : IDisposable
    {
        RegionStockDocument GetRegionStockByVariantId(int variantId);
        //RegionStockDocument GetRegionStockBySku(string sku);
        Task UpdateRegionStock(RegionStockDocument regionStock);
        Task CreateRegionStock(RegionStockDocument regionStock);
        Task CreateSkuVariantMap(SkuVariantMapDocument skuVariantMap);
        SkuVariantMapDocument GetSkuVariantMap(string sku);
        Task Persist(IRegionStockAggregate regionStockAggregate);
        IReadOnlyList<SkuVariantMapDocument> GetSkuVariantMap(int batchSize);
    }
}
