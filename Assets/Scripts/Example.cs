using UnityEngine;
using Synaptafin.PlayModeConsole;

public class Example : MonoBehaviour {

  [SerializeField] private PlayModeCommandRegistry _commandRegistry;

  public void Start() {
    _commandRegistry.RegisterCommand(GameStart);
    _commandRegistry.RegisterCommand<System.Action<int, string, float>>(SetValue);
  }

  private void SetValue(int i, string text, float f) {
    Debug.Log($"SetValue called with i={i}, text={text}, f={f}");
  }

  private void GameStart() {
    Debug.Log("StartGame called");
  }
}
