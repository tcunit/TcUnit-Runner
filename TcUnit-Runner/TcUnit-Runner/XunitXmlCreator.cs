using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TcUnit.TcUnit_Runner
{
    /// <summary>
    /// This class is used to create the xUnit XML-output for others to consume.
    /// The specification that has been followed is Dirk Jagdmann's specification
    /// available at https://llg.cubic.org/docs/junit/
    /// </summary>
    class XunitXmlCreator
    {

        // Singleton constructor
        private XunitXmlCreator()
        { }

        public static string GetXmlString(int numberOfTestSuites, int numberOfTests, int numberOfSuccessfulTests, int numberOfFailedTests)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //XmlElement rootNode = xmlDoc.DocumentElement;
            XmlElement rootNode = xmlDoc.CreateElement("testsuites");
            xmlDoc.AppendChild(rootNode);
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.InsertBefore(xmlDeclaration, rootNode);

            // Testsuites
            XmlAttribute attributeDisabled = xmlDoc.CreateAttribute("disabled");
            attributeDisabled.Value = "";
            rootNode.Attributes.Append(attributeDisabled);

            return xmlDoc.OuterXml;
        }
    }
}
