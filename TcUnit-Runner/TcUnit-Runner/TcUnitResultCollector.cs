using EnvDTE80;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class TcUnitResultCollector
    {
        ILog log = LogManager.GetLogger("TcUnit-Runner");

        private enum ErrorLogEntryType
        {
            TEST_SUITE_FINISHED_RUNNING = 0, // For example: | Test suite 'PRG_TEST.fbDiagnosticMessageFlagsParser_Test' with ID=1 finished running
            TEST_SUITE_STATISTICS, // For example: | ID=1 number of tests=4, number of failed tests=1		
            TEST_NAME, // For example: | Test name=WhenWarningMessageExpectWarningMessageLocalTimestampAndTwoParameters
            TEST_CLASS_NAME, // For example: | Test class name=PRG_TEST.fbDiagnosticMessageFlagsParser_Test
            TEST_ASSERT_MESSAGE, // For example: | Test assert message=Test 'Warning message' failed at 'diagnosis type'
            TEST_ASSERT_TYPE, // For example: | Test assert type=ANY
        }

        private int numberOfTestSuites = -1;
        private int numberOfTests = -1;
        private int numberOfSuccessfulTests = -1;
        private int numberOfFailedTests = -1;

        

        public TcUnitResultCollector()
        {
           
        }

        /// <summary>
        /// Returns whether results are finished collecting
        /// </summary>
        /// <returns>Whether TcUnit results are available</returns>  
        public bool AreResultsAvailable(IEnumerable<ErrorList.Error> errorList)
        {
            string line;

            // First check if we already have results
            if (AreTestResultsAvailable())
                return true;
            else { 
                foreach (var errorItem in errorList.Where(e => e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh))
                {
                    line = errorItem.Description;
                    if (line.Contains("| Test suites:"))
                    {
                        string noTestSuites = line.Substring(line.LastIndexOf("| Test suites:") + 15);
                        Int32.TryParse(noTestSuites, out numberOfTestSuites);
                    }
                    if (line.Contains("| Tests:"))
                    {
                        string noTests = line.Substring(line.LastIndexOf("| Tests:") + 9);
                        Int32.TryParse(noTests, out numberOfTests);
                    }
                    if (line.Contains("| Successful tests:"))
                    {
                        string noSuccessfulTests = line.Substring(line.LastIndexOf("| Successful tests:") + 20);
                        Int32.TryParse(noSuccessfulTests, out numberOfSuccessfulTests);
                    }
                    if (line.Contains("| Failed tests:"))
                    {
                        string noFailedTests = line.Substring(line.LastIndexOf("| Failed tests:") + 16);
                        Int32.TryParse(noFailedTests, out numberOfFailedTests);
                    }
                }
                return AreTestResultsAvailable();
                }
        }

        /// <summary>
        /// Parses the error list from visual studio and collect the results from TcUnit
        /// </summary>
        /// <returns>
        /// null if parse failed or results are not ready
        /// If parse succeeds, the test results are returned
        /// </returns>
        public TcUnitTestResult ParseResults(ErrorList errorList)
        {
            TcUnitTestResult tcUnitTestResult = new TcUnitTestResult();

            if (!AreTestResultsAvailable())
                return null;
            else
            {
                ErrorLogEntryType expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING;

                tcUnitTestResult.AddGeneralTestResults((uint) numberOfTestSuites, (uint) numberOfTests,
                                                       (uint) numberOfSuccessfulTests, (uint) numberOfFailedTests);

                // Temporary variables
                uint currentTestIdentity = 0;

                // Storage variables
                string testSuiteName = "";
                uint testSuiteIdentity = 0;
                uint testSuiteNumberOfTests = 0;
                uint testSuiteNumberOfFailedTests = 0;

                

                /* Find all test suite IDs. There must be one ID for every test suite. The ID starts at 0, so
                 * if we have 5 test suites, we should expect the IDs to be 0, 1, 2, 3, 4 */
                foreach (var item in errorList.Where(e => e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelLow))
                {

                    // Look for test suite finished running
                    if (item.Description.Contains("| Test suite") && item.Description.Contains("with ID=" + currentTestIdentity + " finished running"))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING) {
                            // Parse test suite name
                            testSuiteIdentity = currentTestIdentity;
                            testSuiteName = GetStringBetween(item.Description, "Test suite '", "' with ID=" + currentTestIdentity + " finished running");
                            Console.WriteLine("TestSuiteId: " + testSuiteIdentity);
                            Console.WriteLine("TestSuiteName: " + testSuiteName);
                            expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_STATISTICS;
                        } else
                        {
                            log.Error("Expected " + ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING.ToString() + " but got " + expectedErrorLogEntryType.ToString());
                            return null;
                        }
                    }

                    // Look for test suite statistics
                    else if (item.Description.Contains("| ID=" + currentTestIdentity + " number of tests=") &&
                             item.Description.Contains(", number of failed tests="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_SUITE_STATISTICS)
                        {
                            // Parse test suite results (number of tests + number of failed tests)
                            string numberOfTests = GetStringBetween(item.Description, "ID="+currentTestIdentity +" number of tests=",", number of failed tests=");
                            if (!uint.TryParse(numberOfTests, out testSuiteNumberOfTests)) {
                                // Handle error
                            }
                            Console.WriteLine("NumberOfTests: " + testSuiteNumberOfTests);
                            string numberOfFailedTests = item.Description.Substring(item.Description.LastIndexOf(", number of failed tests=") + 25);
                            if (!uint.TryParse(numberOfFailedTests, out testSuiteNumberOfFailedTests))
                            {
                                // Handle error
                            }
                            Console.WriteLine("NumberOfFailedTests: " + testSuiteNumberOfFailedTests);
                            Console.WriteLine("");

                            // Store test suite & go to next test suite
                            TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult();
                            tsResult.Name = testSuiteName;
                            tsResult.Identity = testSuiteIdentity;
                            tsResult.NumberOfTests = testSuiteNumberOfTests;
                            tsResult.NumberOfFailedTests = testSuiteNumberOfFailedTests;
                            tcUnitTestResult.AddNewTestSuiteResult(tsResult);

                            expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING;
                            currentTestIdentity++;
                        }
                        else
                        {
                            log.Error("Expected " + ErrorLogEntryType.TEST_SUITE_STATISTICS.ToString() + " but got " + expectedErrorLogEntryType.ToString());
                            return null;
                        }


                    }
                }
                return tcUnitTestResult;
            }
        }

        /// <summary>
        /// Returns the string between firstString and lastString
        /// </summary>
        private string GetStringBetween(string str, string firstString, string lastString)
        {
            string finalString;
            int pos1 = str.IndexOf(firstString) + firstString.Length;
            int pos2 = str.IndexOf(lastString);
            finalString = str.Substring(pos1, pos2 - pos1);
            return finalString;
        }

        /// <summary>
        /// Returns whether test results are available
        /// </summary>
        private bool AreTestResultsAvailable()
        {
            return numberOfTestSuites >= 0 && numberOfTests >= 0 &&
                   numberOfSuccessfulTests >= 0 && numberOfFailedTests >= 0;
        }


        public int GetNumberOfTestSuites()
        {
            return numberOfTestSuites;
        }

        public int GetNumberOfTests()
        {
            return numberOfTests;
        }

        public int GetNumberOfSuccessfulTests()
        {
            return numberOfSuccessfulTests;
        }

        public int GetNumberOfFailedTests()
        {
            return numberOfFailedTests;
        }

    }
}