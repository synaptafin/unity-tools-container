using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Synaptafin.Editor.SelectionTracker {

  [Serializable]
  public enum RefType {
    GameObject,
    Asset,
    None,
  }

  [Flags]
  [Serializable]
  public enum GameObjectState {
    NotGameObject = 0,
    Loaded = 1 << 0,
    Unloaded = 1 << 1,
    Destroyed = 1 << 2,  // No built-in GameObject destroy event
    Playing = 1 << 3,
    SceneInstance = Loaded | Unloaded,
    All = ~0
  }

  [Serializable]
  public enum PrefabType {
    PrefabAsset,
    PrefabInstance,
    None,
  }

  /// <summary>
  /// normal gameobject has THE SAME GlobalObjectId in runtime and edit mode
  /// prefab instance has DIFFERENT GlobalObjectId in runtime and edit mode, which means:
  ///   - prefab instance object in runtime mode can't be restored from GlobalObjectId
  /// </summary>
  [Serializable]
  public class Entry : IEquatable<Entry> {

    [SerializeField] private GlobalObjectId _unityId;

    [SerializeField] private Object _cachedRef;
    [SerializeField] private string _cachedName;
    [SerializeField] private RefType _cachedRefType;
    [SerializeField] private Texture _cachedRefIcon;
    [SerializeField] private Scene _cachedScene;

    [SerializeField] private string _sceneName;
    [SerializeField] private string _scenePath;

    [SerializeField] private PrefabType _prefabCachedInfo;

    [SerializeField] private bool _isFavorite = false;
    [SerializeField] private bool _isPlayModeObject;

    public Object Ref {
      get {
        TryRestoreAndCacheObject();
        return _cachedRef;
      }
    }

    public Scene Scene => _cachedScene;
    public UnityEvent<bool> onFavoriteChanged = new();

    public string Name => _cachedRef != null ? _cachedRef.name : _cachedName;
    public string SceneName => _sceneName;
    public string ScenePath => _scenePath;
    public Texture Icon => _cachedRefIcon;
    public bool IsGameObject => _cachedRefType == RefType.GameObject;
    public bool IsAsset => _cachedRefType == RefType.Asset;
    public bool IsDeleted => IsAsset && NotReferencing;

    public bool IsFavorite {
      get => _isFavorite;
      set {
        _isFavorite = value;
        onFavoriteChanged.Invoke(value);
      }
    }

    public GameObjectState GameObjectInstanceState {
      get {
        if (!IsGameObject) {
          return GameObjectState.NotGameObject;
        }

        TryRestoreAndCacheObject();
        if (NotReferencing) {
          if (!_cachedScene.isLoaded) {
            return _isPlayModeObject
              ? GameObjectState.Destroyed
              : GameObjectState.Unloaded;
          }
        } else if (_cachedRef is GameObject go) {
          if (go.scene.isLoaded) {
            return GameObjectState.Loaded;
          }
        }
        return GameObjectState.NotGameObject;
      }
    }

    private bool NotReferencing => _cachedRef == null && !string.IsNullOrEmpty(_unityId.ToString());

    public static Entry None => new(null);

    public Entry(Object obj) {
      if (obj == null) {
        return;
      }
      CacheRefInfo(obj);
    }

    // implement IEquatable.Equals
    public bool Equals(Entry other) {
      if (other is null) {
        return false;
      }
      // check whether address is the same
      if (ReferenceEquals(this, other)) {
        return true;
      }

      // this reference is null or other reference is null
      if (Ref == null || other.Ref == null) {
        return Equals(_unityId, other._unityId);
      } else {
      // check entry object reference whether is the same
        return Ref.Equals(other.Ref);
      }
    }

    // override object.Equals
    public override bool Equals(object obj) {
      return obj is Entry other && Equals(other);
    }

    public override int GetHashCode() {
      return Ref != null ? Ref.GetHashCode() : 0;
    }

    public void Ping() {
      if (IsAsset) {
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(Ref);
      }

      if (GameObjectInstanceState == GameObjectState.Unloaded) {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(sceneAsset);
      } else {
        EditorGUIUtility.PingObject(Ref);
      }
    }

    private void TryRestoreAndCacheObject() {
      if (_cachedRef != null) {
        return;
      }
      if (TryRestoreFromId(out _cachedRef)) {
        CacheRefInfo(_cachedRef);
        return;
      }
      return;
    }

    private bool TryRestoreFromId(out Object obj) {
      if (NotReferencing) {

        // Can not try to restore GameObject instance by globalObjectId in play mode
        if (Application.isPlaying) {
          obj = null;
          return false;
        }

        obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(_unityId);
        return obj != null;
      }
      obj = null;
      return false;
    }

    // cache for scene switching or editor session closed
    private void CacheRefInfo(Object obj) {
      if (obj == null) {
        return;
      }

      _unityId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
      _cachedRef = obj;
      _cachedName = obj.name;
      _cachedRefIcon = AssetPreview.GetMiniThumbnail(obj);

      if (PrefabUtility.IsPartOfPrefabInstance(obj)) {
        _prefabCachedInfo = PrefabType.PrefabInstance;
      } else if (PrefabUtility.IsPartOfPrefabAsset(obj)) {
        _prefabCachedInfo = PrefabType.PrefabAsset;
      }

      if (!PrefabUtility.IsPartOfAnyPrefab(obj)) {
        _prefabCachedInfo = PrefabType.None;
      }

      if (_unityId.identifierType == 2 && _cachedRef is GameObject go) {
        _cachedRefType = RefType.GameObject;
        _isPlayModeObject = Application.isPlaying;
        if (go.scene != null) {
          _cachedScene = go.scene;
          _sceneName = go.scene.name;
          _scenePath = go.scene.path;
        }
      } else if (_unityId.identifierType is 1 or 3) {
        _cachedRefType = RefType.Asset;
      }
    }
  }
}

