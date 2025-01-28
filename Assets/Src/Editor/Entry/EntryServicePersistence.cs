using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Synaptafin.Editor.SelectionTracker {

  [FilePath("UserSettings/SelectionTracker.EntryManager.asset", FilePathAttribute.Location.ProjectFolder)]
  public class EntryServicePersistence : ScriptableSingleton<EntryServicePersistence> {

    [SerializeField]
    private List<IEntryService> _entryServices = new()
    {
      new HistoryService(),
      new MostVisitedService(),
      new FavoritesService(),
    };

    private Dictionary<string, IEntryService> ServiceDict => _entryServices.ToDictionary(static service => service.GetType().Name);

    public List<IEntryService> EntryServices => _entryServices;
    public void OnEnable() {
      foreach (IEntryService entryService in EntryServices) {
        entryService?.OnUpdated.AddListener(OnServiceUpdate);
      }
    }

    public void RecordSelection(Entry selection) {
      ServiceDict[nameof(HistoryService)]?.RecordEntry(selection);
      ServiceDict[nameof(MostVisitedService)]?.RecordEntry(selection);
      Save(true);
    }

    public void RecordFavorites(Entry entry, bool isFavorite = false) {
      (ServiceDict[nameof(FavoritesService)] as FavoritesService)?.RecordEntry(entry, isFavorite);
      Save(true);
    }

    public void RemoveFromFavorites(Entry entry) {
      ServiceDict[nameof(FavoritesService)]?.RemoveEntry(entry);
      Save(true);
    }

    public Entry JumpToPreviousSelection() {
      return (ServiceDict[nameof(HistoryService)] as HistoryService)?.PreviousSelection();
    }

    public Entry JumpToNextSelection() {
      return (ServiceDict[nameof(HistoryService)] as HistoryService)?.NextSelection();
    }

    public void OnDisable() {
      foreach (IEntryService entryService in EntryServices) {
        entryService?.OnUpdated.RemoveListener(OnServiceUpdate);
      }
    }

    public T GetService<T>() where T : IEntryService {
      return (T)ServiceDict[typeof(T).Name];
    }

    private void OnServiceUpdate() {
      Save(true);
    }
  }
}

