using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public GameObject menuOpciones;
    public GameObject menuPrincipal;

    public void OpenOptionsPanel()
    {
        menuPrincipal.SetActive(false);
        menuOpciones.SetActive(true);
    }
    public void OpenMainMenuPanel()
    {
        menuPrincipal.SetActive(true);
        menuOpciones.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void VerNiveles()
    {
        SceneManager.LoadScene("Niveles");
    }
}
