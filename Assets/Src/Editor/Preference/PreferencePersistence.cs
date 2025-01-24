using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Synaptafin.Editor.SelectionTracker {

  [FilePath("UserSettings/SelectionTracker.Preference.asset", FilePathAttribute.Location.ProjectFolder)]
  public class PreferencePersistence : ScriptableSingleton<PreferencePersistence> {

    public UnityAction onUpdated;

    [SerializeReference]
    private List<(string, bool)> _toggles = new();
    public List<(string, bool)> Toggles => _toggles;

    [SerializeField]
    private GameObjectState _globalStateFilter = GameObjectState.All;
    public GameObjectState GlobalStateFilter {
      get => _globalStateFilter;
      set => _globalStateFilter = value;
    }

    public PreferencePersistence() {
      _toggles.Add((Constants.AUTO_REMOVE_DESTROYED_KEY, true));
      _toggles.Add((Constants.AUTO_REMOVE_UNLOADED_KEY, false));
      _toggles.Add((Constants.AUTO_REMOVE_DUPLICATED_KEY, true));
      _toggles.Add((Constants.DRAW_FAVORITES_KEY, true));
      _toggles.Add((Constants.ORDER_BY_NEWER_KEY, true));
      _toggles.Add((Constants.BACKGROUND_RECORD_KEY, false));
      _toggles.Add((Constants.DETAIL_ON_HOVER_KEY, true));

      _toggles.Add((Constants.SHOW_LOADED_GAMEOBJECTS_KEY, true));
      _toggles.Add((Constants.SHOW_UNLOADED_GAMEOBJECTS_KEY, true));
      _toggles.Add((Constants.SHOW_DESTROYED_GAMEOBJECTS_KEY, false));
    }

    public bool GetToggleValue(string key) {
      return _toggles.Find(el => el.Item1 == key).Item2;
    }

    public void SetToggleValue(string key, bool value) {
      _toggles[_toggles.FindIndex(el => el.Item1 == key)] = (key, value);
      UpdateSettings();
    }

    public void UpdateSettings() {
      onUpdated?.Invoke();
      Save(true);
    }
  }
}
