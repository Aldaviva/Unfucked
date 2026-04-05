namespace Unfucked.Caching;

public delegate void RemovalNotification<in K, in V>(object sender, K key, V value, RemovalCause cause);

public enum RemovalCause {

    Explicit,
    Replaced,

    // Collected,
    Expired,
    // Size

}

public static class Extensions {

    public static bool WasEvicted(this RemovalCause cause) => cause is /*RemovalCause.Collected or*/ RemovalCause.Expired /*or RemovalCause.Size*/;

}