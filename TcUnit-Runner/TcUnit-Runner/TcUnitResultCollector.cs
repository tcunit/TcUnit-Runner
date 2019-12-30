using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class TcUnitResultCollector
    {
        private int numberOfTestSuites = -1;
        private int numberOfTests = -1;
        private int numberOfSuccessfulTests = -1;
        private int numberOfFailedTests = -1;

        private TcUnitTestResult tcUnitTestResult;

        public TcUnitResultCollector()
        {
            tcUnitTestResult = new TcUnitTestResult();
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
        /// If parse succeeds, the test results
        /// </returns>
        public TcUnitTestResult ParseResults(ErrorList errorList)
        {
            if (!AreTestResultsAvailable())
                return null;
            else
            {
                tcUnitTestResult.AddGeneralTestResults((uint) numberOfTestSuites, (uint) numberOfTests,
                                                       (uint) numberOfSuccessfulTests, (uint) numberOfFailedTests);

                uint currentTestIdentity = 0;
                string testSuiteName;
                uint testSuiteIdentity;
                uint testSuiteNumberOfTests;
                uint testSuiteNumberOfFailedTests;

                /* Find all test suite IDs. There must be one ID for every test suite. The ID starts at 0, so
                 * if we have 5 test suites, we should expect the IDs to be 0, 1, 2, 3, 4 */
                foreach (var item in errorList.Where(e => e.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelLow))
                {
                    //Console.WriteLine("A: " + item.Description);
                    // Look for test suite finished running
                    if (item.Description.Contains("with ID=" + currentTestIdentity + " finished running"))
                    {
                        // Parse test suite name
                        testSuiteName = GetStringBetween(item.Description, "Test suite '", "' with ID=" + currentTestIdentity + " finished running");
                        Console.WriteLine("TestSuiteName: " + testSuiteName);
                        // Parse test suite results (number of tests + number of failed tests)
                        currentTestIdentity++;
                    }
                }
                return tcUnitTestResult;
            }
        }

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