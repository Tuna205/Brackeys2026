using UnityEngine;

public sealed class Soldier : MonoBehaviour
{
    private const int ModelCount = 4;

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

    public SoldierType Type { get; private set; }
    public int ModelNumber { get; private set; }
    public GameObject ModelInstance { get; private set; }

    private void Awake()
    {
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
    }
}
