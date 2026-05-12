using System.Reflection;

namespace Tests.Unfucked;

public class EventsTest {

    [Fact]
    public void GetGenericMethodWithReflection() {
        Type        subscriberClass = typeof(EventsTest);
        MethodInfo? methodInfo      = subscriberClass.GetMethod(nameof(GetDefault), BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
        methodInfo.Should().NotBeNull();
        MethodInfo longMethod = methodInfo.MakeGenericMethod(typeof(long));
        long       result     = (long) longMethod.Invoke(null, null);
        result.Should().Be(0L);
    }

    private static T? GetDefault<T>() => default;

}