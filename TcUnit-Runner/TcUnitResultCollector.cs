using EnvDTE80;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            TEST_SUITE_FINISHED_RUNNING = 0, // For example: | Test suite ID=1 'PRG_TEST.fbDiagnosticMessageFlagsParser_Test'		
            TEST_SUITE_STATISTICS, // For example: | ID=1 number of tests=4, number of failed tests=1, duration=0.1
            TEST_NAME, // For example: | Test name=WhenWarningMessageExpectWarningMessageLocalTimestampAndTwoParameters
            TEST_CLASS_NAME, // For example: | Test class name=PRG_TEST.fbDiagnosticMessageFlagsParser_Test
            TEST_DURATION, // For example: | Test duration=0.010
            TEST_STATUS_AND_NUMBER_OF_ASSERTS, // For example: | Test status=SUCCESS, number of asserts=3
            TEST_ASSERT_MESSAGE, // For example: | Test assert message=Test 'Warning message' failed at 'diagnosis type'
            TEST_ASSERT_TYPE, // For example: | Test assert type=ANY
        }

        private int numberOfTestSuites = -1;
        private int numberOfTests = -1;
        private int numberOfSuccessfulTests = -1;
        private int numberOfFailedTests = -1;
        private double duration = 0.0;
        private const string tcUnitResult_TestSuites = "| Test suites:";
        private const string tcUnitResult_Tests = "| Tests:";
        private const string tcUnitResult_SuccessfulTests = "| Successful tests:";
        private const string tcUnitResult_FailedTests = "| Failed tests:";
        private const string tcUnitResult_Duration = "| Duration:";

        public TcUnitResultCollector()
        { }

        /// <summary>
        /// Returns whether results are finished collecting
        /// </summary>
        /// <returns>Whether TcUnit results are available</returns>  
        public bool AreResultsAvailable(ErrorItems errorItems)
        {
            string line;

            // First check if we already have results
            if (AreTestResultsAvailable())
                return true;
            else
            {
                for (int i = 1; i <= errorItems.Count; i++)
                {
                    ErrorItem error = errorItems.Item(i);
                    line = error.Description;
                    if (line.Contains(tcUnitResult_TestSuites))
                    {
                        string noTestSuites = line.Substring(line.LastIndexOf(tcUnitResult_TestSuites) + tcUnitResult_TestSuites.Length + 1);
                        Int32.TryParse(noTestSuites, out numberOfTestSuites);
                    }
                    if (line.Contains(tcUnitResult_Tests))
                    {
                        string noTests = line.Substring(line.LastIndexOf(tcUnitResult_Tests) + tcUnitResult_Tests.Length + 1);
                        Int32.TryParse(noTests, out numberOfTests);
                    }
                    if (line.Contains(tcUnitResult_SuccessfulTests))
                    {
                        string noSuccessfulTests = line.Substring(line.LastIndexOf(tcUnitResult_SuccessfulTests) + tcUnitResult_SuccessfulTests.Length + 1);
                        Int32.TryParse(noSuccessfulTests, out numberOfSuccessfulTests);
                    }
                    if (line.Contains(tcUnitResult_FailedTests))
                    {
                        string noFailedTests = line.Substring(line.LastIndexOf(tcUnitResult_FailedTests) + tcUnitResult_FailedTests.Length + 1);
                        Int32.TryParse(noFailedTests, out numberOfFailedTests);
                    }
                    if (line.Contains(tcUnitResult_Duration))
                    {
                        string durationString = line.Substring(line.LastIndexOf(tcUnitResult_Duration) + tcUnitResult_Duration.Length + 1);
                        double.TryParse(durationString, out duration);
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
        public TcUnitTestResult ParseResults(IEnumerable<ErrorList.Error> errors, string unitTestTaskName)
        {

            TcUnitTestResult tcUnitTestResult = new TcUnitTestResult();

            if (!AreTestResultsAvailable())
                return null;
            else
            {
                ErrorLogEntryType expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING;

                tcUnitTestResult.AddGeneralTestResults((uint)numberOfTestSuites, (uint)numberOfTests,
                                                       (uint)numberOfSuccessfulTests, (uint)numberOfFailedTests,
                                                       duration);

                // Temporary variables
                uint currentTestIdentity = 0;
                uint currentTestCaseInTestSuiteNumber = 0;

                /* Storage variables */
                // Test suite
                string testSuiteName = "";
                uint testSuiteIdentity = 0;
                uint testSuiteNumberOfTests = 0;
                uint testSuiteNumberOfFailedTests = 0;
                double testSuiteDuration = 0.0;

                // Test case
                string testSuiteTestCaseName = "";
                string testSuiteTestCaseClassName = "";
                double testSuiteTestCaseDuration = 0.0;
                string testSuiteTestCaseStatus = "";
                string testSuiteTestCaseFailureMessage = "";
                string testSuiteTestCaseAssertType = "";
                uint testSuiteTestCaseNumberOfAsserts = 0;
                List<TcUnitTestResult.TestCaseResult> testSuiteTestCaseResults = new List<TcUnitTestResult.TestCaseResult>();

                /* Find all test suite IDs. There must be one ID for every test suite. The ID starts at 0, so
                 * if we have 5 test suites, we should expect the IDs to be 0, 1, 2, 3, 4 */
                foreach (var item in errors.Where(e => e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelLow))
                {
                    string tcUnitAdsMessage;
                    // Only do further processing if message is from TcUnit
                    if (IsTcUnitAdsMessage(item.Description, unitTestTaskName)) {
                        tcUnitAdsMessage = RemoveEverythingButTcUnitAdsMessage(item.Description, unitTestTaskName);
                        /* -------------------------------------
                        Look for test suite finished running
                       ------------------------------------- */
                        if (tcUnitAdsMessage.Contains("Test suite ID=" + currentTestIdentity + " '"))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING)
                            {
                                // Reset stored variables
                                testSuiteName = "";
                                testSuiteIdentity = 0;
                                testSuiteNumberOfTests = 0;
                                testSuiteNumberOfFailedTests = 0;
                                testSuiteDuration = 0.0;

                                testSuiteTestCaseName = "";
                                testSuiteTestCaseClassName = "";
                                testSuiteTestCaseDuration = 0.0;
                                testSuiteTestCaseStatus = "";
                                testSuiteTestCaseFailureMessage = "";
                                testSuiteTestCaseAssertType = "";
                                testSuiteTestCaseResults.Clear();

                                // Parse test suite name
                                testSuiteIdentity = currentTestIdentity;
                                testSuiteName = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf("Test suite ID=" + currentTestIdentity + " '") + currentTestIdentity.ToString().Length + 16);
                                /* If last character is ', remove it
                                 */
                                if (String.Equals(testSuiteName[testSuiteName.Length - 1].ToString(), "'", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    testSuiteName = testSuiteName.Remove(testSuiteName.Length - 1);
                                }
                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_STATISTICS;
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING.ToString());
                                return null;
                            }
                        }
                        /* -------------------------------------
                           Look for test suite statistics
                           ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("ID=" + currentTestIdentity + " number of tests=") &&
                                 tcUnitAdsMessage.Contains(", number of failed tests="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_SUITE_STATISTICS)
                            {
                                // Parse test suite results (number of tests + number of failed tests)
                                string numberOfTestsString = Utilities.GetStringBetween(tcUnitAdsMessage, "ID=" + currentTestIdentity + " number of tests=", ", number of failed tests=");
                                if (!uint.TryParse(numberOfTestsString, out testSuiteNumberOfTests))
                                {
                                    // Handle error
                                }
                                string numberOfFailedTestsString = Utilities.GetStringBetween(tcUnitAdsMessage, ", number of failed tests=", ", duration=");
                                if (!uint.TryParse(numberOfFailedTestsString, out testSuiteNumberOfFailedTests))
                                {
                                    // Handle error
                                }
                                string durationString = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf(", duration=") + 11);
                                if (!double.TryParse(durationString, out testSuiteDuration))
                                {
                                    // Handle error
                                }

                                /* Now two things can happen. Either the testsuite didn't have any tests (testSuiteNumberOfTests=0)
                                   or it had tests. If it didn't have any tests, we store the testsuite result here and go to the
                                   next test suite. If it had tests, we continue */

                                if (testSuiteNumberOfTests.Equals(0))
                                {
                                    // Store test suite & go to next test suite
                                    TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult(testSuiteName, testSuiteIdentity, testSuiteNumberOfTests, testSuiteNumberOfFailedTests, testSuiteDuration);
                                    tcUnitTestResult.AddNewTestSuiteResult(tsResult);
                                    expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING;
                                }
                                else
                                {
                                    expectedErrorLogEntryType = ErrorLogEntryType.TEST_NAME;
                                    currentTestCaseInTestSuiteNumber = 1;
                                }

                                currentTestIdentity++;
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_SUITE_STATISTICS.ToString());
                                return null;
                            }
                        }

                        /* -------------------------------------
                           Look for test name
                           ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("Test name="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_NAME && currentTestCaseInTestSuiteNumber <= testSuiteNumberOfTests)
                            {
                                // Parse test name
                                string testName = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf("Test name=") + 10);
                                testSuiteTestCaseName = testName;
                                currentTestCaseInTestSuiteNumber++;
                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_CLASS_NAME;
                            }
                            else
                            {
                                if (expectedErrorLogEntryType != ErrorLogEntryType.TEST_NAME)
                                    log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_NAME.ToString());
                                else
                                    log.Error("While parsing TcUnit results, got test case number " + currentTestCaseInTestSuiteNumber + " but expected number is " + testSuiteNumberOfTests);
                                return null;
                            }
                        }

                        /* -------------------------------------
                           Look for test class name
                           ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("Test class name="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_CLASS_NAME)
                            {
                                // Parse test class name
                                string testClassName = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf("Test class name=") + 16);
                                testSuiteTestCaseClassName = testClassName;
                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_DURATION;
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_CLASS_NAME.ToString());
                                return null;
                            }
                        }

                        /* -------------------------------------
                           Look for test duration
                           ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("Test duration="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_DURATION)
                            {
                                // Parse test class name
                                string testDuration = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf("Test duration=") + 14);
                                if (!double.TryParse(testDuration, NumberStyles.Any, CultureInfo.InvariantCulture, out testSuiteTestCaseDuration))
                                {
                                    // Handle error
                                }
                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_STATUS_AND_NUMBER_OF_ASSERTS;
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_CLASS_NAME.ToString());
                                return null;
                            }
                        }

                        /* -------------------------------------
                           Look for test status and number of asserts
                           ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("Test status=") && tcUnitAdsMessage.Contains(", number of asserts="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_STATUS_AND_NUMBER_OF_ASSERTS)
                            {
                                // Parse test status
                                string testStatus = Utilities.GetStringBetween(tcUnitAdsMessage, "Test status=", ", number of asserts=");
                                string testNumberOfAssertions = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf(", number of asserts=") + 20);

                                testSuiteTestCaseStatus = testStatus;
                                if (!uint.TryParse(testNumberOfAssertions, out testSuiteTestCaseNumberOfAsserts))
                                {
                                    // Handle error
                                }
                                /* Now two things can happen. Either the test result/status is FAIL, in which case we want to read the
                                   assertion information. If the test result/status is not FAIL, we either continue to the next test case
                                   or the next test suite */
                                if (testStatus.Equals("FAIL"))
                                    expectedErrorLogEntryType = ErrorLogEntryType.TEST_ASSERT_MESSAGE;
                                else
                                {
                                    // Store test case
                                    TcUnitTestResult.TestCaseResult tcResult = new TcUnitTestResult.TestCaseResult(testSuiteTestCaseName, testSuiteTestCaseClassName, testSuiteTestCaseStatus, "", "", testSuiteTestCaseNumberOfAsserts, testSuiteTestCaseDuration);

                                    // Add test case result to test cases results
                                    testSuiteTestCaseResults.Add(tcResult);

                                    if (currentTestCaseInTestSuiteNumber <= testSuiteNumberOfTests)
                                    { // More tests in this test suite
                                        expectedErrorLogEntryType = ErrorLogEntryType.TEST_NAME; // Goto next test case
                                    }
                                    else
                                    { // Last test case in this test suite
                                        // Create test suite result
                                        TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult(testSuiteName, testSuiteIdentity, testSuiteNumberOfTests, testSuiteNumberOfFailedTests, testSuiteDuration);

                                        // Add test case results to test suite
                                        foreach (TcUnitTestResult.TestCaseResult tcResultToBeStored in testSuiteTestCaseResults)
                                        {
                                            tsResult.TestCaseResults.Add(tcResultToBeStored);
                                        }

                                        // Add test suite to final test results
                                        tcUnitTestResult.AddNewTestSuiteResult(tsResult);
                                        expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING;
                                    }
                                }
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_STATUS_AND_NUMBER_OF_ASSERTS.ToString());
                                return null;
                            }
                        }

                        /* -------------------------------------
                          Look for test assert message
                          ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("Test assert message="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_ASSERT_MESSAGE)
                            {
                                // Parse test assert message
                                string testAssertMessage = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf("Test assert message=") + 20);
                                testSuiteTestCaseFailureMessage = testAssertMessage;

                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_ASSERT_TYPE;
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_ASSERT_MESSAGE.ToString());
                                return null;
                            }
                        }

                        /* -------------------------------------
                          Look for test assert type
                          ------------------------------------- */
                        else if (tcUnitAdsMessage.Contains("Test assert type="))
                        {
                            if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_ASSERT_TYPE
                                /* Even though we might expect a test assertion message, the test assertion might not have included one message, and thus we will
                                 * skip/not receive any TEST_ASSERTION_MESSAGE but rather instead get a TEST_ASSERT_TYPE 
                                 */
                                || expectedErrorLogEntryType == ErrorLogEntryType.TEST_ASSERT_MESSAGE)
                            {
                                // Make sure to reset the assertion message to empty string if we have not received any test assert message
                                if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_ASSERT_MESSAGE)
                                    testSuiteTestCaseFailureMessage = "";

                                // Parse test assert type
                                string testAssertType = tcUnitAdsMessage.Substring(tcUnitAdsMessage.LastIndexOf("Test assert type=") + 17);
                                testSuiteTestCaseAssertType = testAssertType;

                                // Store test case
                                TcUnitTestResult.TestCaseResult tcResult = new TcUnitTestResult.TestCaseResult(testSuiteTestCaseName, testSuiteTestCaseClassName, testSuiteTestCaseStatus, testSuiteTestCaseFailureMessage, testSuiteTestCaseAssertType, testSuiteTestCaseNumberOfAsserts, testSuiteTestCaseDuration);

                                // Add test case result to test cases results
                                testSuiteTestCaseResults.Add(tcResult);

                                if (currentTestCaseInTestSuiteNumber <= testSuiteNumberOfTests)
                                { // More tests in this test suite
                                    expectedErrorLogEntryType = ErrorLogEntryType.TEST_NAME; // Goto next test case
                                }
                                else
                                { // Last test case in this test suite
                                    // Create test suite result
                                    TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult(testSuiteName, testSuiteIdentity, testSuiteNumberOfTests, testSuiteNumberOfFailedTests, testSuiteTestCaseDuration);

                                    // Add test case results to test suite
                                    foreach (TcUnitTestResult.TestCaseResult tcResultToBeStored in testSuiteTestCaseResults)
                                    {
                                        tsResult.TestCaseResults.Add(tcResultToBeStored);
                                    }

                                    // Add test suite to final test results
                                    tcUnitTestResult.AddNewTestSuiteResult(tsResult);
                                    expectedErrorLogEntryType = ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING;
                                }
                            }
                            else
                            {
                                log.Error("While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_ASSERT_TYPE.ToString());
                                return null;
                            }
                        }
                    }
                }
                log.Info("Done collecting TC results");
                return tcUnitTestResult;
            }
        }

        /// <summary>
        /// Removes everything from the error-log other than the ADS message from TcUnit, for example it converts:
        /// Message	53	2020-04-09 07:36:01 864 ms | 'UnitTestTask' (351): | Test class name=PRG_TEST.Beckhoff_EL1008_Test
        /// to:
        /// Test class name=PRG_TEST.Beckhoff_EL1008_Test
        /// </summary>
        private string RemoveEverythingButTcUnitAdsMessage(string message, string taskName) {
            int indexOfSearchString = message.IndexOf("'" + taskName + "'") + (taskName.Length + 2);
            
            // Get index of the character '|' (after the taskname)
            string remainingString = message.Substring(indexOfSearchString);

            indexOfSearchString = remainingString.IndexOf("|") + 1;

            // Get everything after the '| ' characters (note the space as well)
            remainingString = remainingString.Substring(indexOfSearchString + 1);

            return remainingString;
        }

        /// <summary>
        /// Returns whether the message is a message that is originated from TcUnit. So for example:
        /// The following would return false:
        /// Message	20	2020-04-09 07:36:00 901 ms | 'License Server' (30): license validation status is Valid(3)
        /// While the following would return true:
        /// Message	29	2020-04-09 07:36:01 464 ms | 'UnitTestTask' (351): | Test suite ID=0 'PRG_TEST.AnalogChannelStatusConverter_Test'
        /// </summary>
        private bool IsTcUnitAdsMessage(string message, string taskName)
        {
            // Look for task name
            if (message.IndexOf("'" + taskName + "'") < 0)
            {
                return false;
            }
            else
            {
                int indexOfSearchString = message.IndexOf("'" + taskName + "'") + (taskName.Length + 2);
                // Look for the | character of the remaining part
                string remainingString = message.Substring(indexOfSearchString);
                if (remainingString.IndexOf("|") < 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
                
            }
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