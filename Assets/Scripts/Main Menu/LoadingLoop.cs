using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LoadingLoop : MonoBehaviour
{
    private TMP_Text tmpText; 
    public string baseText = "Loading";
    public float interval = 0.5f;

    private Coroutine loadingCoroutine;

    void OnEnable()
    {
        // 1. Automatically grab the component immediately when turned on
        tmpText = GetComponent<TMP_Text>();

        // 2. Start the animation safely now that tmpText is guaranteed to exist
        loadingCoroutine = StartCoroutine(AnimateLoading());
    }

    void OnDisable()
    {
        if (loadingCoroutine != null)
            StopCoroutine(loadingCoroutine);
    }

    IEnumerator AnimateLoading()
    {
        int dotCount = 0;
        while (true)
        {
            string dots = new string('.', dotCount);
            
            // This line will no longer throw an error
            tmpText.text = baseText + dots;
            
            dotCount = (dotCount + 1) % 4; 
            yield return new WaitForSeconds(interval);
        }
    }
}
