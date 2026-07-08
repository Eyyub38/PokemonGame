public static class GameDebugBootstrap {
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize() {
        GameDebugLogger.Ensure();
    }
}
