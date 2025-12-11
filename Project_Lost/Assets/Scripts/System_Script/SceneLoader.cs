using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// 指定されたシーンに遷移します。
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"'{sceneName}' をロードします。");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ゲームを終了します。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("ゲームを終了します。");
        Application.Quit();

        // Unityエディターでテストする際の停止
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}