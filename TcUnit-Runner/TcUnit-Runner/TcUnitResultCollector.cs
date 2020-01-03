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
            TEST_SUITE_FINISHED_RUNNING = 0, // For example: | Test suite ID=1 'PRG_TEST.fbDiagnosticMessageFlagsParser_Test'		
            TEST_SUITE_STATISTICS, // For example: | ID=1 number of tests=4, number of failed tests=1		
            TEST_NAME, // For example: | Test name=WhenWarningMessageExpectWarningMessageLocalTimestampAndTwoParameters
            TEST_CLASS_NAME, // For example: | Test class name=PRG_TEST.fbDiagnosticMessageFlagsParser_Test
            TEST_STATUS, // For example: | Test status=SUCCESS
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
            else
            {
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

                tcUnitTestResult.AddGeneralTestResults((uint)numberOfTestSuites, (uint)numberOfTests,
                                                       (uint)numberOfSuccessfulTests, (uint)numberOfFailedTests);

                // Temporary variables
                uint currentTestIdentity = 0;
                uint currentTestCaseInTestSuiteNumber = 0;

                /* Storage variables */
                // Test suite
                string testSuiteName = "";
                uint testSuiteIdentity = 0;
                uint testSuiteNumberOfTests = 0;
                uint testSuiteNumberOfFailedTests = 0;

                // Test case
                string testSuiteTestCaseName = "";
                string testSuiteTestCaseClassName = "";
                string testSuiteTestCaseStatus = "";
                string testSuiteTestCaseFailureMessage = "";
                string testSuiteTestCaseAssertType = "";
                List<TcUnitTestResult.TestCaseResult> testSuiteTestCaseResults = new List<TcUnitTestResult.TestCaseResult>();

                /* Find all test suite IDs. There must be one ID for every test suite. The ID starts at 0, so
                 * if we have 5 test suites, we should expect the IDs to be 0, 1, 2, 3, 4 */
                foreach (var item in errorList.Where(e => e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelLow))
                {

                    /* -------------------------------------
                        Look for test suite finished running
                       ------------------------------------- */
                    if (item.Description.Contains("| Test suite ID="+currentTestIdentity +" '"))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING)
                        {
                            // Reset stored variables
                            testSuiteName = "";
                            testSuiteIdentity = 0;
                            testSuiteNumberOfTests = 0;
                            testSuiteNumberOfFailedTests = 0;

                            testSuiteTestCaseName = "";
                            testSuiteTestCaseClassName = "";
                            testSuiteTestCaseStatus = "";
                            testSuiteTestCaseFailureMessage = "";
                            testSuiteTestCaseAssertType = "";
                            testSuiteTestCaseResults.Clear();

                            // Parse test suite name
                            testSuiteIdentity = currentTestIdentity;
                            testSuiteName = item.Description.Substring(item.Description.LastIndexOf("| Test suite ID=" +currentTestIdentity +" '") +currentTestIdentity.ToString().Length + 18);
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
                            log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_SUITE_FINISHED_RUNNING.ToString());
                            return null;
                        }
                    }
                    /* -------------------------------------
                       Look for test suite statistics
                       ------------------------------------- */
                    else if (item.Description.Contains("| ID=" + currentTestIdentity + " number of tests=") &&
                             item.Description.Contains(", number of failed tests="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_SUITE_STATISTICS)
                        {
                            // Parse test suite results (number of tests + number of failed tests)
                            string numberOfTestsString = GetStringBetween(item.Description, "ID=" + currentTestIdentity + " number of tests=", ", number of failed tests=");
                            if (!uint.TryParse(numberOfTestsString, out testSuiteNumberOfTests))
                            {
                                // Handle error
                            }
                            string numberOfFailedTestsString = item.Description.Substring(item.Description.LastIndexOf(", number of failed tests=") + 25);
                            if (!uint.TryParse(numberOfFailedTestsString, out testSuiteNumberOfFailedTests))
                            {
                                // Handle error
                            }

                            /* Now two things can happen. Either the testsuite didn't have any tests (testSuiteNumberOfTests=0)
                               or it had tests. If it didn't have any tests, we store the testsuite result here and go to the
                               next test suite. If it had tests, we continue */

                            if (testSuiteNumberOfTests.Equals(0))
                            {
                                // Store test suite & go to next test suite
                                TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult(testSuiteName, testSuiteIdentity, testSuiteNumberOfTests, testSuiteNumberOfFailedTests);
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
                            log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_SUITE_STATISTICS.ToString());
                            return null;
                        }
                    }

                    /* -------------------------------------
                       Look for test name
                       ------------------------------------- */
                    else if (item.Description.Contains("| Test name="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_NAME && currentTestCaseInTestSuiteNumber <= testSuiteNumberOfTests)
                        {
                            // Parse test name
                            string testName = item.Description.Substring(item.Description.LastIndexOf("| Test name=") + 12);
                            testSuiteTestCaseName = testName;
                            currentTestCaseInTestSuiteNumber++;
                            expectedErrorLogEntryType = ErrorLogEntryType.TEST_CLASS_NAME;
                        }
                        else
                        {
                            if (expectedErrorLogEntryType != ErrorLogEntryType.TEST_NAME)
                                log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_NAME.ToString());
                            else
                                log.Error("ERROR: While parsing TcUnit results, got test case number " + currentTestCaseInTestSuiteNumber + " but expected amount is " + testSuiteNumberOfTests);
                            return null;
                        }
                    }

                    /* -------------------------------------
                       Look for test class name
                       ------------------------------------- */
                    else if (item.Description.Contains("| Test class name="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_CLASS_NAME)
                        {
                            // Parse test class name
                            string testClassName = item.Description.Substring(item.Description.LastIndexOf("| Test class name=") + 18);
                            testSuiteTestCaseClassName = testClassName;
                            expectedErrorLogEntryType = ErrorLogEntryType.TEST_STATUS;
                        }
                        else
                        {
                            log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_CLASS_NAME.ToString());
                            return null;
                        }
                    }

                    /* -------------------------------------
                       Look for test status
                       ------------------------------------- */
                    else if (item.Description.Contains("| Test status="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_STATUS)
                        {
                            // Parse test status
                            string testStatus = item.Description.Substring(item.Description.LastIndexOf("| Test status=") + 14);
                            testSuiteTestCaseStatus = testStatus;
                            /* Now two things can happen. Either the test result/status is FAIL, in which case we want to read the
                               assertion information. If the test result/status is not FAIL, we either continue to the next test case
                               or the next test suite */
                            if (testStatus.Equals("FAIL"))
                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_ASSERT_MESSAGE;
                            else {
                                // Store test case
                                TcUnitTestResult.TestCaseResult tcResult = new TcUnitTestResult.TestCaseResult(testSuiteTestCaseName, testSuiteTestCaseClassName, testSuiteTestCaseStatus, "", "");

                                // Add test case result to test cases results
                                testSuiteTestCaseResults.Add(tcResult);

                                if (currentTestCaseInTestSuiteNumber <= testSuiteNumberOfTests) { // More tests in this test suite
                                    expectedErrorLogEntryType = ErrorLogEntryType.TEST_NAME; // Goto next test case
                                }
                                else { // Last test case in this test suite
                                    // Create test suite result
                                    TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult(testSuiteName, testSuiteIdentity, testSuiteNumberOfTests, testSuiteNumberOfFailedTests);
                                    
                                    // Add test case results to test suite
                                    foreach (TcUnitTestResult.TestCaseResult tcResultToBeStored in testSuiteTestCaseResults) {
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
                            log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_STATUS.ToString());
                            return null;
                        }
                    }

                    /* -------------------------------------
                      Look for test assert message
                      ------------------------------------- */
                    else if (item.Description.Contains("| Test assert message="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_ASSERT_MESSAGE)
                        {
                            // Parse test assert message
                            string testAssertMessage = item.Description.Substring(item.Description.LastIndexOf("| Test assert message=") + 22);
                            testSuiteTestCaseFailureMessage = testAssertMessage;

                            expectedErrorLogEntryType = ErrorLogEntryType.TEST_ASSERT_TYPE;
                        }
                        else
                        {
                            log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_ASSERT_MESSAGE.ToString());
                            return null;
                        }
                    }

                    /* -------------------------------------
                      Look for test assert type
                      ------------------------------------- */
                    else if (item.Description.Contains("| Test assert type="))
                    {
                        if (expectedErrorLogEntryType == ErrorLogEntryType.TEST_ASSERT_TYPE)
                        {
                            // Parse test assert type
                            string testAssertType = item.Description.Substring(item.Description.LastIndexOf("| Test assert type=") + 19);
                            testSuiteTestCaseAssertType = testAssertType;

                            // Store test case
                            TcUnitTestResult.TestCaseResult tcResult = new TcUnitTestResult.TestCaseResult(testSuiteTestCaseName, testSuiteTestCaseClassName, testSuiteTestCaseStatus, testSuiteTestCaseFailureMessage, testSuiteTestCaseAssertType);
                            
                            // Add test case result to test cases results
                            testSuiteTestCaseResults.Add(tcResult);

                            if (currentTestCaseInTestSuiteNumber <= testSuiteNumberOfTests)
                            { // More tests in this test suite
                                expectedErrorLogEntryType = ErrorLogEntryType.TEST_NAME; // Goto next test case
                            }
                            else
                            { // Last test case in this test suite
                                // Create test suite result
                                TcUnitTestResult.TestSuiteResult tsResult = new TcUnitTestResult.TestSuiteResult(testSuiteName, testSuiteIdentity, testSuiteNumberOfTests, testSuiteNumberOfFailedTests);

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
                            log.Error("ERROR: While parsing TcUnit results, expected " + expectedErrorLogEntryType.ToString() + " but got " + ErrorLogEntryType.TEST_ASSERT_TYPE.ToString());
                            return null;
                        }
                    }
                }
                log.Info("Done collecting TC results");
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