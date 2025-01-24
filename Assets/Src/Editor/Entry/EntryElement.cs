using System.Threading;
using System.Threading.Tasks;
using Synaptafin.Editor.SelectionTracker;
using UnityEditor;
using UnityEditor.SceneManagement;
using static Synaptafin.Editor.SelectionTracker.Constants;
using static Synaptafin.Editor.SelectionTracker.UnityBuiltInIcons;

namespace UnityEngine.UIElements {

  [UxmlElement("Entry")]
  public partial class EntryElement : VisualElement {

    private readonly VisualElement _entryRoot;
    private readonly IEntryService _entryService;
    private readonly Label _selectedName;
    private readonly Image _selectedIcon;
    private readonly Image _pingIcon;
    private readonly Image _openPrefabIcon;
    private readonly Image _favoriteIcon;
    private readonly VisualElement _entryPopupRoot;
    private bool _isFavorite = false;

    public int Index { get; set; }
    public string EntryLabel => _selectedName.text;

    private Entry _entry;
    public Entry Entry {
      get => _entry;
      set => SetupEntry(value);
    }

    public EntryElement() {

      _entryRoot = UIAssetManager.instance.entryTemplate.Instantiate();

      this.AddManipulator(new ContextualMenuManipulator(evt => {
        evt.menu.AppendAction("Ping", (_) => PingEntry(), DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Remove", _ => _entryService.RemoveEntry(Entry), DropdownMenuAction.AlwaysEnabled);
        evt.StopPropagation();
      }));

      VisualElement info = _entryRoot.Q<VisualElement>("Info");
      info?.AddManipulator(new DragAndLeftClickManipulator(this));
      // info?.AddManipulator(new OnHoverManipulator(this));

      RegisterCallback<AttachToPanelEvent>(evt => {
        PreferencePersistence.instance.onUpdated += PreferenceUpdatedCallback;
      });
      RegisterCallback<DetachFromPanelEvent>(evt => {
        PreferencePersistence.instance.onUpdated -= PreferenceUpdatedCallback;
      });

      _selectedName = _entryRoot.Q<Label>("Name");
      _selectedIcon = _entryRoot.Q<Image>("Icon");
      _pingIcon = _entryRoot.Q<Image>("PingIcon");
      _openPrefabIcon = _entryRoot.Q<Image>("OpenPrefabIcon");
      _favoriteIcon = _entryRoot.Q<Image>("FavoriteIcon");
      _entryPopupRoot = _entryRoot.Q<VisualElement>("PopupDetail");

      if (_pingIcon != null) {
        _pingIcon.image = EditorGUIUtility.IconContent(SEARCH_ICON_NAME).image;
        _pingIcon.RegisterCallback<MouseUpEvent>(PingIconCallback);
      }

      if (_openPrefabIcon != null) {
        _openPrefabIcon.image = EditorGUIUtility.IconContent(OPEN_ASSET_ICON_NAME).image;
        _openPrefabIcon.RegisterCallback<MouseUpEvent>(PrefabIconCallback);
      }

      if (_favoriteIcon != null) {
        _favoriteIcon.image = EditorGUIUtility.IconContent(FAVORITE_EMPTY_ICON_NAME).image;
        _favoriteIcon?.RegisterCallback<MouseUpEvent>(FavoriteIconCallback);
      }


      Add(_entryRoot);
    }

    public EntryElement(int index, IEntryService service) : this() {
      Index = index;
      _entryService = service;
    }

    public void Reset() {
      Entry = null;
    }

    public IEntryService GetEntryService() {
      return _entryService;
    }

    public void PopupDetail() {
      _entryPopupRoot.style.display = DisplayStyle.Flex;
    }

    public void HideDetail() {
      _entryPopupRoot.style.display = DisplayStyle.None;
    }

    private void FavoriteIconCallback(MouseUpEvent evt) {
      if (Entry == null) {
        return;
      }

      _isFavorite = !_isFavorite;
      EntryServicePersistence.instance.RecordFavorites(Entry, _isFavorite);
    }

    private void SetupEntry(Entry value) {
      if (value == null) {
        style.display = DisplayStyle.None;
        _entry?.onFavoriteChanged.RemoveListener(FavoriteChangedCallback);
        _entry = null;
        _selectedName.text = string.Empty;
        _selectedIcon.image = null;
        return;
      }

      _entry?.onFavoriteChanged.RemoveListener(FavoriteChangedCallback);
      _entry = value;
      _entry.onFavoriteChanged.AddListener(FavoriteChangedCallback);

      if (_selectedName != null) {
        SetNameLabel(value);
      }

      if (_selectedIcon != null) {
        _selectedIcon.image = value.Icon;
      }

      if (_openPrefabIcon != null) {
        _openPrefabIcon.style.display = value.IsGameObject
          ? DisplayStyle.None
          : DisplayStyle.Flex;
      }

      if (_favoriteIcon != null) {
        _isFavorite = value.IsFavorite;
        _favoriteIcon.image = _isFavorite
          ? EditorGUIUtility.IconContent(FAVORITE_ICON_NAME).image
          : EditorGUIUtility.IconContent(FAVORITE_EMPTY_ICON_NAME).image;
      }
    }

    private void SetNameLabel(Entry value) {
      if (Entry == null) {
        return;
      }

      if (Entry.IsAsset) {
        if (value.IsDeleted) {
          _selectedName.style.color = (StyleColor)DELETED_OR_DESTROYED_COLOR;
          _selectedName.text = "<s>" + value.Name + "</s>";
        } else {
          _selectedName.style.color = (StyleColor)Color.white;
          _selectedName.text = value.Name;
        }
      }

      if (Entry.IsGameObject) {
        string extName = string.IsNullOrEmpty(value.SceneName)
          ? value.Name
          : string.Concat(value.SceneName, "/", value.Name);
        _selectedName.text = value.GameObjectInstanceState == GameObjectState.Destroyed
          ? "<s>" + extName + "</s>"
          : extName;
        _selectedName.style.color = Entry.GameObjectInstanceState switch {
          GameObjectState.Loaded => (StyleColor)SCENE_OBJECT_COLOR,
          GameObjectState.Unloaded => (StyleColor)Color.grey,
          GameObjectState.Destroyed => (StyleColor)DELETED_OR_DESTROYED_COLOR,
          _ => (StyleColor)Color.white,
        };
      }
    }

    private void PingEntry() {
      if (Entry == null) {
        return;
      }

      Entry.Ping();
    }

    private void PingIconCallback(MouseUpEvent evt) {
      PingEntry();
      evt.StopPropagation();
    }

    private void PrefabIconCallback(MouseUpEvent evt) {
      if (Entry == null || Entry.Ref == null) {
        return;
      }

      if (!Entry.IsGameObject) {
        AssetDatabase.OpenAsset(Entry.Ref);
      } else if (Entry.GameObjectInstanceState == GameObjectState.Unloaded) {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
          EditorSceneManager.OpenScene(Entry.ScenePath);
        }
      }
      evt.StopPropagation();
    }

    private void FavoriteChangedCallback(bool value) {
      _favoriteIcon.image = value
        ? EditorGUIUtility.IconContent(FAVORITE_ICON_NAME).image
        : EditorGUIUtility.IconContent(FAVORITE_EMPTY_ICON_NAME).image;
    }

    private void PreferenceUpdatedCallback() {
      _favoriteIcon.style.display = PreferencePersistence.instance.GetToggleValue(DRAW_FAVORITES_KEY)
        ? DisplayStyle.Flex
        : DisplayStyle.None;
    }

  }
}
