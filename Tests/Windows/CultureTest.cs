using System.Globalization;

namespace Tests.Windows;

public class CultureTest {

    [Fact]
    public void CurrentMachineCulture() {
        CultureInfo actual = CultureInfo.CurrentMachineCulture;

        actual.Name.Should().Be("en-US");
        CultureInfo.CurrentMachineCulture.Should().BeSameAs(actual); // don't regenerate on each call
    }

}