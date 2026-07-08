using UnityEngine;

public static class GameEventBusBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureEventBus() {
        GameEventBus.Ensure();
    }
}
