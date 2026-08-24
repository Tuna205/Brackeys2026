using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player instance { get; private set; }

    public enum BeerTypes
    {
        White,
        Red,
        Dark
    }

    [SerializeField]
    private List<BeerTypes> beers = new()
    {
        BeerTypes.White,
        BeerTypes.White,
        BeerTypes.White,
        BeerTypes.Red,
        BeerTypes.Red,
        BeerTypes.Red,
        BeerTypes.Dark,
        BeerTypes.Dark,
        BeerTypes.Dark
    };

    public List<BeerTypes> Beers => beers;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("More than one Player component exists in the scene.", this);
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
