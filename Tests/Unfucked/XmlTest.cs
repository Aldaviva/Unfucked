using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Tests.Unfucked;

public class XmlTest {

    // lang=xml
    private const string Xml =
        """
        <Root name="a" />
        """;

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
    public async Task ReadXmlLinqFromHttpResponse() {
        using HttpContent httpContent = new ByteArrayContent(Xml.ToByteArray());

        XDocument doc = await httpContent.ReadLinqFromXmlAsync();
        doc.Root!.Attribute("name")!.Value.Should().Be("a");
    }

    [Fact]
    public async Task ReadXmlDomFromHttpResponse() {
        using HttpContent httpContent = new ByteArrayContent(Xml.ToByteArray());

        XmlDocument doc = await httpContent.ReadDomFromXmlAsync();
        doc.DocumentElement!.GetAttribute("name").Should().Be("a");
    }

    [Fact]
    public async Task ReadXmlMappingFromHttpResponse() {
        using HttpContent httpContent = new ByteArrayContent(Xml.ToByteArray());

        XmlObject doc = await httpContent.ReadObjectFromXmlAsync<XmlObject>();
        doc.name.Should().Be("a");
    }

    [Fact]
    public async Task ReadXpathFromHttpResponse() {
        using HttpContent httpContent = new ByteArrayContent(Xml.ToByteArray());

        XPathNavigator doc = await httpContent.ReadXPathFromXmlAsync();
        doc.SelectSingleNode("/Root/@name")!.Value.Should().Be("a");
    }

    [XmlRoot("Root")]
    public class XmlObject {

        [XmlAttribute("name")]
        public string name { get; set; }

    }

}