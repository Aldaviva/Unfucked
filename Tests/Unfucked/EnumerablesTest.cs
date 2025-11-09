using System.Collections.Concurrent;

namespace Tests.Unfucked;

public class EnumerablesTest {

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
        dest.AddAll(2, 3, 4);
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
        (string value, added) = await dest.GetOrAdd(6, asyncValueFactory);
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
        new int?[] { 1, 1, 2, null, 2, null, null, 3 }.DistinctConsecutive().Should().Equal(1, 2, null, 2, null, 3);
    }

    [Fact]
    public void DistinctConsecutiveByDerivedValue() {
        new[] { 1, 2, -2, 3, -3, 3, 1, -3 }.DistinctConsecutive(Math.Abs).Should().Equal(1, 2, 3, 1, -3);
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
        ConcurrentDictionary<int, ValueHolder<string>> stringDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, string> { { 1, "a" } });

        string? oldStringValue = stringDict.Swap(1, "aa");
        oldStringValue.Should().Be("a");
        stringDict[1].Value.Should().Be("aa");
        stringDict.Swap(2, "bb").Should().BeNull("the key was not previously in the dictionary");
        stringDict[2].Value.Should().Be("bb");
        stringDict.CompareAndSwap(2, "bb", "BB").Should().Be("bb");
        stringDict[2].Value.Should().Be("BB");
        stringDict.CompareAndSwap(3, "cc", "CC").Should().BeNull("the key was not previously in the dictionary");
        stringDict.Should().NotContainKey(3, "CompareAndSwap does not insert values, since the comparison would always fail");

        ConcurrentDictionary<int, ValueHolder<string?>> nullableReferenceDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, string?> { { 1, "a" } });

        oldStringValue = nullableReferenceDict.SwapNullable(1, "aa");
        oldStringValue.Should().Be("a");
        nullableReferenceDict[1].Value.Should().Be("aa");
        oldStringValue = nullableReferenceDict.SwapNullable(1, null);
        oldStringValue.Should().Be("aa");
        nullableReferenceDict[1].Value.Should().BeNull();
        nullableReferenceDict.SwapNullable(2, "bb").Should().BeNull("the key was not previously in the dictionary");
        nullableReferenceDict[2].Value.Should().Be("bb");
        nullableReferenceDict.CompareAndSwapNullable(2, "bb", "BB").Should().Be("bb");
        nullableReferenceDict[2].Value.Should().Be("BB");
        nullableReferenceDict.SwapNullable(3, null).Should().BeNull("the key was not previously in the dictionary");
        nullableReferenceDict[3].Value.Should().BeNull();
        nullableReferenceDict.CompareAndSwapNullable(4, null, "DD").Should().BeNull("CompareAndSwap does not insert values, since the comparison would always fail");

        ConcurrentDictionary<int, ValueHolder<int>> intDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, int> { { 1, 100 } });

        int? oldIntValue = intDict.Swap(1, 101);
        oldIntValue.Should().Be(100);
        intDict[1].Value.Should().Be(101);
        intDict.Swap(2, 201).Should().BeNull("the key was not previously in the dictionary");
        intDict[2].Value.Should().Be(201);
        intDict.CompareAndSwap(2, 201, 202).Should().Be(201);
        intDict[2].Value.Should().Be(202);

        ConcurrentDictionary<int, ValueHolder<long>> longDict = Enumerables.CreateConcurrentDictionary<int, long>(capacity: 1, concurrency: 2);
        longDict[1] = new ValueHolder<long>(100L);

        long? oldLongValue = longDict.Swap(1, 101L);
        oldLongValue.Should().Be(100L);
        longDict[1].Value.Should().Be(101L);
        longDict.Swap(2, 201L).Should().BeNull("the key was not previously in the dictionary");
        longDict[2].Value.Should().Be(201L);
        longDict.CompareAndSwap(2, 201L, 202L).Should().Be(201L);
        longDict[2].Value.Should().Be(202L);

        ConcurrentDictionary<int, ValueHolder<double>> doubleDict = Enumerables.CreateConcurrentDictionary<int, double>();
        doubleDict[1] = new ValueHolder<double>(100);

        double? oldDoubleValue = doubleDict.Swap(1, 101.0);
        oldDoubleValue.Should().Be(100.0);
        doubleDict[1].Value.Should().Be(101.0);
        doubleDict.Swap(2, 201.0).Should().BeNull("the key was not previously in the dictionary");
        doubleDict[2].Value.Should().Be(201.0);
        doubleDict.CompareAndSwap(2, 201.0, 202.0).Should().Be(201.0);
        doubleDict[2].Value.Should().Be(202.0);

        ConcurrentDictionary<int, ValueHolder<float>> floatDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, float> { { 1, 100.0f } });

        float? oldFloatValue = floatDict.Swap(1, 101.0f);
        oldFloatValue.Should().Be(100.0f);
        floatDict[1].Value.Should().Be(101.0f);
        floatDict.Swap(2, 201.0f).Should().BeNull("the key was not previously in the dictionary");
        floatDict[2].Value.Should().Be(201.0f);
        floatDict.CompareAndSwap(2, 201.0f, 202.0f).Should().Be(201.0f);
        floatDict[2].Value.Should().Be(202.0f);

        ConcurrentDictionary<int, ValueHolder<IntPtr>> intPtrDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, IntPtr> { { 1, new IntPtr(100) } });

        IntPtr? oldIntPtrValue = intPtrDict.Swap(1, new IntPtr(101));
        oldIntPtrValue!.Value.Should().Be(new IntPtr(100));
        intPtrDict[1].Value.Should().Be(new IntPtr(101));
        intPtrDict.Swap(2, new IntPtr(201)).Should().BeNull("the key was not previously in the dictionary");
        intPtrDict[2].Value.Should().Be(new IntPtr(201));
        intPtrDict.CompareAndSwap(2, new IntPtr(201), new IntPtr(202)).Should().Be(new IntPtr(201));
        intPtrDict[2].Value.Should().Be(new IntPtr(202));

        ConcurrentDictionary<int, ValueHolder<UIntPtr>> uintPtrDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, UIntPtr> { { 1, new UIntPtr(100) } });

        UIntPtr? oldUIntPtrValue = uintPtrDict.Swap(1, new UIntPtr(101));
        oldUIntPtrValue!.Value.Should().Be(new UIntPtr(100));
        uintPtrDict[1].Value.Should().Be(new UIntPtr(101));
        uintPtrDict.Swap(2, new UIntPtr(201)).Should().BeNull("the key was not previously in the dictionary");
        uintPtrDict[2].Value.Should().Be(new UIntPtr(201));
        uintPtrDict.CompareAndSwap(2, new UIntPtr(201), new UIntPtr(202)).Should().Be(new UIntPtr(201));
        uintPtrDict[2].Value.Should().Be(new UIntPtr(202));

        ConcurrentDictionary<int, BooleanValueHolder> boolDict = Enumerables.CreateConcurrentBooleanDictionary(new Dictionary<int, bool> { { 1, false } });

        bool? oldBoolValue = boolDict.Swap(1, true);
        oldBoolValue.Should().BeFalse();
        boolDict[1].Value.Should().BeTrue();
        boolDict.Swap(2, true).Should().BeNull("the key was not previously in the dictionary");
        boolDict[2].Value.Should().BeTrue();
        boolDict[2].Value = false;
        boolDict[2].Value.Should().BeFalse();
        boolDict.CompareAndSwap(2, false, true).Should().BeFalse();
        boolDict[2].Value.Should().BeTrue();

        ConcurrentDictionary<int, EnumValueHolder<IntEnum, int>> intEnumDict =
            Enumerables.CreateConcurrentEnumDictionary<int, IntEnum, int>(new Dictionary<int, IntEnum> { { 1, IntEnum.A } });

        IntEnum? oldIntEnumValue = intEnumDict.Swap(1, IntEnum.B);
        oldIntEnumValue.Should().Be(IntEnum.A);
        intEnumDict[1].Value.Should().Be(IntEnum.B);
        intEnumDict.Swap(2, IntEnum.C).Should().BeNull("the key was not previously in the dictionary");
        intEnumDict[2].Value.Should().Be(IntEnum.C);
        intEnumDict[2].Value = IntEnum.A;
        intEnumDict[2].Value.Should().Be(IntEnum.A);
        intEnumDict.CompareAndSwap(2, IntEnum.A, IntEnum.B).Should().Be(IntEnum.A);
        intEnumDict[2].Value.Should().Be(IntEnum.B);

        ConcurrentDictionary<int, EnumValueHolder<UIntEnum, uint>> uintEnumDict =
            Enumerables.CreateConcurrentEnumDictionary<int, UIntEnum, uint>(new Dictionary<int, UIntEnum> { { 1, UIntEnum.A } });

        UIntEnum? oldUIntEnumValue = uintEnumDict.Swap(1, UIntEnum.B);
        oldUIntEnumValue.Should().Be(UIntEnum.A);
        uintEnumDict[1].Value.Should().Be(UIntEnum.B);
        uintEnumDict.Swap(2, UIntEnum.C).Should().BeNull("the key was not previously in the dictionary");
        uintEnumDict[2].Value.Should().Be(UIntEnum.C);
        uintEnumDict.CompareAndSwap(2, UIntEnum.C, UIntEnum.B).Should().Be(UIntEnum.C);
        uintEnumDict[2].Value.Should().Be(UIntEnum.B);

        ConcurrentDictionary<int, EnumValueHolder<LongEnum, long>> longEnumDict =
            Enumerables.CreateConcurrentEnumDictionary<int, LongEnum, long>(new Dictionary<int, LongEnum> { { 1, LongEnum.A } });

        LongEnum? oldLongEnumValue = longEnumDict.Swap(1, LongEnum.B);
        oldLongEnumValue.Should().Be(LongEnum.A);
        longEnumDict[1].Value.Should().Be(LongEnum.B);
        longEnumDict.Swap(2, LongEnum.C).Should().BeNull("the key was not previously in the dictionary");
        longEnumDict[2].Value.Should().Be(LongEnum.C);
        longEnumDict.CompareAndSwap(2, LongEnum.C, LongEnum.B).Should().Be(LongEnum.C);
        longEnumDict[2].Value.Should().Be(LongEnum.B);

        ConcurrentDictionary<int, EnumValueHolder<ULongEnum, ulong>> ulongEnumDict =
            Enumerables.CreateConcurrentEnumDictionary<int, ULongEnum, ulong>(new Dictionary<int, ULongEnum> { { 1, ULongEnum.A } });

        ULongEnum? oldULongEnumValue = ulongEnumDict.Swap(1, ULongEnum.B);
        oldULongEnumValue.Should().Be(ULongEnum.A);
        ulongEnumDict[1].Value.Should().Be(ULongEnum.B);
        ulongEnumDict.Swap(2, ULongEnum.C).Should().BeNull("the key was not previously in the dictionary");
        ulongEnumDict[2].Value.Should().Be(ULongEnum.C);
        ulongEnumDict.CompareAndSwap(2, ULongEnum.C, ULongEnum.B).Should().Be(ULongEnum.C);
        ulongEnumDict[2].Value.Should().Be(ULongEnum.B);

        ConcurrentDictionary<int, ValueHolder<uint>> uintDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, uint> { { 1, 100u } });

        uint? oldUintValue = uintDict.Swap(1, 101u);
        oldUintValue.Should().Be(100u);
        uintDict[1].Value.Should().Be(101u);
        uintDict.Swap(2, 201u).Should().BeNull("the key was not previously in the dictionary");
        uintDict[2].Value.Should().Be(201u);
        uintDict.CompareAndSwap(2, 201u, 202u).Should().Be(201u);
        uintDict[2].Value.Should().Be(202u);

        ConcurrentDictionary<int, ValueHolder<ulong>> ulongDict = Enumerables.CreateConcurrentDictionary(new Dictionary<int, ulong> { { 1, 100uL } });

        ulong? oldUlongValue = ulongDict.Swap(1, 101uL);
        oldUlongValue.Should().Be(100uL);
        ulongDict[1].Value.Should().Be(101uL);
        ulongDict.Swap(2, 201u).Should().BeNull("the key was not previously in the dictionary");
        ulongDict[2].Value.Should().Be(201uL);
        ulongDict.CompareAndSwap(2, 201uL, 202uL).Should().Be(201uL);
        ulongDict[2].Value.Should().Be(202uL);
    }

    [Fact]
    public void WrongUnderlyingEnumIntegralType() {
        Action thrower = () => _ = Enumerables.CreateConcurrentEnumDictionary<int, ULongEnum, long>(new Dictionary<int, ULongEnum> { { 1, ULongEnum.A } });
        thrower.Should().Throw<InvalidCastException>();

        thrower = () => _ = new EnumValueHolder<ULongEnum, long>(ULongEnum.A);
        thrower.Should().Throw<InvalidCastException>();
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

    private enum IntEnum {

        A,
        B,
        C

    }

    private enum LongEnum: long {

        A,
        B,
        C

    }

    private enum UIntEnum: uint {

        A,
        B,
        C

    }

    private enum ULongEnum: ulong {

        A,
        B,
        C

    }

    [Fact]
    public void GetOrAddWithDisposalCreatedAndAdded() {
        Disposable created    = new();
        var        dictionary = new ConcurrentDictionary<int, Disposable>();

        dictionary.GetOrAddWithDisposal(0, _ => created, out bool added);

        added.Should().BeTrue();
        dictionary[0].Should().BeSameAs(created);
        dictionary[0].IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void GetOrAddWithDisposalNotCreatedAndNotAdded() {
        Disposable created = new();
        var dictionary = new ConcurrentDictionary<int, Disposable> {
            [0] = new()
        };

        dictionary[0].IsDisposed.Should().BeFalse();
        bool factoryRun = false;
        dictionary.GetOrAddWithDisposal(0, _ => {
            factoryRun = true;
            return created;
        }, out bool added);

        added.Should().BeFalse();
        dictionary[0].Should().NotBeSameAs(created);
        dictionary[0].IsDisposed.Should().BeFalse();
        factoryRun.Should().BeFalse("key already existed in the dictionary");
        created.IsDisposed.Should().BeFalse("factory was never run");
    }

    [Fact]
    public void GetOrAddWithDisposalCreatedButNotAdded() {
        Disposable created = new();
        Disposable added   = new();

        var dictionary = new ConcurrentDictionary<int, Disposable>();

        dictionary.GetOrAddWithDisposal(0, key => {
            dictionary[key] = added;
            return created;
        }, out bool isAdded);

        isAdded.Should().BeFalse();
        dictionary[0].Should().BeSameAs(added);
        dictionary[0].Should().NotBeSameAs(created);
        added.IsDisposed.Should().BeFalse();
        created.IsDisposed.Should().BeTrue("factory was run but value was not added");
    }

    private class Disposable: IDisposable {

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;

    }

    [Fact]
    public void GetValueOrNullStruct() {
        var map = new Dictionary<string, int> { { "a", 1 } };
        map.GetValueOrNullStruct("a").Should().Be(1);
        map.GetValueOrNullStruct("b").Should().BeNull();

        IDictionary<string, int> imap = map;
        imap.GetValueOrNullStruct("a").Should().Be(1);
        imap.GetValueOrNullStruct("b").Should().BeNull();

        IReadOnlyDictionary<string, int> romap = map;
        romap.GetValueOrNullStruct("a").Should().Be(1);
        romap.GetValueOrNullStruct("b").Should().BeNull();

        var nullableMap = new Dictionary<string, int?> { { "a", 1 } };
        nullableMap.GetValueOrNullStruct("a").Should().Be(1);
        nullableMap.GetValueOrNullStruct("b").Should().BeNull();

        IDictionary<string, int?> nullableImap = nullableMap;
        nullableImap.GetValueOrNullStruct("a").Should().Be(1);
        nullableImap.GetValueOrNullStruct("b").Should().BeNull();

        IReadOnlyDictionary<string, int?> nullableRomap = nullableMap;
        nullableRomap.GetValueOrNullStruct("a").Should().Be(1);
        nullableRomap.GetValueOrNullStruct("b").Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefaultClass() {
        var map = new Dictionary<string, string> { { "a", "1" } };
        map.GetValueOrNull("a").Should().Be("1");
        map.GetValueOrNull("b").Should().BeNull();

        IDictionary<string, string> imap = map;
        imap.GetValueOrNull("a").Should().Be("1");
        imap.GetValueOrNull("b").Should().BeNull();

        IReadOnlyDictionary<string, string> romap = map;
        romap.GetValueOrNull("a").Should().Be("1");
        romap.GetValueOrNull("b").Should().BeNull();
    }

}