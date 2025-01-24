using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Synaptafin.Editor.SelectionTracker {

  [InitializeOnLoad]
  public static class Utils {

    private static readonly PreferencePersistence s_preferenceOption = PreferencePersistence.instance;

    static Utils() {
      Selection.selectionChanged += RecordSelectionChange;
    }

    private static void RecordSelectionChange() {
      if (Selection.activeObject == null) {
        return;
      }

      bool isSceneObject = Selection.activeObject is GameObject go && go.scene.isLoaded;

      if (isSceneObject && !s_preferenceOption.GetToggleValue(Constants.SHOW_LOADED_GAMEOBJECTS_KEY)) {
        return;
      }

      Entry entry = new(Selection.activeObject);
      EntryServicePersistence.instance.RecordSelection(entry);
    }

    [Shortcut("Selection Tracker/Previous Selection", KeyCode.O, ShortcutModifiers.Control)]
    public static void PreviousSelection() {
      Entry selection = EntryServicePersistence.instance.JumpToPreviousSelection();
      JumpToSelection(selection);
    }

    [Shortcut("SelectionTracker/Next Selection", KeyCode.I, ShortcutModifiers.Control)]
    public static void NextSelection() {
      Entry selection = EntryServicePersistence.instance.JumpToNextSelection();
      JumpToSelection(selection);
    }

    private static void JumpToSelection(Entry entry) {
      Object obj = entry?.Ref;
      if (obj != null) {
        Selection.activeObject = obj;
      } else {
        if (entry.IsGameObject && entry.GameObjectInstanceState.HasFlag(GameObjectState.Unloaded)) {
          entry.Ping();
        }
      }
    }
  }
}

