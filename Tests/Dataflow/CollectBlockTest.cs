using System.Threading.Tasks.Dataflow;
using Unfucked.Dataflow;

namespace Tests.Dataflow;

public class CollectBlockTest {

    [Fact]
    public void CollectAll() {
        CollectBlock<int> collectBlock = new();

        collectBlock.Post(1);
        collectBlock.Post(2);
        collectBlock.Post(3);
        collectBlock.Complete();

        IReadOnlyCollection<int> joined = collectBlock.Receive();
        joined.Should().Equal(1, 2, 3);
    }

}