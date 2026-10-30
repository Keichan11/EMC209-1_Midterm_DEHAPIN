using UnityEngine;

public class BackgroundBehaviour : MonoBehaviour
{
    [Header("Movement")]
    private float scrollSpeed = 0.25f;

    [Header("Seamless Setup")]
    [Tooltip("The exact width of your background image in Unity units. Set this manually to avoid auto-calculation gaps!")]
    [SerializeField] private float exactPanelWidth = 20f; 

    [SerializeField] private Transform[] backgroundPanels;

    void Start()
    {
        // Automatically grab children if the array is empty
        if (backgroundPanels == null || backgroundPanels.Length == 0)
        {
            int childCount = transform.childCount;
            backgroundPanels = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                backgroundPanels[i] = transform.GetChild(i);
            }
        }
    }

    void Update()
    {
        foreach (Transform panel in backgroundPanels)
        {
            // Move left continuously
            panel.position += Vector3.left * scrollSpeed * Time.deltaTime;

            // When a panel moves fully off-screen to the left, 
            // teleport it to the back of the line on the right
            if (panel.position.x <= -exactPanelWidth)
            {
                float furthestRightX = GetFurthestRightX();
                panel.position = new Vector3(furthestRightX + exactPanelWidth, panel.position.y, panel.position.z);
            }
        }
    }

    float GetFurthestRightX()
    {
        float maxX = backgroundPanels[0].position.x;
        foreach (Transform panel in backgroundPanels)
        {
            if (panel.position.x > maxX)
            {
                maxX = panel.position.x;
            }
        }
        return maxX;
    }
}