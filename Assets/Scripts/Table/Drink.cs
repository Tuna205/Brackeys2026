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
        DrinkingAndGivingInfo,
        Leaving
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions = null;

    [Header("Indicator")]
    [SerializeField] private GameObject drinkIndicator = null;
    [SerializeField] private Renderer indicatorRenderer = null;
    [SerializeField] private Material greenMaterial = null;
    [SerializeField] private Material redMaterial = null;

    [Header("Soldiers")]
    [SerializeField] private Transform soldierList = null;
    [SerializeField] private Soldier soldierPrefab = null;

    [Header("Beer Materials")]
    [SerializeField] private Material whiteBeerMaterial = null;
    [SerializeField] private Material redBeerMaterial = null;
    [SerializeField] private Material darkBeerMaterial = null;

    [Header("Audio")]
    [SerializeField, Range(0f, 1f)] private float lowTalkingVolume = 0.1f;
    [SerializeField, Range(0f, 1f)] private float mediumTalkingVolume = 0.22f;
    [SerializeField, Range(0f, 1f)] private float highTalkingVolume = 0.4f;

    private const float MinimumRequestDelay = 5f;
    private const float MaximumRequestDelay = 30f;
    private const float SoldierSpawnInterval = 1f;
    private const float PatientDuration = 15f;
    private const float AngryDuration = 30f;
    private const float WaitingForDrinksDuration = 30f;
    private const float WaitingForDrinksAngryDuration = 15f;
    private const float DrinkingAndGivingInfoDuration = 30f;
    private const int MinimumSoldiersPerTable = 2;
    private const int MaximumSoldiersPerTable = 4;
    private static readonly Vector3 SmallBeerScale = Vector3.one * 0.2f;
    private static readonly Vector3 LargeBeerScale = Vector3.one * 0.4f;
    private static Drink initialSpawnTable;

    private float suspicionPenalty = 30f;

    public DrinkState State { get; private set; }

    private Table table;
    private InputAction interactAction;
    private Coroutine stateTimer;
    private readonly List<Transform> soldierPositions = new();
    private readonly List<Soldier> spawnedSoldiers = new();
    private readonly List<Player.BeerTypes> requiredBeers = new();
    private Player player;
    private Transform door;
    private Transform soldierSpawn;
    private Coroutine soldierArrivalRoutine;
    private int expectedSoldierCount;
    private int expectedLeavingSoldierCount;
    private int soldiersAtDoorCount;
    private bool hasEnteredState;
    private bool stateMachineIsRunning;
    private bool hasScheduledInitialRequest;

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
        Door doorComponent = FindAnyObjectByType<Door>();
        if (doorComponent == null)
        {
            Debug.LogError("Drink could not find the Door object.", this);
            enabled = false;
            return;
        }

        door = doorComponent.transform;
        soldierSpawn = door.Find("SoldierSpawn");
        if (soldierSpawn == null)
        {
            Debug.LogError("The Door needs a child named SoldierSpawn.", doorComponent);
            enabled = false;
            return;
        }

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
            case DrinkState.Leaving:
                OnEnterLeaving();
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
            case DrinkState.Leaving:
                OnExitLeaving();
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
            float requestDelay = GetNextRequestDelay();
            StartStateTimer(requestDelay, () => TransitionTo(DrinkState.SoldiersArriving));
        }
    }

    private float GetNextRequestDelay()
    {
        if (!hasScheduledInitialRequest)
        {
            hasScheduledInitialRequest = true;
            SelectInitialSpawnTable();

            if (initialSpawnTable == this)
            {
                return 0f;
            }
        }

        return UnityEngine.Random.Range(MinimumRequestDelay, MaximumRequestDelay);
    }

    private static void SelectInitialSpawnTable()
    {
        if (initialSpawnTable != null)
        {
            return;
        }

        Drink[] tables = FindObjectsByType<Drink>(FindObjectsInactive.Exclude);
        if (tables.Length > 0)
        {
            initialSpawnTable = tables[UnityEngine.Random.Range(0, tables.Length)];
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
        SetSoldierTalkingVolume(lowTalkingVolume);
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
        SetSoldierTalkingVolume(mediumTalkingVolume);
        StartStateTimer(AngryDuration, LeaveAngry);
    }

    private void OnExitAngry()
    {
        StopStateTimer();
    }

    private void OnEnterWaitingForDrinks()
    {
        drinkIndicator.SetActive(false);
        SetSoldierAnimations(soldier => soldier.SetWaitingAnimation());
        SetSoldierTalkingVolume(lowTalkingVolume);
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
        SetSoldierTalkingVolume(mediumTalkingVolume);
        SetActiveBeerScale(LargeBeerScale);
        StartStateTimer(WaitingForDrinksAngryDuration, LeaveAngry);
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
        SetSoldierTalkingVolume(highTalkingVolume);
        DisableAllBeers();
        StartStateTimer(DrinkingAndGivingInfoDuration, FinishDrinking);
    }

    private void OnExitDrinkingAndGivingInfo()
    {
        StopStateTimer();
    }

    private void OnEnterLeaving()
    {
        drinkIndicator.SetActive(false);
        DisableAllBeers();
        StopSoldierTalking();

        List<Soldier> soldiersLeaving = new(table.ArrivedSoldiers);
        expectedLeavingSoldierCount = soldiersLeaving.Count;
        soldiersAtDoorCount = 0;

        if (expectedLeavingSoldierCount == 0)
        {
            TransitionTo(DrinkState.Empty);
            return;
        }

        foreach (Soldier soldier in soldiersLeaving)
        {
            soldier.BeginLeaving(OnSoldierExitedDoor);

            SoldierMovement movement = soldier.GetComponent<SoldierMovement>();
            if (movement == null
                || !movement.MoveFromTo(soldier.transform.position, door, OnSoldierReachedDoor))
            {
                Debug.LogError("Soldier could not start leaving for the Door.", soldier);
                soldier.ExitThroughDoor();
            }
        }
    }

    private void OnExitLeaving()
    {
        expectedLeavingSoldierCount = 0;
        soldiersAtDoorCount = 0;
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

    private void LeaveAngry()
    {
        CauseSoldiersLeavingAngry();
    }

    private void FinishDrinking()
    {
        TransitionTo(DrinkState.Leaving);
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
            Vector3 spawnPosition = soldierSpawn.position;

            Soldier soldier = Instantiate(
                soldierPrefab,
                spawnPosition,
                soldierSpawn.rotation);
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

    private void OnSoldierReachedDoor(SoldierMovement movement)
    {
        if (State != DrinkState.Leaving
            || !movement.TryGetComponent(out Soldier soldier))
        {
            return;
        }

        soldier.ExitThroughDoor();
    }

    private void OnSoldierExitedDoor(Soldier soldier)
    {
        if (State != DrinkState.Leaving)
        {
            return;
        }

        soldiersAtDoorCount++;

        if (soldiersAtDoorCount >= expectedLeavingSoldierCount)
        {
            TransitionTo(DrinkState.Empty);
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
        expectedLeavingSoldierCount = 0;
        soldiersAtDoorCount = 0;
    }

    private void EnableBeersForActiveSoldiers()
    {
        Material[] beerMaterials = { whiteBeerMaterial, redBeerMaterial, darkBeerMaterial };
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
            Renderer beerRenderer = GetBeerRenderer(beer);
            if (beerRenderer == null)
            {
                Debug.LogError($"{soldier.name} needs a Beer child containing a Renderer.", soldier);
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
        ForEachArrivedSoldier(setAnimation);
    }

    private void SetSoldierTalkingVolume(float volume)
    {
        ForEachArrivedSoldier(soldier => soldier.SetTalkingVolume(volume));
    }

    private void StopSoldierTalking()
    {
        ForEachArrivedSoldier(soldier => soldier.StopTalking());
    }

    private void ForEachArrivedSoldier(Action<Soldier> action)
    {
        foreach (Soldier soldier in table.ArrivedSoldiers)
        {
            if (soldier != null)
            {
                action(soldier);
            }
        }
    }

    private static Renderer GetBeerRenderer(Transform beer)
    {
        if (beer == null)
        {
            return null;
        }

        foreach (Renderer beerRenderer in beer.GetComponentsInChildren<Renderer>(true))
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

    private void CauseSoldiersLeavingAngry()
    {
        if (Suspition.instance == null)
        {
            Debug.LogError("Drink could not find the Suspition singleton.", this);
        }
        else
        {
            Suspition.instance.Add(suspicionPenalty);
        }

        TransitionTo(DrinkState.Leaving);
    }
}
