using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Drink : MonoBehaviour
{
    public enum DrinkState
    {
        Empty,
        Patient,
        Inpatient,
        Angry
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

    [Header("Timing")]
    [SerializeField, Min(0f)] private float minimumRequestDelay = 20f;
    [SerializeField, Min(0f)] private float maximumRequestDelay = 80f;
    [SerializeField, Min(0f)] private float stateDuration = 15f;
    [SerializeField, Min(0f)] private float angryDuration = 30f;

    [Header("Penalty")]
    [SerializeField, Min(0f)] private float suspicionPenalty = 40f;

    public DrinkState State { get; private set; }
    public bool IsRequestingDrink => drinkIndicator != null && drinkIndicator.activeSelf;

    private Table table;
    private InputAction interactAction;
    private Coroutine requestLoop;

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

        SetState(DrinkState.Empty);
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void Start()
    {
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
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (State != DrinkState.Empty && table.IsPlayerInside)
        {
            EnterEmptyState();
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

            yield return new WaitForSeconds(stateDuration);
            SetState(DrinkState.Inpatient);

            yield return new WaitForSeconds(stateDuration);
            SetState(DrinkState.Angry);

            yield return new WaitForSeconds(angryDuration);
            CauseSoldiersLeft();
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
        State = newState;

        if (newState == DrinkState.Empty)
        {
            drinkIndicator.SetActive(false);
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

    private void CauseSoldiersLeft()
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
