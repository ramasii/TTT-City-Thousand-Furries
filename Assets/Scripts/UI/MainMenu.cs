using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public void PlayGame(string sceneName)
    {
        AudioManager.instance.PlayUIClick();
        SceneManager.LoadScene(sceneName);
    }
    
}
