using UnityEngine;

public static class NotificationFeedBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureNotificationFeed() {
        NotificationFeed.Ensure();
    }
}
