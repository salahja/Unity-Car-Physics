using UnityEngine;

// GameManager.cs — put this on an empty GameObject in the scene
 
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score = 0;
    //public Text scoreText; // Assign a UI Text in the inspector
public TextMeshProUGUI scoreText;

  
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (scoreText != null)
            scoreText.text = "Score: " + score.ToString();
               
    }
}
