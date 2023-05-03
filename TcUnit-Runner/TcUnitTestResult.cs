using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TcUnit.TcUnit_Runner
{
    class TcUnitTestResult : IEnumerable<TcUnitTestResult.TestSuiteResult>, IEnumerable
    {
        /* General test results */
        protected uint _numberOfTestSuites;
        protected uint _numberOfTestCases;
        protected uint _numberOfSuccessfulTestCases;
        protected uint _numberOfFailedTestCases;
        protected double _duration;

        /* Test results for each individiual test suite */
        protected List<TestSuiteResult> _testSuiteResults = new List<TestSuiteResult>();

        public enum AssertionType
        {
            UNDEFINED = 0,
            ANY,

            /* Primitive types */
            BOOL,
            BYTE,
            DATE,
            DATE_AND_TIME,
            DINT,
            DWORD,
            INT,
            LINT,
            LREAL,
            LTIME,
            LWORD,
            REAL,
            SINT,
            STRING,
            TIME,
            TIME_OF_DAY,
            UDINT,
            UINT,
            ULINT,
            USINT,
            WORD,

            /* Array types */
            Array2D_LREAL,
            Array2D_REAL,
            Array3D_LREAL,
            Array3D_REAL,
            Array_BOOL,
            Array_BYTE,
            Array_DINT,
            Array_DWORD,
            Array_INT,
            Array_LINT,
            Array_LREAL,
            Array_LWORD,
            Array_REAL,
            Array_SINT,
            Array_UDINT,
            Array_UINT,
            Array_ULINT,
            Array_USINT,
            Array_WORD
        }

        public struct TestCaseResult
        {
            public string TestName;
            public string TestClassName;
            public string TestStatus;
            public string FailureMessage;
            public string AssertType;
            public uint NumberOfAsserts;
            public double TestDuration;
            public bool WasSuccessful;
            public TestCaseResult(string testName, string testClassName, string testStatus, string failureMessage, string assertType, uint numberOfAsserts, double duration)
            {
                TestName = testName;
                TestClassName = testClassName;
                TestDuration = duration;
                TestStatus = testStatus;
                FailureMessage = failureMessage;
                AssertType = assertType;
                NumberOfAsserts = numberOfAsserts;
                WasSuccessful = !string.IsNullOrEmpty(testStatus) && string.Equals(testStatus, "PASS", System.StringComparison.OrdinalIgnoreCase);
            }
        }

        public struct TestSuiteResult
        {
            public string Name;
            public uint Identity;
            public uint NumberOfTests;
            public uint NumberOfFailedTests;
            public double Duration;
            public List<TestCaseResult> TestCaseResults;

            public TestSuiteResult(string name, uint identity, uint numberOfTests, uint numberOfFailedTests, double duration)
            {
                Name = name;
                Identity = identity;
                NumberOfTests = numberOfTests;
                NumberOfFailedTests = numberOfFailedTests;
                Duration = duration;
                TestCaseResults = new List<TestCaseResult>();
            }
        }

        public void AddNewTestSuiteResult(TestSuiteResult testsuiteResult)
        {
            _testSuiteResults.Add(testsuiteResult);
        }

        public void AddGeneralTestResults(uint numberOfTestSuites, uint numberOfTestCases, uint numberOfSuccessfulTestCases, uint numberOfFailedTestCases, double duration)
        {
            _numberOfTestSuites = numberOfTestSuites;
            _numberOfTestCases = numberOfTestCases;
            _numberOfSuccessfulTestCases = numberOfSuccessfulTestCases;
            _numberOfFailedTestCases = numberOfFailedTestCases;
            _duration = duration;
        }

        IEnumerator<TestSuiteResult> IEnumerable<TestSuiteResult>.GetEnumerator()
        {
            return _testSuiteResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _testSuiteResults.GetEnumerator();
        }

        public uint GetNumberOfTestSuites()
        {
            return _numberOfTestSuites;
        }

        public uint GetNumberOfTestCases()
        {
            return _numberOfTestCases;
        }

        public uint GetNumberOfSuccessfulTestCases()
        {
            return _numberOfSuccessfulTestCases;
        }

        public uint GetNumberOfFailedTestCases()
        {
            return _numberOfFailedTestCases;
        }
        public double GetDuration()
        {
            return _duration;
        }

        public bool GetAllTestsPassed()
        {
            return GetNumberOfTestSuites() > 0 &&
                GetNumberOfTestCases() > 0 &&
                GetNumberOfFailedTestCases() == 0;
        }

        public string PrintTestResults()
        {
            var builder = new StringBuilder((int)(_numberOfTestSuites * _numberOfTestCases * 100));
            builder.Append("\r\n");
            builder.Append(string.Format("Test results for {0} test suite(s) with total {1} test(s):\r\n", _numberOfTestSuites, _numberOfTestCases));
            foreach (var suite in _testSuiteResults)
            {
                builder.Append(string.Format("ID={0}, Name=\"{1}\": Tests={2}, Failures={3}.\r\n", suite.Identity, suite.Name, suite.NumberOfTests, suite.NumberOfFailedTests));

                foreach (var testCaseResult in suite.TestCaseResults)
                {
                    builder.Append(string.Format("\tTest case \"{0}\", Assertions={1}, ClassName=\"{2}\", Status=\"{3}\"", testCaseResult.TestName, testCaseResult.NumberOfAsserts, testCaseResult.TestClassName, testCaseResult.TestStatus));
                    builder.Append(testCaseResult.WasSuccessful ? ".\r\n" :
                        string.Format(", Message=\"{0}\", Type=\"{1}\".\r\n", testCaseResult.FailureMessage, testCaseResult.AssertType));
                }
            }

            return builder.ToString();
        }
    }
}
