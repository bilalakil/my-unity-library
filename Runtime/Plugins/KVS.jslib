mergeInto(LibraryManager.library, {
  SyncIndexedDB: function () {
    FS.syncfs(false, function (err) {});
  }
});