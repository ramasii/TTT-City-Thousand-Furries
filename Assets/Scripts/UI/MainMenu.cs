using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        AudioManager.instance.PlayUIClick();
        SceneManager.LoadScene("SampleScene");
    }
    
}
