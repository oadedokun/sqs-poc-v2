using System;
using System.Collections.Generic;
using CacheManager.Core;

namespace StockQuantity.Data
{
    public class SkuVariantCacheManager
    {

        private ICacheManager<SkuVariantMapDocument> _skuVariantCacheManager;
        public void Initialise(IEnumerable<SkuVariantMapDocument> skuVariantMapDocuments)
        {
            _skuVariantCacheManager = CacheFactory.Build<SkuVariantMapDocument>(settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle()
                        .EnablePerformanceCounters()
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
            });


            foreach (var svm in skuVariantMapDocuments)
            {
                _skuVariantCacheManager.Add(svm.SKU, svm);
            }
        }

        public SkuVariantMapDocument GetItemByKey(string sku)
        {
            return _skuVariantCacheManager.Get<SkuVariantMapDocument>(sku);
        }

        public void AddItem(SkuVariantMapDocument item)
        {
            _skuVariantCacheManager.Add(item.SKU, item);
        }
    }
}