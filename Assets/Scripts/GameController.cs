using UnityEngine;
using UnityEngine.SceneManagement;
using Synaptafin.PlayModeConsole;


public class GameController : MonoBehaviour {
  public static GameController Instance { get; private set; }

  [SerializeField] private PlayModeCommandRegistry _commandRegistry;

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void Start() {
    _commandRegistry.RegisterCommand(GameStart);
    _commandRegistry.RegisterCommand<System.Action<int, string, float>>(SetValue);
    _commandRegistry.RegisterCommand(LoadSecondScene);
    _commandRegistry.RegisterCommand(LoadSampleScene);
  }

  private void SetValue(int i, string text, float f) {
    Debug.Log($"SetValue called with i={i}, text={text}, f={f}");
  }

  private void GameStart() {
    Debug.Log("StartGame called");
  }

  private void LoadSecondScene() {
    SceneManager.LoadScene("second");
  }

  private void LoadSampleScene() {
    SceneManager.LoadScene("SampleScene");
  }
}
