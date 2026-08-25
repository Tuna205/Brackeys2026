using UnityEngine;

public sealed class Soldier : MonoBehaviour
{
    public enum SoldierType
    {
        Soldier,
        Officer
    }

    [Header("Models")]
    [SerializeField] private GameObject soldierModel01 = null;
    [SerializeField] private GameObject soldierModel02 = null;
    [SerializeField] private GameObject officerModel03 = null;
    [SerializeField] private GameObject officerModel04 = null;

    public SoldierType Type { get; private set; }
    public int ModelNumber { get; private set; }
    public GameObject ModelInstance { get; private set; }

    private void OnEnable()
    {
        int modelIndex = Random.Range(0, 4);
        GameObject modelPrefab = GetModelPrefab(modelIndex);

        if (modelPrefab == null)
        {
            Debug.LogError($"{name} is missing soldier model {modelIndex + 1}.", this);
            enabled = false;
            return;
        }

        if (ModelInstance != null)
        {
            ModelInstance.SetActive(false);
            Destroy(ModelInstance);
        }

        ModelNumber = modelIndex + 1;
        Type = modelIndex < 2 ? SoldierType.Soldier : SoldierType.Officer;

        ModelInstance = Instantiate(modelPrefab, transform);
        ModelInstance.name = modelPrefab.name;
        ModelInstance.transform.SetLocalPositionAndRotation(
            new Vector3(0f, 1.62f, 0f),
            Quaternion.identity);
        ModelInstance.transform.localScale = Vector3.one;
    }

    private GameObject GetModelPrefab(int modelIndex)
    {
        return modelIndex switch
        {
            0 => soldierModel01,
            1 => soldierModel02,
            2 => officerModel03,
            3 => officerModel04,
            _ => null
        };
    }
}
