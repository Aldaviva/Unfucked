namespace Tests;

public class CollectionsTest {

    [Fact]
    public void CompactValueTypes() {
        new List<int?> { 1, 2, null, 4 }.Compact().Should().Equal(1, 2, 4);
    }

    [Fact]
    public void CompactReferenceTypes() {
        new List<string?> { "a", "b", "c", null }.Compact().Should().Equal("a", "b", "c");
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
        List<Person> existing = [new Person("A", 1), new Person("B", 2), new Person("C", 3)];
        List<Human>  @new     = [new Human("A", 1), new Human("B", 4), new Human("D", 5)];

        (IEnumerable<Human> created, IEnumerable<Human> updated, IEnumerable<Person> deleted, IEnumerable<Person> unmodified) actual = existing.DeltaWith(@new, p => p.Name, h => h.Name,
            (person, human) => person.Name.Equals(human.Name, StringComparison.CurrentCulture) && person.Age == human.Age);

        actual.created.Should().Equal(new Human("D", 5));
        actual.updated.Should().Equal(new Human("B", 4));
        actual.deleted.Should().Equal(new Person("C", 3));
        actual.unmodified.Should().Equal(new Person("A", 1));
    }

    private record Person(string Name, int Age);
    private record Human(string  Name, int Age);

}