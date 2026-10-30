using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Canvas Elements")]
    [SerializeField] private GameObject winPanel; // You Win Panel
    [SerializeField] private GameObject losePanel; // You Lose Panel


    [Header("Game State")]
    private int activeEnemyCount = 0; // variable for current Enemy count
    private int alivePlayers = 1; // total active players, set to 2 for 2 players
    private bool isGameOver = false; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    void Start()
    {
        SetWinLosePanel();
        GetEnemyCount();
        Time.timeScale = 1f; // set to 1f everytime the MainGameScene gets loaded
    }

    void SetWinLosePanel()
    {
        if (winPanel != null) winPanel.SetActive(false); 
        if (losePanel != null) losePanel.SetActive(false);
    }

    void GetEnemyCount()
    {
        // automatically count all pre-placed scene enemies via listing all Game Objects with tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        activeEnemyCount = enemies.Length; // sets initial Enemy count via length gathered from the List
    }

    // method to decrease current enemy count and trigger Win
    public void UnregisterEnemy()
    {
        if (isGameOver) return;

        activeEnemyCount--;

        if (activeEnemyCount <= 0)
        {
            TriggerWin();
        }
    }

    // method to decrease player count and trigger Lose
    public void RegisterPlayerDeath()
    {
        if (isGameOver) return;

        alivePlayers--;

        if (alivePlayers <= 0)
        {
            TriggerLose();
        }
    }

    // trigger Player Win method
    public void TriggerWin()
    {
        if (isGameOver) return;
        isGameOver = true;

        // wait 0.7f before freezing so Enemy/Player explosions can finish playing
        StartCoroutine(ShowEndGameSequence(winPanel, 0.7f));
    }

    // trigger Player Lose method
    public void TriggerLose()
    {
        if (isGameOver) return;
        isGameOver = true;

        // wait 0.7f before freezing so death explosion can finish playing
        StartCoroutine(ShowEndGameSequence(losePanel, 0.7f));
    }

    // delay sequence allowing explosion animations to complete before setting timeScale = 0
    private IEnumerator ShowEndGameSequence(GameObject panelToShow, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (panelToShow != null) panelToShow.SetActive(true);
        Time.timeScale = 0f; // freeze game after the explosion finishes
    }

    // Main Menu Button OnClick() event
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // unpauses gameplay time before switching scenes
        SceneManager.LoadScene("MainMenuScene"); 
    }

    // Retry/Restart Button OnClick() event (To be USED, currently UNUSED)
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}