using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.DvosTools.blogger.Service
{
    public class TerminalValueTracker : MonoBehaviour
    {
        private static TerminalValueTracker _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null)
                return;

            var go = new GameObject("TerminalValueTracker");
            _instance = go.AddComponent<TerminalValueTracker>();
            DontDestroyOnLoad(go);

            SceneManager.sceneLoaded += OnSceneLoaded;
            ObjectFactory.componentWasAdded += OnComponentAdded;

            ScanCurrentScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ScanCurrentScene();
        }

        private static void OnComponentAdded(Component component)
        {
            if (component is MonoBehaviour mb)
            {
                var registry = TerminalValueRegistry.Instance;
                var type = mb.GetType();
                
                if (registry.IsAggregateType(type))
                {
                    registry.RegisterInstance(mb);
                }
            }
        }

        private static void ScanCurrentScene()
        {
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var registry = TerminalValueRegistry.Instance;

            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null)
                    continue;

                var type = mb.GetType();
                if (registry.IsAggregateType(type))
                {
                    registry.RegisterInstance(mb);
                }
            }

            Debug.Log($"[TerminalValueTracker] Scanned scene, found {allMonoBehaviours.Length} MonoBehaviours");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ObjectFactory.componentWasAdded -= OnComponentAdded;
        }
    }
}
