using UnityEngine.UIElements;
using UnityEditor;

namespace Synaptafin.Editor.SelectionTracker {

  public class UIAssetManager : ScriptableSingleton<UIAssetManager> {
    public StyleSheet globalStyle;

    public VisualTreeAsset preferenceTemplate;
    public VisualTreeAsset TrackerTemplate;
    public VisualTreeAsset entryTemplate;

    public VisualTreeAsset detailInfoTemplate;
  }
}
