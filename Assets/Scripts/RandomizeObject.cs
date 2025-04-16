using UnityEngine;

public class RandomizeObject : MonoBehaviour
{
    [SerializeField]
    Vector3 localRotatonMin = Vector3.zero;
    [SerializeField]
    Vector3 localRotatonMax = Vector3.zero;
    [SerializeField]
    float localscaleMultiplierMin = 0.0f;
    [SerializeField]
    float localscaleMultiplierMax = 0.0f;
    Vector3 localScaleOriginal = Vector3.one;
    void Start()
    {
        localScaleOriginal = transform.localScale;
    }
    void onEnable()
    {
        transform.localRotation = Quaternion.Euler(Random.Range(localRotatonMin.x, localRotatonMax.x),
        Random.Range(localRotatonMin.y, localRotatonMax.y), Random.Range(localRotatonMin.z, localRotatonMax.z));
        transform.localScale = localScaleOriginal * Random.Range(localscaleMultiplierMin, localscaleMultiplierMax);
    }

}


