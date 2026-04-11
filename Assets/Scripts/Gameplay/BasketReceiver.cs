using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BasketReceiver : MonoBehaviour
{
    public Action<BasketCollectible> OnItemDropped;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponentInParent<BasketCollectible>();
        if (item == null) return;

        OnItemDropped?.Invoke(item);
    }
}