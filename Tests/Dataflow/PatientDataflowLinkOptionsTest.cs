using System.Threading.Tasks.Dataflow;
using Unfucked.Dataflow;

namespace Tests.Dataflow;

public class PatientDataflowLinkOptionsTest {

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task PatientLink(bool patient, int expectedLength) {
        BufferBlock<int> source1 = new(), source2 = new(), target = new();

        PatientDataflowLinkOptions opts = new() {
            PropagateCompletion                     = true,
            PropagatingCompletionWaitsForAllSources = patient
        };

        source1.LinkTo(target, opts);
        source2.LinkTo(target, opts);

        source1.Post(1);
        source1.Complete();

        _ = Task.Run(async () => {
            await Task.Delay(500);
            source2.Post(2);
            source2.Complete();
        });

        List<int> actual = await target.ReceiveAllAsync().ToListAsync();

        actual.Should().HaveCount(expectedLength);
    }

}