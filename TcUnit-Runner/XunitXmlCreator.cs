using System;
using System.Collections.Generic;
using System.IO;
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

        public static void WriteXml(TcUnitTestResult testResults, string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            // <testsuites>
            XmlElement testSuitesNode = xmlDoc.CreateElement("testsuites");
            xmlDoc.AppendChild(testSuitesNode);

            // <testsuites> attributes
            XmlAttribute testSuitesAttributeFailures = xmlDoc.CreateAttribute("failures");
            testSuitesAttributeFailures.Value = testResults.GetNumberOfFailedTestCases().ToString();
            testSuitesNode.Attributes.Append(testSuitesAttributeFailures);
            XmlAttribute testSuitesAttributeTests = xmlDoc.CreateAttribute("tests");
            testSuitesAttributeTests.Value = testResults.GetNumberOfTestCases().ToString();
            testSuitesNode.Attributes.Append(testSuitesAttributeTests);
            XmlAttribute testSuitesAttributeTime = xmlDoc.CreateAttribute("time");
            testSuitesAttributeTime.Value = testResults.GetDuration().ToString(System.Globalization.CultureInfo.InvariantCulture);
            testSuitesNode.Attributes.Append(testSuitesAttributeTests);

            foreach (TcUnitTestResult.TestSuiteResult tsResult in testResults)
            {
                // <testsuite>
                XmlElement testSuiteNode = xmlDoc.CreateElement("testsuite");
                // <testsuite> attributes
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
                XmlAttribute testSuiteAttributeTime = xmlDoc.CreateAttribute("time");
                testSuiteAttributeTime.Value = tsResult.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture);
                testSuiteNode.Attributes.Append(testSuiteAttributeTime);

                // <testcase>
                foreach (TcUnitTestResult.TestCaseResult tcResult in tsResult.TestCaseResults)
                {
                    XmlElement testCaseNode = xmlDoc.CreateElement("testcase");

                    // <testcase> attributes
                    XmlAttribute testCaseAttributeName = xmlDoc.CreateAttribute("name");
                    testCaseAttributeName.Value = tcResult.TestName;
                    testCaseNode.Attributes.Append(testCaseAttributeName);
                    XmlAttribute testCaseAttributeNumberOfAsserts = xmlDoc.CreateAttribute("assertions");
                    testCaseAttributeNumberOfAsserts.Value = tcResult.NumberOfAsserts.ToString();
                    testCaseNode.Attributes.Append(testCaseAttributeNumberOfAsserts);
                    XmlAttribute testCaseAttributeTestClassName = xmlDoc.CreateAttribute("classname");
                    testCaseAttributeTestClassName.Value = tcResult.TestClassName;
                    testCaseNode.Attributes.Append(testCaseAttributeTestClassName);
                    XmlAttribute testCaseAttributeTestTime = xmlDoc.CreateAttribute("time");
                    testCaseAttributeTestTime.Value = tcResult.TestDuration.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    testCaseNode.Attributes.Append(testCaseAttributeTestTime);
                    XmlAttribute testCaseAttributeStatus = xmlDoc.CreateAttribute("status");
                    testCaseAttributeStatus.Value = tcResult.TestStatus;
                    testCaseNode.Attributes.Append(testCaseAttributeStatus);


                    if (tcResult.TestStatus.Equals("SKIP"))
                    {
                        // <skipped>
                        XmlElement testCaseSkippedNode = xmlDoc.CreateElement("skipped");

                        // Append <skipped> to <testcase>
                        testCaseNode.AppendChild(testCaseSkippedNode);
                    }
                    else if (tcResult.TestStatus.Equals("FAIL"))
                    {
                        // <failure>
                        XmlElement failureNode = xmlDoc.CreateElement("failure");

                        // <failure> attributes
                        XmlAttribute failureAttributeMessage = xmlDoc.CreateAttribute("message");
                        failureAttributeMessage.Value = tcResult.FailureMessage;
                        failureNode.Attributes.Append(failureAttributeMessage);
                        XmlAttribute failureAttributeType = xmlDoc.CreateAttribute("type");
                        failureAttributeType.Value = tcResult.AssertType;
                        failureNode.Attributes.Append(failureAttributeType);

                        // Append <failure> to <testcase>
                        testCaseNode.AppendChild(failureNode);
                    }

                    // Append <testcase> to <testesuite>
                    testSuiteNode.AppendChild(testCaseNode);
                }

                // Append <testsuite> to <testsuites>
                testSuitesNode.AppendChild(testSuiteNode);
            }

            BeautifyAndWriteToFile(xmlDoc, filePath);
        }

        private static void BeautifyAndWriteToFile(XmlDocument doc, string filePath)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document,
                OmitXmlDeclaration = false,
                CloseOutput = true,
                Indent = true,
                IndentChars = "  ",
                NewLineHandling = NewLineHandling.Replace
            };
            using (StreamWriter sw = File.CreateText(filePath) )
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                doc.WriteContentTo(writer);
                writer.Close();
            }
        }
    }
}
