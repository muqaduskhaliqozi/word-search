using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Product catalog: the ids RuStoreBillingManager fetches prices for.
/// Ids must match the RuStore Console exactly. Create via
/// Assets > Create > RuStore > Product Config and assign it in the scene.
/// </summary>
[CreateAssetMenu(fileName = "RuStoreProductConfig", menuName = "RuStore/Product Config")]
public class RuStoreProductConfig : ScriptableObject
{
    public enum ProductType { Consumable, NonConsumable }

    [System.Serializable]
    public class ProductEntry
    {
        public string productId;
        public ProductType type;
    }

    public List<ProductEntry> products = new List<ProductEntry>();

    public List<string> AllProductIds()
    {
        var ids = new List<string>();
        foreach (var p in products) ids.Add(p.productId);
        return ids;
    }
}
