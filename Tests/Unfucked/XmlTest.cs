using System.Xml.Linq;

namespace Tests.Unfucked;

public class XmlTest {

    [Fact]
    public void Descendants() {
        // lang=xml
        const string xml =
            """
            <Root>
                <Parent>
                    <Child1 name="a" />
                    <Child2 name="b" />
                </Parent>
                <Parent>
                    <Child1 name="c" />
                </Parent>
                <Grandparent>
                    <Parent>
                        <Child2 name="d" />
                    </Parent>
                </Grandparent>
            </Root>               
            """;

        XDocument doc = XDocument.Parse(xml);

        IEnumerable<XElement> actual = doc.Descendants("Child1", "Child2").ToList();
        actual.Should().Contain(el => el.Name == "Child1" && el.Attribute("name")!.Value == "a");
        actual.Should().Contain(el => el.Name == "Child2" && el.Attribute("name")!.Value == "b");
        actual.Should().Contain(el => el.Name == "Child1" && el.Attribute("name")!.Value == "c");
        actual.Should().Contain(el => el.Name == "Child2" && el.Attribute("name")!.Value == "d");
    }

    [Fact]
    public async Task ReadXmlFromHttpResponse() {
        // lang=xml
        const string xml =
            """
            <Root name="a" />
            """;

        using HttpContent httpContent = new ByteArrayContent(xml.ToBytes());

        XDocument doc = await httpContent.ReadFromXmlAsync();
        doc.Root!.Attribute("name")!.Value.Should().Be("a");
    }

}