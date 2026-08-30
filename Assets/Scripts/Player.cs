using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const int MaximumBeers = 5;

    private static readonly int BartenderingParameter = Animator.StringToHash("bartendering");
    private static readonly int BartenderWalkWithPlateState = Animator.StringToHash("Base Layer.BartenderWalk_02");

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
    [SerializeField] private Animator playerAnimator = null;
    [SerializeField] private GameObject plateObject = null;

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

        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        if (playerAnimator == null)
        {
            Debug.LogError("Player needs an Animator for the bartendering parameter.", this);
        }

        if (plateObject == null)
        {
            plateObject = FindChildGameObject("SM_Plate");
        }

        if (plateObject == null)
        {
            Debug.LogError("Player needs an SM_Plate child for the bartender walking animation.", this);
        }
        else
        {
            plateObject.SetActive(false);
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

    private void LateUpdate()
    {
        if (playerAnimator == null || plateObject == null)
        {
            return;
        }

        AnimatorStateInfo currentState = playerAnimator.GetCurrentAnimatorStateInfo(0);
        bool shouldShowPlate = currentState.fullPathHash == BartenderWalkWithPlateState;

        if (playerAnimator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = playerAnimator.GetNextAnimatorStateInfo(0);
            shouldShowPlate |= nextState.fullPathHash == BartenderWalkWithPlateState;
        }

        if (plateObject.activeSelf != shouldShowPlate)
        {
            plateObject.SetActive(shouldShowPlate);
        }
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
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(BartenderingParameter, beers.Count > 0);
        }

        for (int i = 0; i < beerHolder.childCount; i++)
        {
            GameObject beerVisual = beerHolder.GetChild(i).gameObject;
            bool hasBeer = i < beers.Count && i < MaximumBeers;

            Renderer beerRenderer = GetBeerRenderer(beerVisual.transform);
            if (hasBeer && beerRenderer != null)
            {
                beerRenderer.sharedMaterial = GetBeerMaterial(beers[i]);
            }

            beerVisual.SetActive(hasBeer);
        }
    }

    private static Renderer GetBeerRenderer(Transform beerVisual)
    {
        foreach (Renderer beerRenderer in beerVisual.GetComponentsInChildren<Renderer>(true))
        {
            if (beerRenderer.enabled
                && (beerRenderer.sharedMaterial == null
                    || beerRenderer.sharedMaterial.name != "Foam"))
            {
                return beerRenderer;
            }
        }

        return null;
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

    private GameObject FindChildGameObject(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private void OnDisable()
    {
        if (plateObject != null)
        {
            plateObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
