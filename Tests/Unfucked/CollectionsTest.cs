using System.Collections.Concurrent;

namespace Tests.Unfucked;

public class CollectionsTest {

    [Fact]
    public void CompactValueTypes() {
        new List<int?> { 1, 2, null, 4 }.Compact().Should().Equal(1, 2, 4);
        new int?[] { 1, 2, null, 4 }.Compact().Should().Equal(1, 2, 4);
        new Dictionary<string, int?> { { "a", 1 }, { "b", null } }.Compact().Should().Equal(new Dictionary<string, int> { { "a", 1 } });
        new Dictionary<string, int?> { { "a", 1 }, { "b", null } }.AsEnumerable().Compact().Should().Equal(new Dictionary<string, int> { { "a", 1 } });
    }

    [Fact]
    public void CompactReferenceTypes() {
        new List<string?> { "a", "b", "c", null }.Compact().Should().Equal("a", "b", "c");
        new[] { "a", "b", "c", null }.Compact().Should().Equal("a", "b", "c");
        new Dictionary<string, string?> { { "a", "A" }, { "b", null } }.Compact().Should().Equal(new Dictionary<string, string> { { "a", "A" } });
        new Dictionary<string, string?> { { "a", "A" }, { "b", null } }.AsEnumerable().Compact().Should().Equal(new Dictionary<string, string> { { "a", "A" } });
    }

