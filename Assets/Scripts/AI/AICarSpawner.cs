using System.Collections;
using UnityEngine;

public class AICarSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] GameObject[] carAIPrefabs;
    [SerializeField] int poolSize = 20;
    [SerializeField] float spawnCooldown = 2f;
    [SerializeField] float spawnDistanceAhead = 100f;
    [SerializeField] float despawnDistanceBehind = 50f;
    [SerializeField] float despawnDistanceAhead = 200f;
    
    [Header("Position Settings")]
    [SerializeField] float carXPosition = 0f;
    [SerializeField] float carYPosition = -1.4f; // Forced to -1.4f as requested
    [SerializeField] float positionCheckRadius = 0.5f;
    [SerializeField] LayerMask positionCheckLayer;

    private GameObject[] carAIPool;
    private WaitForSeconds waitFor100ms = new WaitForSeconds(0.5f);
    private float timeLastCarSpawned = 0;
    private Transform playerCarTransform;

    void Start()
    {
        if (carAIPrefabs == null || carAIPrefabs.Length == 0)
        {
            Debug.LogError("No AI car prefabs assigned!");
            enabled = false;
            return;
        }

        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;
        InitializePool();
        StartCoroutine(UpdateLessOftenCO());
    }

    void InitializePool()
    {
        carAIPool = new GameObject[poolSize];
        int prefabIndex = 0;

        for (int i = 0; i < carAIPool.Length; i++)
        {
            carAIPool[i] = Instantiate(carAIPrefabs[prefabIndex]);
            ResetCarTransform(carAIPool[i].transform);
            carAIPool[i].SetActive(false);
            
            prefabIndex = (prefabIndex + 1) % carAIPrefabs.Length;
        }
    }

    void ResetCarTransform(Transform carTransform)
    {
        // Reset all transform values to ensure no inherited positioning
        carTransform.localPosition = Vector3.zero;
        carTransform.localRotation = Quaternion.identity;
        carTransform.localScale = Vector3.one;
        
        // Apply our specific position
        carTransform.position = new Vector3(
            carXPosition,
            carYPosition,
            carTransform.position.z
        );
    }

    IEnumerator UpdateLessOftenCO()
    {
        while (true)
        {
            CleanUpCarsBeyondView();
            SpawnNewCars();
            yield return waitFor100ms;
        }
    }

    void SpawnNewCars()
    {
        if (Time.time - timeLastCarSpawned < spawnCooldown)
            return;

        GameObject carToSpawn = GetInactiveCarFromPool();
        if (carToSpawn == null)
            return;

        // Triple-check position application
        carToSpawn.transform.position = new Vector3(
            carXPosition,
            carYPosition, // This should absolutely force the Y position
            playerCarTransform.position.z + spawnDistanceAhead
        );

        carToSpawn.SetActive(true);
        timeLastCarSpawned = Time.time;

        // Debug log to verify actual position
        Debug.Log($"Spawned AI car at: {carToSpawn.transform.position}");
    }

    GameObject GetInactiveCarFromPool()
    {
        foreach (GameObject aiCar in carAIPool)
        {
            if (!aiCar.activeInHierarchy)
                return aiCar;
        }
        return null;
    }

    void CleanUpCarsBeyondView()
    {
        foreach (GameObject aiCar in carAIPool)
        {
            if (!aiCar.activeInHierarchy)
                continue;

            float distanceFromPlayer = aiCar.transform.position.z - playerCarTransform.position.z;
            
            if (distanceFromPlayer > despawnDistanceAhead || 
                distanceFromPlayer < -despawnDistanceBehind)
            {
                aiCar.SetActive(false);
                // Reset position when deactivating
                aiCar.transform.position = new Vector3(
                    carXPosition,
                    carYPosition,
                    aiCar.transform.position.z
                );
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            new Vector3(carXPosition, carYPosition, playerCarTransform ? playerCarTransform.position.z + 50f : 50f),
            new Vector3(5f, 0.1f, 100f)
        );
    }
}