using UnityEngine;

public sealed class Soldier : MonoBehaviour
{
    private const int ModelCount = 4;

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
    }
}
