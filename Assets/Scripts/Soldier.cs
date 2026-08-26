using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class Soldier : MonoBehaviour
{
    private const int ModelCount = 4;
    private const float MinimumAnimationSpeed = 0.95f;
    private const float MaximumAnimationSpeed = 1.05f;

    private static readonly int WaitingParameter = Animator.StringToHash("waiting");
    private static readonly int AngryParameter = Animator.StringToHash("angry");
    private static readonly int TypeParameter = Animator.StringToHash("type");
    private static readonly int LeavingParameter = Animator.StringToHash("leaving");
    private static readonly int ServedParameter = Animator.StringToHash("served");

    private static readonly string[] ModelNames =
    {
        "SM_Soldier_01",
        "SM_Soldier_02",
        "SM_Soldier_03",
        "SM_Soldier_04"
    };

    public enum SoldierType
    {
        Soldier,
        Officer
    }

    [Header("Models")]
    [SerializeField] private Transform models = null;

    private readonly GameObject[] modelOptions = new GameObject[ModelCount];
    private Animator animator;
    private Action<Soldier> exitedDoorCallback;
    private Coroutine randomizeLoopStartRoutine;

    public SoldierType Type { get; private set; }
    public int ModelNumber { get; private set; }
    public GameObject ModelInstance { get; private set; }
    public bool IsLeaving { get; private set; }

    private void Awake()
    {
        if (!TryGetComponent(out Collider _))
        {
            CapsuleCollider bodyCollider = gameObject.AddComponent<CapsuleCollider>();
            bodyCollider.center = new Vector3(0f, 1f, 0f);
            bodyCollider.height = 2f;
            bodyCollider.radius = 0.4f;
            bodyCollider.isTrigger = true;
        }

        if (models == null)
        {
            models = transform.Find("Models");
        }

        if (models == null)
        {
            Debug.LogError($"{name} is missing its Models child.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < ModelCount; i++)
        {
            Transform model = models.Find(ModelNames[i]);
            if (model == null)
            {
                Debug.LogError($"{name}/Models is missing {ModelNames[i]}.", this);
                enabled = false;
                return;
            }

            modelOptions[i] = model.gameObject;
        }
    }

    private void OnEnable()
    {
        if (models == null)
        {
            return;
        }

        int modelIndex = Random.Range(0, ModelCount);
        IsLeaving = false;
        exitedDoorCallback = null;

        for (int i = 0; i < ModelCount; i++)
        {
            modelOptions[i].SetActive(i == modelIndex);
        }

        ModelNumber = modelIndex + 1;
        Type = modelIndex < 2 ? SoldierType.Soldier : SoldierType.Officer;
        ModelInstance = modelOptions[modelIndex];

        animator = ModelInstance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError($"{ModelInstance.name} needs an Animator component.", ModelInstance);
            return;
        }

        animator.SetBool(WaitingParameter, false);
        animator.SetBool(AngryParameter, false);
        animator.SetBool(LeavingParameter, false);
        animator.SetBool(ServedParameter, false);
        animator.speed = Random.Range(MinimumAnimationSpeed, MaximumAnimationSpeed);
        QueueLoopStartRandomization();
    }

    public void SetWaitingAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(AngryParameter, false);
        animator.SetBool(ServedParameter, false);
        animator.SetBool(WaitingParameter, true);
        QueueLoopStartRandomization();
    }

    public void SetAngryAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetInteger(TypeParameter, Random.Range(0, 2));
        animator.SetBool(WaitingParameter, false);
        animator.SetBool(ServedParameter, false);
        animator.SetBool(AngryParameter, true);
        QueueLoopStartRandomization();
    }

    public void SetServedAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(WaitingParameter, false);
        animator.SetBool(AngryParameter, false);
        animator.SetBool(ServedParameter, true);
        QueueLoopStartRandomization();
    }

    public void BeginLeaving(Action<Soldier> onExitedDoor)
    {
        IsLeaving = true;
        exitedDoorCallback = onExitedDoor;

        if (animator == null)
        {
            return;
        }

        animator.SetBool(WaitingParameter, false);
        animator.SetBool(AngryParameter, false);
        animator.SetBool(ServedParameter, false);
        animator.SetBool(LeavingParameter, true);
        QueueLoopStartRandomization();
    }

    public void ExitThroughDoor()
    {
        if (!IsLeaving)
        {
            return;
        }

        IsLeaving = false;
        Action<Soldier> callback = exitedDoorCallback;
        exitedDoorCallback = null;
        callback?.Invoke(this);
        Destroy(gameObject);
    }

    private void QueueLoopStartRandomization()
    {
        if (randomizeLoopStartRoutine != null)
        {
            StopCoroutine(randomizeLoopStartRoutine);
        }

        randomizeLoopStartRoutine = StartCoroutine(RandomizeLoopStartAfterTransition());
    }

    private IEnumerator RandomizeLoopStartAfterTransition()
    {
        yield return null;

        while (animator != null && animator.IsInTransition(0))
        {
            yield return null;
        }

        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.loop)
            {
                animator.Play(state.fullPathHash, 0, Random.value);
            }
        }

        randomizeLoopStartRoutine = null;
    }
}
