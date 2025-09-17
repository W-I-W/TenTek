using UnityEngine;
using UnityEngine.UIElements;

public class PlayScenes : MonoBehaviour
{
    public void OnNewGame()
    {
        ScenesController.PlayGame();
    }

    public void OnMenu()
    {
        ScenesController.PlayMenu();
    }

    public void OnExit()
    {
        Application.Quit();
    }
}
