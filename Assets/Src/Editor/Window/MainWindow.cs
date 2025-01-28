using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Synaptafin.Editor.SelectionTracker {

  public class BaseWindow<T> : EditorWindow, IHasCustomMenu where T : IEntryService {

    public VisualTreeAsset rootVisualTreeAsset;

    protected T _entryService;
    protected VisualElement _windowRoot;

    private VisualElement _entryContainer;
    private readonly List<EntryElement> _entryElementsCache = new();
    private string _searchText;
    private GameObjectState _stateFilter = GameObjectState.All;

    public void OnEnable() {
      PreferencePersistence.instance.onUpdated += PreferencesUpdatedCallback;
      EntryServicePersistence.instance.TryGetService(out _entryService);
      _entryService?.OnUpdated.AddListener(EntryServiceUpdatedCallback);
    }

    public void OnDisable() {
      PreferencePersistence.instance.onUpdated -= PreferencesUpdatedCallback;
      _entryService?.OnUpdated.RemoveListener(EntryServiceUpdatedCallback);
    }

    public void CreateGUI() {
      if (_entryService == null) {
        return;
      }
      VisualElement root = rootVisualElement;
      if (rootVisualTreeAsset == null) {
        rootVisualTreeAsset = UIAssetManager.instance.TrackerTemplate;
      }
      _windowRoot = rootVisualTreeAsset.CloneTree();
      root.Add(_windowRoot);

      _windowRoot.style.width = new StyleLength(Length.Percent(100));
      _windowRoot.style.height = new StyleLength(Length.Percent(100));

      ToolbarSearchField searchBar = _windowRoot.Q<ToolbarSearchField>("SearchField");
      searchBar.RegisterValueChangedCallback((ChangeEvent<string> evt) => {
        _searchText = evt.newValue;
        ReloadView();
      });

      _entryContainer = _windowRoot.Q<VisualElement>("EntryContainer");

      for (int i = 0; i < _entryService.SizeLimit; i++) {
        EntryElement entryElement = new(i, _entryService);
        entryElement.style.display = DisplayStyle.None;
        _entryContainer.Add(entryElement);
        _entryElementsCache.Add(entryElement);
      }

      AddContextMenu();
      ReloadEntryList();
      ReloadView();
    }

    private void ReloadEntryList() {
      List<Entry> entries = _entryService.GetEntries;
      for (int i = 0; i < _entryElementsCache.Count; i++) {
        if (i < entries.Count) {
          _entryElementsCache[i].Entry = entries[i];
        } else {
          _entryElementsCache[i].Reset();
        }
      }
    }

    public void AddItemsToMenu(GenericMenu menu) {

      menu.AddItem(
        new GUIContent("Hide Unloaded", "Show Unloaded tooltips"),
        !_stateFilter.HasFlag(GameObjectState.Unloaded),
        () => ToggleStateFilterFlag(GameObjectState.Unloaded));
      menu.AddItem(new GUIContent("Clear/All", "Clear tooltips"), false, RemoveAll);
      menu.AddItem(new GUIContent("Clear/Deleted", "Clear deleted tooltips"), false, RemoveDeleted);
      menu.AddItem(new GUIContent("Clear/Destroyed", "Clear destroyed tooltips"), false, RemoveDestroyed);
    }

    public void AddContextMenu() {
      ContextualMenuManipulator contextMenuManipulator = new((evt) => {
        evt.menu.AppendAction("Show Unloaded", null, DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Show Destroyed", null, DropdownMenuAction.AlwaysEnabled);
        evt.menu.AppendAction("Clear", (_) => RemoveAll(), DropdownMenuAction.AlwaysEnabled);
      });
      _windowRoot.AddManipulator(contextMenuManipulator);
    }

    private void ReloadView() {
      foreach (EntryElement elt in _entryElementsCache) {
        bool show = elt.Entry != null && IsMatch(elt) && PassFilter(elt.Entry);
        elt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
      }
    }

    private bool IsMatch(EntryElement elt) {
      if (elt == null) {
        return false;
      }

      if (string.IsNullOrEmpty(_searchText)) {
        return true;
      }

      if (string.IsNullOrEmpty(elt.EntryLabel)) {
        return false;
      }

      string[] keywords = _searchText.Split(' ');
      bool isMatch = false;
      foreach (string keyword in keywords) {
        if (elt.EntryLabel.ToLower().Contains(keyword)) {
          isMatch = true;
          break;
        }
      }
      return isMatch;
    }

    private bool PassFilter(Entry entry) {
      if (entry == null) {
        return false;
      }

      if (_stateFilter != 0 && _stateFilter.HasFlag(entry.GameObjectInstanceState)) {
        return true;
      }

      if (_stateFilter == 0 && PreferencePersistence.instance.GlobalStateFilter == GameObjectState.All) {
        return true;
      }

      return false;
    }

    private void RemoveAll() {
      if (EditorUtility.DisplayDialog("Confirm", "Clear Records?", "Yes", "No")) {
        _entryService.RemoveAll();
      }
    }

    private void RemoveDeleted() {
      if (EditorUtility.DisplayDialog("Confirm", "Clear Deleted Records?", "Yes", "No")) {
        _entryService.RemoveAll(static (entry) => entry.IsDeleted);
      }
    }

    private void RemoveDestroyed() {
      if (EditorUtility.DisplayDialog("Confirm", "Clear Destroyed Records?", "Yes", "No")) {
        _entryService.RemoveAll(static (entry) => entry.GameObjectInstanceState == GameObjectState.Destroyed);
      }
    }

    private void ToggleStateFilterFlag(GameObjectState state) {

      if (_stateFilter.HasFlag(state)) {
        _stateFilter &= ~state;
      } else {
        _stateFilter |= state;
      }

      ReloadView();
    }

    private void PreferencesUpdatedCallback() {
      ReloadView();
    }

    private void EntryServiceUpdatedCallback() {
      ReloadEntryList();
      ReloadView();
    }

  }

  public class HistoryWindow : BaseWindow<HistoryService> { }
  public class MostVisitedWindow : BaseWindow<MostVisitedService> { }
  public class FavoritesWindow : BaseWindow<FavoritesService> {

    public new void OnEnable() {
      base.OnEnable();
      _entryService.OnUpdated.AddListener(OnFavoritesUpdated);
    }

    public new void CreateGUI() {
      base.CreateGUI();
      _entryService.StoreOriginalFavorites();
      _windowRoot.Q<VisualElement>("EditConfirm").style.display = DisplayStyle.Flex;

      Button _applyChangesButton = _windowRoot.Q<Button>("ApplyChanges");
      Button _discardChangesButton = _windowRoot.Q<Button>("DiscardChanges");

      _applyChangesButton.RegisterCallback<ClickEvent>((evt) => SaveChanges());
      _discardChangesButton.RegisterCallback<ClickEvent>((evt) => DiscardChanges());

      EnableEditConfirmButtons(false);
    }

    public void OnDestroy() {
      SaveChanges();
      _entryService.OnUpdated.RemoveListener(OnFavoritesUpdated);
    }

    public override void SaveChanges() {
      _entryService.ApplyChanges();
      EnableEditConfirmButtons(false);
      base.SaveChanges();
    }

    public override void DiscardChanges() {
      _entryService.DiscardChanges();
      EnableEditConfirmButtons(false);
      base.DiscardChanges();
    }

    private void OnFavoritesUpdated() {
      EnableEditConfirmButtons(true);
    }

    private void EnableEditConfirmButtons(bool enable) {
      _windowRoot.Q<Button>("ApplyChanges").SetEnabled(enable);
      _windowRoot.Q<Button>("DiscardChanges").SetEnabled(enable);
    }
  }
}
