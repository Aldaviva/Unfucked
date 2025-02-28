using System.Globalization;

namespace Tests.Windows;

public class CultureTest {

    [Fact]
    public void CurrentMachineCulture() {
        CultureInfo actual = Cultures.CurrentMachineCulture;

        actual.Name.Should().Be("en-US");
        Cultures.CurrentMachineCulture.Should().BeSameAs(actual); // don't regenerate on each call
    }

}