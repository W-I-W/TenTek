using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScenesController
{
    private const string Menu = "Menu";
    private const string Game = "Game";
    

    public static void PlayGame()
    {
        SceneManager.LoadScene(Game);
    }

    public static void PlayMenu()
    {
        SceneManager.LoadScene(Menu);
    }
}
