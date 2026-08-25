using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const int MaximumBeers = 5;

    public static Player instance { get; private set; }

    public enum BeerTypes
    {
        White,
        Red,
        Dark
    }

    [Header("Beer Inventory")]
    [SerializeField] private List<BeerTypes> beers = new();
    [SerializeField] private Transform beerHolder = null;
    [SerializeField] private Material whiteBeerMaterial = null;
    [SerializeField] private Material redBeerMaterial = null;
    [SerializeField] private Material darkBeerMaterial = null;

    public IReadOnlyList<BeerTypes> Beers => beers;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("More than one Player component exists in the scene.", this);
            Destroy(this);
            return;
        }

        instance = this;

        if (beers.Count > MaximumBeers)
        {
            beers.RemoveRange(MaximumBeers, beers.Count - MaximumBeers);
        }

        if (beerHolder == null)
        {
            beerHolder = transform.Find("BeerHolder");
        }

        if (beerHolder == null)
        {
            Debug.LogError("Player needs a BeerHolder child.", this);
            return;
        }

        RefreshBeerHolder();
    }

    public bool AddBeer(BeerTypes beerType)
    {
        if (beers.Count >= MaximumBeers)
        {
            return false;
        }

        beers.Add(beerType);
        RefreshBeerHolder();
        return true;
    }

    public bool RemoveBeer(BeerTypes beerType)
    {
        if (!beers.Remove(beerType))
        {
            return false;
        }

        RefreshBeerHolder();
        return true;
    }

    public bool RemoveLastBeer()
    {
        if (beers.Count == 0)
        {
            return false;
        }

        beers.RemoveAt(beers.Count - 1);
        RefreshBeerHolder();
        return true;
    }

    private void RefreshBeerHolder()
    {
        for (int i = 0; i < beerHolder.childCount; i++)
        {
            GameObject beerVisual = beerHolder.GetChild(i).gameObject;
            bool hasBeer = i < beers.Count && i < MaximumBeers;

            if (hasBeer && beerVisual.TryGetComponent(out Renderer beerRenderer))
            {
                beerRenderer.sharedMaterial = GetBeerMaterial(beers[i]);
            }

            beerVisual.SetActive(hasBeer);
        }
    }

    private Material GetBeerMaterial(BeerTypes beerType)
    {
        return beerType switch
        {
            BeerTypes.White => whiteBeerMaterial,
            BeerTypes.Red => redBeerMaterial,
            BeerTypes.Dark => darkBeerMaterial,
            _ => null
        };
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
