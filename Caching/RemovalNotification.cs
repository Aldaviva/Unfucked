namespace Unfucked.Caching;

public delegate void RemovalNotification<in K, in V>(object sender, K key, V value, RemovalCause cause);

public enum RemovalCause {

    EXPLICIT,
    REPLACED,

    // Collected,
    EXPIRED,
    // Size

}

public static class Extensions {

    public static bool WasEvicted(this RemovalCause cause) => cause is /*RemovalCause.Collected or*/ RemovalCause.EXPIRED /*or RemovalCause.Size*/;

}