    [Fact]
    public void AddAll() {
        ICollection<int> dest = new List<int> { 1 };
        dest.AddAll([2, 3, 4]);
        dest.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task GetOrAdd() {
        IDictionary<int, string> dest = new Dictionary<int, string> { { 1, "a" }, { 2, "b" }, { 3, "b" } };

        dest.GetOrAdd(4, "d", out bool added).Should().Be("d");
        added.Should().BeTrue();
        dest.GetOrAdd(4, "d2", out added).Should().Be("d");
        added.Should().BeFalse();

        var valueFactory = A.Fake<Func<string>>();
        A.CallTo(() => valueFactory()).Returns("e");
        dest.GetOrAdd(5, valueFactory, out added).Should().Be("e");
        added.Should().BeTrue();
        A.CallTo(() => valueFactory()).MustHaveHappenedOnceExactly();
        dest.GetOrAdd(5, valueFactory, out added).Should().Be("e");
        added.Should().BeFalse();
        A.CallTo(() => valueFactory()).MustHaveHappenedOnceExactly();

        var asyncValueFactory = A.Fake<Func<Task<string>>>();
        A.CallTo(() => asyncValueFactory()).Returns("f");
        (string? value, added) = await dest.GetOrAdd(6, asyncValueFactory);
        value.Should().Be("f");
        added.Should().BeTrue();
        A.CallTo(() => asyncValueFactory()).MustHaveHappenedOnceExactly();
        (value, added) = await dest.GetOrAdd(6, asyncValueFactory);
        value.Should().Be("f");
        added.Should().BeFalse();
        A.CallTo(() => asyncValueFactory()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void SetMinus() {
        ISet<int> a = new HashSet<int> { 1, 2, 3 };
        a.Minus(new HashSet<int> { 2, 3 }).Should().Equal(1);
        a.Should().Equal([1, 2, 3], "original set should not be mutated");
    }

    [Fact]
    public void DistinctConsecutive() {
        new List<int> { 1, 2, 2, 3, 3, 3, 1, 3 }.DistinctConsecutive().Should().Equal(1, 2, 3, 1, 3);
    }

    [Fact]
    public async Task AsyncEnumeratorToList() {
        (await EnumerateAsync().ToList()).Should().Equal(1, 2, 3);
        (await EmptyEnumerateAsync().ToList()).Should().BeEmpty();
    }

    private static async IAsyncEnumerator<int> EnumerateAsync() {
        for (int i = 1; i <= 3; i++) {
            await Task.Delay(4);
            yield return i;
        }
    }

    private static async IAsyncEnumerator<int> EmptyEnumerateAsync() {
        await Task.Delay(4);
        yield break;
    }

    [Fact]
    public void FirstOrNullValueTypes() {
        Enumerable.Empty<int>().FirstOrNull().Should().BeNull();
        Enumerable.Empty<int>().Append(1).FirstOrNull().Should().Be(1);
        new List<int>().FirstOrNull().Should().BeNull();
        new List<int> { 1, 2, 3 }.FirstOrNull().Should().Be(1);

        new List<int>().FirstOrNull(i => i % 2 == 0).Should().BeNull();
        new List<int> { 1, 2, 3 }.FirstOrNull(i => i % 2 == 0).Should().Be(2);
    }

    [Fact]
    public void HeadAndTail() {
        (int? head, IEnumerable<int> tail) = new List<int> { 1, 2, 3 }.HeadAndTailStruct();
        head.Should().Be(1);
        tail.Should().Equal(2, 3);

        (int? head2, IEnumerable<int?> tail2) = new List<int?> { 1, 2, 3 }.HeadAndTailStruct();
        head2.Should().Be(1);
        tail2.Should().Equal(2, 3);

        (head, tail) = new List<int>().HeadAndTailStruct();
        head.Should().BeNull();
        tail.Should().BeEmpty();

        (head, tail) = new List<int> { 1 }.HeadAndTailStruct();
        head.Should().Be(1);
        tail.Should().BeEmpty();

        (string? head3, IEnumerable<string> tail3) = new List<string> { "a", "b", "c" }.HeadAndTail();
        head3.Should().Be("a");
        tail3.Should().Equal("b", "c");
    }

    [Fact]
    public void InterlockedConcurrentDictionary() {
        ConcurrentDictionary<int, Collections.ValueHolder<string>> stringDict = Collections.CreateConcurrentDictionary(new Dictionary<int, string> { { 1, "a" } });

        string oldStringValue = stringDict.Exchange(1, "aa");
        oldStringValue.Should().Be("a");
        stringDict[1].Value.Should().Be("aa");

        ConcurrentDictionary<int, Collections.ValueHolder<int>> intDict = Collections.CreateConcurrentDictionary(new Dictionary<int, int> { { 1, 100 } });

        int oldIntValue = intDict.Exchange(1, 101);
        oldIntValue.Should().Be(100);
        intDict[1].Value.Should().Be(101);

        ConcurrentDictionary<int, Collections.ValueHolder<long>> longDict = Collections.CreateConcurrentDictionary<int, long>(1, concurrency: 2);
        longDict[1] = new Collections.ValueHolder<long>(100L);

        long oldLongValue = longDict.Exchange(1, 101L);
        oldLongValue.Should().Be(100L);
        longDict[1].Value.Should().Be(101L);

        ConcurrentDictionary<int, Collections.ValueHolder<double>> doubleDict = Collections.CreateConcurrentDictionary<int, double>();
        doubleDict[1] = new Collections.ValueHolder<double>(100);

        double oldDoubleValue = doubleDict.Exchange(1, 101.0);
        oldDoubleValue.Should().Be(100.0);
        doubleDict[1].Value.Should().Be(101.0);

        ConcurrentDictionary<int, Collections.ValueHolder<int>> enumDict = Collections.CreateConcurrentDictionary(new Dictionary<int, int> { { 1, (int) MyEnum.A } });

        MyEnum oldEnumValue = enumDict.ExchangeEnum(1, MyEnum.B);
        oldEnumValue.Should().Be(MyEnum.A);
        enumDict[1].Value.Should().Be((int) MyEnum.B);
    }

    [Fact]
    public void DeltaWithPrimitives() {
        List<int> existing = [1, 2, 3];
        List<int> @new     = [1, 2, 4];

        (IEnumerable<int> created, IEnumerable<int> updated, IEnumerable<int> deleted, IEnumerable<int> unmodified) actual = existing.DeltaWith(@new);

        actual.created.Should().Equal(4);
        actual.updated.Should().BeEmpty("ints are immutable");
        actual.deleted.Should().Equal(3);
        actual.unmodified.Should().Equal(1, 2);
    }

    [Fact]
    public void DeltaWithObjects() {
        List<Person> existing = [new("A", 1), new("B", 2), new("C", 3)];
        List<Human>  @new     = [new("A", 1), new("B", 4), new("D", 5)];

        (IEnumerable<Human> created, IEnumerable<Human> updated, IEnumerable<Person> deleted, IEnumerable<Person> unmodified) actual = existing.DeltaWith(@new, p => p.Name, h => h.Name,
            (person, human) => person.Name.Equals(human.Name, StringComparison.CurrentCulture) && person.Age == human.Age);

        actual.created.Should().Equal(new Human("D", 5));
        actual.updated.Should().Equal(new Human("B", 4));
        actual.deleted.Should().Equal(new Person("C", 3));
        actual.unmodified.Should().Equal(new Person("A", 1));
    }

    private record Person(string Name, int Age);

    private record Human(string Name, int Age);

    private enum MyEnum {

        A,
        B,
        C

    }

}