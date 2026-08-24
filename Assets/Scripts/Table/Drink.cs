using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Drink : MonoBehaviour
{
    public enum DrinkState
    {
        Empty,
        Patient,
        Inpatient,
        Angry,
        WaitingForDrinks,
        DrinkingAndGivingInfo
    }

    public static event Action<Drink> SoldiersLeft;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions = null;

    [Header("Indicator")]
    [SerializeField] private GameObject drinkIndicator = null;
    [SerializeField] private Renderer indicatorRenderer = null;
    [SerializeField] private Material greenMaterial = null;
    [SerializeField] private Material yellowMaterial = null;
    [SerializeField] private Material redMaterial = null;

    [Header("Soldiers")]
    [SerializeField] private Transform soldierList = null;
    [SerializeField] private Material blackMaterial = null;

    [Header("Timing")]
    private float minimumRequestDelay = 0f; //20
    private float maximumRequestDelay = 10f; //80
    private float stateDuration = 15f;
    private float angryDuration = 30f;
    private float waitingForDrinksDuration = 30f;
    private float drinkingAndGivingInfoDuration = 30f;

    [Header("Penalty")]
    [SerializeField, Min(0f)] private float suspicionPenalty = 40f;

    public DrinkState State { get; private set; }
    public bool IsRequestingDrink => drinkIndicator != null && drinkIndicator.activeSelf;

    private Table table;
    private InputAction interactAction;
    private Coroutine requestLoop;
    private readonly List<GameObject> soldiers = new();
    private readonly List<Player.BeerTypes> requiredBeers = new();
    private readonly Dictionary<Renderer, Material[]> originalSoldierMaterials = new();
    private Player player;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("Drink needs an Input Action Asset.", this);
            enabled = false;
            return;
        }

        interactAction = inputActions.FindAction("Player/Jump", true).Clone();

        if (drinkIndicator == null)
        {
            Transform indicator = transform.Find("Drink_Indicator");
            if (indicator == null)
            {
                indicator = transform.Find("Drink");
            }

            if (indicator != null)
            {
                drinkIndicator = indicator.gameObject;
            }
        }

        if (indicatorRenderer == null && drinkIndicator != null)
        {
            indicatorRenderer = drinkIndicator.GetComponent<Renderer>();
        }

        if (drinkIndicator == null || indicatorRenderer == null)
        {
            Debug.LogError("Drink needs a child indicator with a Renderer.", this);
            enabled = false;
            return;
        }

        table = GetComponent<Table>();
        if (table == null)
        {
            Debug.LogError("Drink needs a Table component on the same GameObject.", this);
            enabled = false;
            return;
        }

        if (soldierList == null)
        {
            soldierList = transform.Find("SoldierList");
        }

        for (int i = 0; i < soldierList.childCount; i++)
        {
            GameObject soldier = soldierList.GetChild(i).gameObject;
            soldiers.Add(soldier);

            foreach (Renderer soldierRenderer in soldier.GetComponentsInChildren<Renderer>(true))
            {
                if (soldierRenderer.transform.name != "Beer")
                {
                    originalSoldierMaterials.Add(soldierRenderer, soldierRenderer.sharedMaterials);
                }
            }
        }

        SetState(DrinkState.Empty);
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void Start()
    {
        if (Player.instance == null)
        {
            Debug.LogError("Drink could not find the Player singleton.", this);
            enabled = false;
            return;
        }

        GameObject playerObject = Player.instance.gameObject;
        player = playerObject.GetComponent<Player>();
        requestLoop = StartCoroutine(DrinkRequestLoop());
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteract;
            interactAction.Disable();
        }

        if (drinkIndicator != null)
        {
            drinkIndicator.SetActive(false);
        }

        DisableAllSoldiers();
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!table.IsPlayerInside)
        {
            return;
        }

        if (State == DrinkState.WaitingForDrinks)
        {
            TryDeliverDrinks();
        }
        else if (State != DrinkState.Empty && State != DrinkState.DrinkingAndGivingInfo)
        {
            EnterWaitingForDrinksState();
        }
    }

    private IEnumerator DrinkRequestLoop()
    {
        while (true)
        {
            SetState(DrinkState.Empty);

            float requestDelay = UnityEngine.Random.Range(minimumRequestDelay, maximumRequestDelay);
            yield return new WaitForSeconds(requestDelay);

            SetState(DrinkState.Patient);
            EnableRandomSoldiers();

            yield return new WaitForSeconds(stateDuration);
            SetState(DrinkState.Inpatient);

            yield return new WaitForSeconds(stateDuration);
            SetState(DrinkState.Angry);

            yield return new WaitForSeconds(angryDuration);
            CauseSoldiersLeaving();
        }
    }

    private void EnterWaitingForDrinksState()
    {
        if (requestLoop != null)
        {
            StopCoroutine(requestLoop);
        }

        SetState(DrinkState.WaitingForDrinks);
        EnableBeersForActiveSoldiers();
        requestLoop = StartCoroutine(WaitForDrinks());
    }

    private IEnumerator WaitForDrinks()
    {
        yield return new WaitForSeconds(waitingForDrinksDuration);

        if (State != DrinkState.WaitingForDrinks)
        {
            yield break;
        }

        requestLoop = null;
        CauseSoldiersLeaving();
        EnterEmptyState();
    }

    private void TryDeliverDrinks()
    {
        List<Player.BeerTypes> availableBeers = new(player.Beers);
        foreach (Player.BeerTypes requiredBeer in requiredBeers)
        {
            int beerIndex = availableBeers.IndexOf(requiredBeer);
            if (beerIndex < 0)
            {
                return;
            }

            availableBeers.RemoveAt(beerIndex);
        }

        foreach (Player.BeerTypes requiredBeer in requiredBeers)
        {
            player.Beers.Remove(requiredBeer);
        }

        if (requestLoop != null)
        {
            StopCoroutine(requestLoop);
        }

        SetState(DrinkState.DrinkingAndGivingInfo);
        requestLoop = StartCoroutine(DrinkAndGiveInfo());
    }

    private IEnumerator DrinkAndGiveInfo()
    {
        yield return new WaitForSeconds(drinkingAndGivingInfoDuration);

        if (State != DrinkState.DrinkingAndGivingInfo)
        {
            yield break;
        }

        requestLoop = null;
        if (UnityEngine.Random.value < 0.5f)
        {
            EnterWaitingForDrinksState();
        }
        else
        {
            EnterEmptyState();
        }
    }

    private void EnterEmptyState()
    {
        if (requestLoop != null)
        {
            StopCoroutine(requestLoop);
        }

        SetState(DrinkState.Empty);
        requestLoop = StartCoroutine(DrinkRequestLoop());
    }

    private void SetState(DrinkState newState)
    {
        if (State == DrinkState.DrinkingAndGivingInfo && newState != DrinkState.DrinkingAndGivingInfo)
        {
            RestoreSoldierMaterials();
        }

        State = newState;

        if (newState == DrinkState.Empty)
        {
            drinkIndicator.SetActive(false);
            requiredBeers.Clear();
            DisableAllSoldiers();
            return;
        }

        if (newState == DrinkState.WaitingForDrinks)
        {
            drinkIndicator.SetActive(false);
            return;
        }

        if (newState == DrinkState.DrinkingAndGivingInfo)
        {
            drinkIndicator.SetActive(false);
            DisableAllBeers();
            SetActiveSoldierMaterials(greenMaterial);
            return;
        }

        Material material = newState switch
        {
            DrinkState.Patient => greenMaterial,
            DrinkState.Inpatient => yellowMaterial,
            DrinkState.Angry => redMaterial,
            _ => null
        };

        if (material == null)
        {
            Debug.LogError($"Drink is missing the material for its {newState} state.", this);
            return;
        }

        indicatorRenderer.sharedMaterial = material;
        drinkIndicator.SetActive(true);
    }

    private void EnableRandomSoldiers()
    {
        DisableAllSoldiers();

        int maximumSoldiers = Mathf.Min(6, soldiers.Count);
        int soldiersToEnable = UnityEngine.Random.Range(1, maximumSoldiers + 1);

        List<GameObject> availableSoldiers = new(soldiers);
        for (int i = 0; i < soldiersToEnable; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(i, availableSoldiers.Count);
            (availableSoldiers[i], availableSoldiers[selectedIndex]) =
                (availableSoldiers[selectedIndex], availableSoldiers[i]);
            availableSoldiers[i].SetActive(true);
        }
    }

    private void DisableAllSoldiers()
    {
        foreach (GameObject soldier in soldiers)
        {
            Transform beer = soldier.transform.Find("Beer");
            if (beer != null)
            {
                beer.gameObject.SetActive(false);
            }

            soldier.SetActive(false);
        }
    }

    private void EnableBeersForActiveSoldiers()
    {
        Material[] beerMaterials = { yellowMaterial, redMaterial, blackMaterial };
        Player.BeerTypes[] beerTypes =
        {
            Player.BeerTypes.White,
            Player.BeerTypes.Red,
            Player.BeerTypes.Dark
        };

        requiredBeers.Clear();

        foreach (GameObject soldier in soldiers)
        {
            if (!soldier.activeSelf)
            {
                continue;
            }

            Transform beer = soldier.transform.Find("Beer");
            if (beer == null || !beer.TryGetComponent(out Renderer beerRenderer))
            {
                Debug.LogError($"{soldier.name} needs a Beer child with a Renderer.", soldier);
                continue;
            }

            int beerIndex = UnityEngine.Random.Range(0, beerMaterials.Length);
            beerRenderer.sharedMaterial = beerMaterials[beerIndex];
            beer.gameObject.SetActive(true);
            requiredBeers.Add(beerTypes[beerIndex]);
        }
    }

    private void DisableAllBeers()
    {
        foreach (GameObject soldier in soldiers)
        {
            Transform beer = soldier.transform.Find("Beer");
            if (beer != null)
            {
                beer.gameObject.SetActive(false);
            }
        }
    }

    private void SetActiveSoldierMaterials(Material material)
    {
        foreach (KeyValuePair<Renderer, Material[]> soldierRenderer in originalSoldierMaterials)
        {
            if (!soldierRenderer.Key.gameObject.activeInHierarchy)
            {
                continue;
            }

            Material[] materials = new Material[soldierRenderer.Key.sharedMaterials.Length];
            Array.Fill(materials, material);
            soldierRenderer.Key.sharedMaterials = materials;
        }
    }

    private void RestoreSoldierMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> soldierRenderer in originalSoldierMaterials)
        {
            soldierRenderer.Key.sharedMaterials = soldierRenderer.Value;
        }
    }

    private void CauseSoldiersLeaving()
    {
        SoldiersLeft?.Invoke(this);

        if (Suspition.instance == null)
        {
            Debug.LogError("Drink could not find the Suspition singleton.", this);
            return;
        }

        Suspition.instance.Add(suspicionPenalty);
    }

    private void OnValidate()
    {
        maximumRequestDelay = Mathf.Max(minimumRequestDelay, maximumRequestDelay);
    }
}
