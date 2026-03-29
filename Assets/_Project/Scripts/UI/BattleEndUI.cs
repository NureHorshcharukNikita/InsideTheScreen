using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleEndUI : MonoBehaviour
{
    [SerializeField] private PlayerCharacter player;
    public TMP_Text resultText;

    public void ShowVictory()
    {
        gameObject.SetActive(true);
        resultText.text = "VICTORY";
    }

    public void ShowDefeat()
    {
        gameObject.SetActive(true);
        resultText.text = "DEFEAT";
    }

    public void NewGame()
    {
        SceneManager.LoadScene(SceneNames.Battle);
    }
}