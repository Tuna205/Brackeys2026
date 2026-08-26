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
        SoldiersArriving,
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
    [SerializeField] private Soldier soldierPrefab = null;
    [SerializeField] private Material blackMaterial = null;

    private const float MinimumRequestDelay = 0f;
    private const float MaximumRequestDelay = 10f;
    private const float SoldierSpawnInterval = 1f;
    private const float SoldierSpawnY = 0f;
    private const float PatientDuration = 15f;
    private const float AngryDuration = 30f;
    private const float WaitingForDrinksDuration = 30f;
    private const float WaitingForDrinksAngryDuration = 15f;
    private const float DrinkingAndGivingInfoDuration = 30f;
    private const int MinimumSoldiersPerTable = 2;
    private const int MaximumSoldiersPerTable = 4;
    private static readonly Vector3 SmallBeerScale = Vector3.one * 0.2f;
    private static readonly Vector3 LargeBeerScale = Vector3.one * 0.4f;

    private float suspicionPenalty = 30f;

    public DrinkState State { get; private set; }

    private Table table;
    private InputAction interactAction;
    private Coroutine stateTimer;
    private readonly List<Transform> soldierPositions = new();
    private readonly List<Soldier> spawnedSoldiers = new();
    private readonly List<Player.BeerTypes> requiredBeers = new();
    private readonly Dictionary<Renderer, Material[]> originalSoldierMaterials = new();
    private Player player;
    private Transform door;
    private Coroutine soldierArrivalRoutine;
    private int expectedSoldierCount;
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

        if (soldierList == null || soldierPrefab == null)
        {
            Debug.LogError("Drink needs a SoldierList and Soldier prefab.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < soldierList.childCount; i++)
        {
            Transform child = soldierList.GetChild(i);
            if (child.name.StartsWith("SoldierPosition", StringComparison.Ordinal))
            {
                soldierPositions.Add(child);
            }
            else if (child.TryGetComponent(out Soldier legacySoldier))
            {
                legacySoldier.gameObject.SetActive(false);
            }
        }

        if (soldierPositions.Count == 0)
        {
            Debug.LogError("Drink needs SoldierPosition children inside SoldierList.", this);
            enabled = false;
            return;
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
        GameObject doorObject = GameObject.Find("Door");
        if (doorObject == null)
        {
            Debug.LogError("Drink could not find the Door object.", this);
            enabled = false;
            return;
        }

        door = doorObject.transform;
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

        SetActiveBeerScale(SmallBeerScale);
        RemoveAllSoldiers();
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
            case DrinkState.SoldiersArriving:
                OnEnterSoldiersArriving();
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
            case DrinkState.SoldiersArriving:
                OnExitSoldiersArriving();
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
        RemoveAllSoldiers();

        if (stateMachineIsRunning)
        {
            float requestDelay = UnityEngine.Random.Range(MinimumRequestDelay, MaximumRequestDelay);
            StartStateTimer(requestDelay, () => TransitionTo(DrinkState.SoldiersArriving));
        }
    }

    private void OnExitEmpty()
    {
        StopStateTimer();
    }

    private void OnEnterSoldiersArriving()
    {
        drinkIndicator.SetActive(false);
        StartSoldierArrivals();
    }

    private void OnExitSoldiersArriving()
    {
        StopSoldierArrivals();
    }

    private void OnEnterPatient()
    {
        ShowDrinkIndicator(greenMaterial);
        StartStateTimer(PatientDuration, () => TransitionTo(DrinkState.Angry));
    }

    private void OnExitPatient()
    {
        StopStateTimer();
    }

    private void OnEnterAngry()
    {
        ShowDrinkIndicator(redMaterial);
        SetSoldierAnimations(soldier => soldier.SetAngryAnimation());
        StartStateTimer(AngryDuration, LeaveAngryAndReturnToEmpty);
    }

    private void OnExitAngry()
    {
        StopStateTimer();
    }

    private void OnEnterWaitingForDrinks()
    {
        drinkIndicator.SetActive(false);
        SetSoldierAnimations(soldier => soldier.SetWaitingAnimation());
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
        SetSoldierAnimations(soldier => soldier.SetAngryAnimation());
        SetActiveBeerScale(LargeBeerScale);
        StartStateTimer(WaitingForDrinksAngryDuration, LeaveAngryAndReturnToEmpty);
    }

    private void OnExitWaitingForDrinksAngry()
    {
        StopStateTimer();
        SetActiveBeerScale(SmallBeerScale);
    }

    private void OnEnterDrinkingAndGivingInfo()
    {
        drinkIndicator.SetActive(false);
        SetSoldierAnimations(soldier => soldier.SetServedAnimation());
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

    private void LeaveAngryAndReturnToEmpty()
    {
        CauseSoldiersLeavingAngry();
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

    private void StartSoldierArrivals()
    {
        StopSoldierArrivals();
        RemoveAllSoldiers();

        int maximumSoldiers = Mathf.Min(MaximumSoldiersPerTable, soldierPositions.Count);
        int minimumSoldiers = Mathf.Min(MinimumSoldiersPerTable, maximumSoldiers);
        expectedSoldierCount = UnityEngine.Random.Range(minimumSoldiers, maximumSoldiers + 1);

        List<Transform> availablePositions = new(soldierPositions);
        for (int i = 0; i < expectedSoldierCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(i, availablePositions.Count);
            (availablePositions[i], availablePositions[selectedIndex]) =
                (availablePositions[selectedIndex], availablePositions[i]);
        }

        soldierArrivalRoutine = StartCoroutine(SpawnSoldiers(availablePositions));
    }

    private IEnumerator SpawnSoldiers(List<Transform> destinations)
    {
        for (int i = 0; i < expectedSoldierCount; i++)
        {
            Vector3 spawnPosition = door.position;
            spawnPosition.y = SoldierSpawnY;

            Soldier soldier = Instantiate(
                soldierPrefab,
                spawnPosition,
                door.rotation);
            spawnedSoldiers.Add(soldier);

            SoldierMovement movement = soldier.GetComponent<SoldierMovement>();
            if (movement == null
                || !movement.MoveFromTo(spawnPosition, destinations[i], OnSoldierArrived))
            {
                Debug.LogError("Spawned Soldier could not start moving to its table.", soldier);
                yield break;
            }

            if (i < expectedSoldierCount - 1)
            {
                yield return new WaitForSeconds(SoldierSpawnInterval);
            }
        }

        soldierArrivalRoutine = null;
    }

    private void OnSoldierArrived(SoldierMovement movement)
    {
        if (State != DrinkState.SoldiersArriving
            || !movement.TryGetComponent(out Soldier soldier))
        {
            return;
        }

        soldier.transform.SetParent(soldierList, true);
        table.RegisterArrivedSoldier(soldier);
        soldier.SetWaitingAnimation();

        if (table.ArrivedSoldiers.Count == expectedSoldierCount)
        {
            TransitionTo(DrinkState.Patient);
        }
    }

    private void StopSoldierArrivals()
    {
        if (soldierArrivalRoutine != null)
        {
            StopCoroutine(soldierArrivalRoutine);
            soldierArrivalRoutine = null;
        }
    }

    private void RemoveAllSoldiers()
    {
        StopSoldierArrivals();
        table?.ClearArrivedSoldiers();

        foreach (Soldier soldier in spawnedSoldiers)
        {
            if (soldier != null)
            {
                Destroy(soldier.gameObject);
            }
        }

        spawnedSoldiers.Clear();
        expectedSoldierCount = 0;
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

        foreach (Soldier soldier in table.ArrivedSoldiers)
        {
            Transform beer = soldier.transform.Find("Beer");
            if (beer == null || !beer.TryGetComponent(out Renderer beerRenderer))
            {
                Debug.LogError($"{soldier.name} needs a Beer child with a Renderer.", soldier);
                continue;
            }

            int beerIndex = UnityEngine.Random.Range(0, beerMaterials.Length);
            beerRenderer.sharedMaterial = beerMaterials[beerIndex];
            beer.localScale = SmallBeerScale;
            beer.gameObject.SetActive(true);
            requiredBeers.Add(beerTypes[beerIndex]);
        }
    }

    private void DisableAllBeers()
    {
        foreach (Soldier soldier in table.ArrivedSoldiers)
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
        foreach (Soldier soldier in table.ArrivedSoldiers)
        {
            Transform beer = soldier.transform.Find("Beer");
            if (beer != null)
            {
                beer.localScale = scale;
            }
        }
    }

    private void SetSoldierAnimations(Action<Soldier> setAnimation)
    {
        foreach (Soldier soldier in table.ArrivedSoldiers)
        {
            if (soldier != null)
            {
                setAnimation(soldier);
            }
        }
    }

    private void SetActiveSoldierMaterials(Material material)
    {
        originalSoldierMaterials.Clear();

        foreach (Soldier soldier in table.ArrivedSoldiers)
        {
            foreach (Renderer soldierRenderer in soldier.GetComponentsInChildren<Renderer>())
            {
                if (soldierRenderer.transform.name == "Beer")
                {
                    continue;
                }

                originalSoldierMaterials.Add(soldierRenderer, soldierRenderer.sharedMaterials);

                Material[] materials = new Material[soldierRenderer.sharedMaterials.Length];
                Array.Fill(materials, material);
                soldierRenderer.sharedMaterials = materials;
            }
        }
    }

    private void RestoreSoldierMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> soldierRenderer in originalSoldierMaterials)
        {
            if (soldierRenderer.Key != null)
            {
                soldierRenderer.Key.sharedMaterials = soldierRenderer.Value;
            }
        }

        originalSoldierMaterials.Clear();
    }

    private void CauseSoldiersLeavingAngry()
    {
        if (Suspition.instance == null)
        {
            Debug.LogError("Drink could not find the Suspition singleton.", this);
            return;
        }

        Suspition.instance.Add(suspicionPenalty);
    }
}
