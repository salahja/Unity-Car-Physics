// Coin.cs
using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotateSpeed = 100f;
    [SerializeField] private ParticleSystem collectEffect;

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the collider is the dedicated coin detector
        if (other.CompareTag("CoinDetector")) 
        {
            CollectCoin();
        }
    }

    void CollectCoin()
    {
        Debug.Log("Coin collected!");
        
        // Visual feedback
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Update score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }
        else
        {
            Debug.LogWarning("GameManager instance missing!");
        }

        Destroy(gameObject);
    }
}