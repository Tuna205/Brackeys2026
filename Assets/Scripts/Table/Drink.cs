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
        Angry,
        WaitingForDrinks,
        WaitingForDrinksAngry,
        DrinkingAndGivingInfo
    }

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

    private const float MinimumRequestDelay = 0f;
    private const float MaximumRequestDelay = 10f;
    private const float PatientDuration = 15f;
    private const float AngryDuration = 30f;
    private const float WaitingForDrinksDuration = 30f;
    private const float WaitingForDrinksAngryDuration = 15f;
    private const float DrinkingAndGivingInfoDuration = 30f;

    private float suspicionPenalty = 40f;

    public DrinkState State { get; private set; }

    private Table table;
    private InputAction interactAction;
    private Coroutine stateTimer;
    private readonly List<GameObject> soldiers = new();
    private readonly List<Player.BeerTypes> requiredBeers = new();
    private readonly Dictionary<Renderer, Material[]> originalSoldierMaterials = new();
    private Player player;
    private bool hasEnteredState;
    private bool stateMachineIsRunning;

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

        TransitionTo(DrinkState.Empty);
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

        player = Player.instance;
        stateMachineIsRunning = true;
        TransitionTo(DrinkState.Empty);
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

        SetActiveBeerScale(Vector3.one * 0.2f);
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

        switch (State)
        {
            case DrinkState.Patient:
            case DrinkState.Angry:
                TransitionTo(DrinkState.WaitingForDrinks);
                break;

            case DrinkState.WaitingForDrinks:
            case DrinkState.WaitingForDrinksAngry:
                TryDeliverDrinks();
                break;
        }
    }

    private void TransitionTo(DrinkState nextState)
    {
        if (hasEnteredState)
        {
            ExitState(State);
        }

        State = nextState;
        hasEnteredState = true;
        EnterState(State);
    }

    private void EnterState(DrinkState state)
    {
        switch (state)
        {
            case DrinkState.Empty:
                OnEnterEmpty();
                break;
            case DrinkState.Patient:
                OnEnterPatient();
                break;
            case DrinkState.Angry:
                OnEnterAngry();
                break;
            case DrinkState.WaitingForDrinks:
                OnEnterWaitingForDrinks();
                break;
            case DrinkState.WaitingForDrinksAngry:
                OnEnterWaitingForDrinksAngry();
                break;
            case DrinkState.DrinkingAndGivingInfo:
                OnEnterDrinkingAndGivingInfo();
                break;
        }
    }

    private void ExitState(DrinkState state)
    {
        switch (state)
        {
            case DrinkState.Empty:
                OnExitEmpty();
                break;
            case DrinkState.Patient:
                OnExitPatient();
                break;
            case DrinkState.Angry:
                OnExitAngry();
                break;
            case DrinkState.WaitingForDrinks:
                OnExitWaitingForDrinks();
                break;
            case DrinkState.WaitingForDrinksAngry:
                OnExitWaitingForDrinksAngry();
                break;
            case DrinkState.DrinkingAndGivingInfo:
                OnExitDrinkingAndGivingInfo();
                break;
        }
    }

    private void OnEnterEmpty()
    {
        drinkIndicator.SetActive(false);
        requiredBeers.Clear();
        DisableAllSoldiers();

        if (stateMachineIsRunning)
        {
            float requestDelay = UnityEngine.Random.Range(MinimumRequestDelay, MaximumRequestDelay);
            StartStateTimer(requestDelay, () => TransitionTo(DrinkState.Patient));
        }
    }

    private void OnExitEmpty()
    {
        StopStateTimer();
    }

    private void OnEnterPatient()
    {
        ShowDrinkIndicator(greenMaterial);
        EnableRandomSoldiers();
        StartStateTimer(PatientDuration, () => TransitionTo(DrinkState.Angry));
    }

    private void OnExitPatient()
    {
        StopStateTimer();
    }

    private void OnEnterAngry()
    {
        ShowDrinkIndicator(redMaterial);
        StartStateTimer(AngryDuration, LeaveAndReturnToEmpty);
    }

    private void OnExitAngry()
    {
        StopStateTimer();
    }

    private void OnEnterWaitingForDrinks()
    {
        drinkIndicator.SetActive(false);
        EnableBeersForActiveSoldiers();
        StartStateTimer(
            WaitingForDrinksDuration,
            () => TransitionTo(DrinkState.WaitingForDrinksAngry));
    }

    private void OnExitWaitingForDrinks()
    {
        StopStateTimer();
    }

    private void OnEnterWaitingForDrinksAngry()
    {
        drinkIndicator.SetActive(false);
        SetActiveBeerScale(Vector3.one * 0.4f);
        StartStateTimer(WaitingForDrinksAngryDuration, LeaveAndReturnToEmpty);
    }

    private void OnExitWaitingForDrinksAngry()
    {
        StopStateTimer();
        SetActiveBeerScale(Vector3.one);
    }

    private void OnEnterDrinkingAndGivingInfo()
    {
        drinkIndicator.SetActive(false);
        DisableAllBeers();
        SetActiveSoldierMaterials(greenMaterial);
        StartStateTimer(DrinkingAndGivingInfoDuration, FinishDrinking);
    }

    private void OnExitDrinkingAndGivingInfo()
    {
        StopStateTimer();
        RestoreSoldierMaterials();
    }

    private void StartStateTimer(float duration, Action onFinished)
    {
        StopStateTimer();
        stateTimer = StartCoroutine(RunStateTimer(duration, onFinished));
    }

    private void StopStateTimer()
    {
        if (stateTimer == null)
        {
            return;
        }

        StopCoroutine(stateTimer);
        stateTimer = null;
    }

    private IEnumerator RunStateTimer(float duration, Action onFinished)
    {
        yield return new WaitForSeconds(duration);
        stateTimer = null;
        onFinished();
    }

    private void LeaveAndReturnToEmpty()
    {
        CauseSoldiersLeaving();
        TransitionTo(DrinkState.Empty);
    }

    private void FinishDrinking()
    {
        DrinkState nextState = UnityEngine.Random.value < 0.5f
            ? DrinkState.WaitingForDrinks
            : DrinkState.Empty;

        TransitionTo(nextState);
    }

    private void ShowDrinkIndicator(Material material)
    {
        if (material == null)
        {
            Debug.LogError($"Drink is missing the material for its {State} state.", this);
            return;
        }

        indicatorRenderer.sharedMaterial = material;
        drinkIndicator.SetActive(true);
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
            player.RemoveBeer(requiredBeer);
        }

        TransitionTo(DrinkState.DrinkingAndGivingInfo);
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

    private void SetActiveBeerScale(Vector3 scale)
    {
        foreach (GameObject soldier in soldiers)
        {
            if (!soldier.activeSelf)
            {
                continue;
            }

            Transform beer = soldier.transform.Find("Beer");
            if (beer != null)
            {
                beer.localScale = scale;
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
        if (Suspition.instance == null)
        {
            Debug.LogError("Drink could not find the Suspition singleton.", this);
            return;
        }

        Suspition.instance.Add(suspicionPenalty);
    }
}
