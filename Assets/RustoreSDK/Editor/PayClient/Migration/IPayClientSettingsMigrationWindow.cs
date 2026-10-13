#if UNITY_EDITOR
using System;
using UnityEditor;

namespace RuStore.Editor {

    internal interface IPayClientSettingsMigrationWindow {
        void Show(PayClientSettings settings);
        event Action OnMigrationComplete;
    }
}
#endif
