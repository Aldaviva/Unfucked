using System.Globalization;

namespace Tests.Windows;

public class CultureInfo2Test {

    [Fact]
    public void CurrentMachineCulture() {
        CultureInfo actual = CultureInfo2.CurrentMachineCulture;

        actual.Name.Should().Be("en-US");
        CultureInfo2.CurrentMachineCulture.Should().BeSameAs(actual); // don't regenerate on each call
    }

}