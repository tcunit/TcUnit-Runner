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

        public TcUnitResultCollector()
        { }

        /// <summary>
        /// Parses the error list from visual studio and collect the results from TcUnit
        /// </summary>
        /// <returns>Whether TcUnit results are available</returns>  
        public bool CollectResults(ErrorItems errors)
        {
            Console.WriteLine(Environment.NewLine);
            string line;
            for (int i = 1; i <= errors.Count; i++)
            {
                ErrorItem item = errors.Item(i);
                if ((item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh))
                {
                    line = item.Description;

                    if (line.Contains("Test suites:"))
                    {
                        string noTestSuites = line.Substring(line.LastIndexOf("Test suites:") + 13);
                        Console.WriteLine("No of test suites: " + noTestSuites);
                        if (!Int32.TryParse(noTestSuites, out numberOfTestSuites))
                        {
                            i = -1;
                        }
                    }
                    if (line.Contains("Tests:"))
                    {
                        string noTests = line.Substring(line.LastIndexOf("Tests:") + 7);
                        Console.WriteLine("No of tests: " + noTests);
                        if (!Int32.TryParse(noTests, out numberOfTests))
                        {
                            i = -1;
                        }
                    }
                    if (line.Contains("Successful tests:"))
                    {
                        string noSuccessfulTests = line.Substring(line.LastIndexOf("Successful tests:") + 18);
                        Console.WriteLine("No of successful tests: " + noSuccessfulTests);
                        if (!Int32.TryParse(noSuccessfulTests, out numberOfSuccessfulTests))
                        {
                            i = -1;
                        }
                    }
                    if (line.Contains("Failed tests:"))
                    {
                        string noFailedTests = line.Substring(line.LastIndexOf("Failed tests:") + 14);
                        Console.WriteLine("No of failed tests: " + noFailedTests);
                        if (!Int32.TryParse(noFailedTests, out numberOfFailedTests))
                        {
                            i = -1;
                        }
                    }
                    Console.WriteLine("Description: " + item.Description);
                }
            }

            if (AreResultsReady())
                return true;
            else
                return false;

        }

        /// <summary>
        /// Returns whether results are finished collecting
        /// </summary>
        private bool AreResultsReady()
        {
            return (numberOfTestSuites >= 0 && numberOfTests >= 0 &&
                    numberOfSuccessfulTests >= 0 && numberOfFailedTests >= 0);
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