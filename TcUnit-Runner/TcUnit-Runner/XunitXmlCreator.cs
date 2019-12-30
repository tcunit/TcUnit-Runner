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

        public static string GetXmlString(TcUnitTestResult testResults)
        {
            XmlDocument xmlDoc = new XmlDocument();
            // <Testsuites>
            XmlElement testSuitesNode = xmlDoc.CreateElement("testsuites");
            xmlDoc.AppendChild(testSuitesNode);
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.InsertBefore(xmlDeclaration, testSuitesNode);

            // <Testsuites attributes>
            XmlAttribute testSuitesAttributeDisabled = xmlDoc.CreateAttribute("disabled");
            testSuitesAttributeDisabled.Value = "";
            testSuitesNode.Attributes.Append(testSuitesAttributeDisabled);
            XmlAttribute testSuitesAttributeFailures = xmlDoc.CreateAttribute("failures");
            testSuitesAttributeFailures.Value = testResults.GetNumberOfFailedTestCases().ToString();
            testSuitesNode.Attributes.Append(testSuitesAttributeFailures);
            XmlAttribute testSuitesAttributeTests = xmlDoc.CreateAttribute("tests");
            testSuitesAttributeTests.Value = testResults.GetNumberOfTestCases().ToString();
            testSuitesNode.Attributes.Append(testSuitesAttributeTests);

            foreach (TcUnitTestResult.TestSuiteResult tsResult in testResults)
            {
                // <Testsuite>
                XmlElement testSuiteNode = xmlDoc.CreateElement("testsuite");
                // <Testsuite attributes>
                XmlAttribute testSuiteAttributeIdentity = xmlDoc.CreateAttribute("id");
                testSuiteAttributeIdentity.Value = tsResult.Identity.ToString();
                testSuiteNode.Attributes.Append(testSuiteAttributeIdentity);
                XmlAttribute testSuiteAttributeName = xmlDoc.CreateAttribute("name");
                testSuiteAttributeName.Value = tsResult.Name;
                testSuiteNode.Attributes.Append(testSuiteAttributeName);
                XmlAttribute testSuiteAttributeTests = xmlDoc.CreateAttribute("tests");
                testSuiteAttributeTests.Value = tsResult.NumberOfTests.ToString();
                testSuiteNode.Attributes.Append(testSuiteAttributeTests);
                XmlAttribute testSuiteAttributeFailures = xmlDoc.CreateAttribute("failures");
                testSuiteAttributeFailures.Value = tsResult.NumberOfFailedTests.ToString();
                testSuiteNode.Attributes.Append(testSuiteAttributeFailures);

                testSuitesNode.AppendChild(testSuiteNode);
            }


            return xmlDoc.OuterXml;
        }
    }
}
