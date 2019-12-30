using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class TcUnitTestResult : IEnumerable<TcUnitTestResult.TestSuiteResult>, IEnumerable
    {
        /* General test results */
        protected uint _numberOfTestSuites;
        protected uint _numberOfTestCases;
        protected uint _numberOfSuccessfulTestCases;
        protected uint _numberOfFailedTestCases;

        /* Test results for each individiual test suite */
        protected List<TestSuiteResult> _testSuiteResults = new List<TestSuiteResult>();

        public enum AssertionType
        {
            Type_UNDEFINED = 0,
            Type_ANY,

            /* Primitive types */
            Type_BOOL,
            Type_BYTE,
            Type_DATE,
            Type_DATE_AND_TIME,
            Type_DINT,
            Type_DWORD,
            Type_INT,
            Type_LINT,
            Type_LREAL,
            Type_LTIME,
            Type_LWORD,
            Type_REAL,
            Type_SINT,
            Type_STRING,
            Type_TIME,
            Type_TIME_OF_DAY,
            Type_UDINT,
            Type_UINT,
            Type_ULINT,
            Type_USINT,
            Type_WORD,
    
            /* Array types */
            Type_Array2D_LREAL,
            Type_Array2D_REAL,
            Type_Array3D_LREAL,
            Type_Array3D_REAL,
            Type_Array_BOOL,
            Type_Array_BYTE,
            Type_Array_DINT,
            Type_Array_DWORD,
            Type_Array_INT,
            Type_Array_LINT,
            Type_Array_LREAL,
            Type_Array_LWORD,
            Type_Array_REAL,
            Type_Array_SINT,
            Type_Array_UDINT,
            Type_Array_UINT,
            Type_Array_ULINT,
            Type_Array_USINT,
            Type_Array_WORD
        }

        public struct TestCaseResult
        {
            public string TestName;
            public string TestClassName;
            public string FailureMessage;
            public AssertionType AssertType;
            public TestCaseResult(string testName, string testClassName, string failureMessage, AssertionType assertType)
            {
                TestName = testName;
                TestClassName = testClassName;
                FailureMessage = failureMessage;
                AssertType = assertType;
            }
        }

        public struct TestSuiteResult
        {
            public string Name;
            public uint Identity;
            public uint NumberOfTests;
            public uint NumberOfFailedTests;
            public List<TestCaseResult> TestCaseResults;

            public TestSuiteResult(string name, uint identity, uint numberOfTests, uint numberOfFailedTests)
            {
                Name = name;
                Identity = identity;
                NumberOfTests = numberOfTests;
                NumberOfFailedTests = numberOfFailedTests;
                TestCaseResults = new List<TestCaseResult>();
            }
        }

        public void AddNewTestSuiteResult(TestSuiteResult testsuiteResult)
        {
            _testSuiteResults.Add(testsuiteResult);
        }

        public void AddGeneralTestResults(uint numberOfTestSuites, uint numberOfTestCases, uint numberOfSuccessfulTestCases, uint numberOfFailedTestCases)
        {
            _numberOfTestSuites = numberOfTestSuites;
            _numberOfTestCases = numberOfTestCases;
            _numberOfSuccessfulTestCases = numberOfSuccessfulTestCases;
            _numberOfFailedTestCases = numberOfFailedTestCases;
        }

        //public struct TestSuiteResults
        //{
            /* General test results */
            //public uint NumberOfTestSuites;
            //public uint NumberOfTestCases;
            //public uint NumberOfSuccessfulTestCases;
            //public uint NumberOfFailedTestCases;

            /* Test results for each individiual test suite */
            //public List<TestSuiteResult> TestSuiteResultList;

            //public TestSuiteResults(uint numberOfTestSuites, uint numberOfTestCases, uint numberOfSuccessfulTestCases, uint numberOfFailedTestCases)
            //{
              //  NumberOfTestSuites = numberOfTestSuites;
                //NumberOfTestCases = numberOfTestCases;
                //NumberOfSuccessfulTestCases = numberOfSuccessfulTestCases;
                //NumberOfFailedTestCases = numberOfFailedTestCases;

                //TestSuiteResultList = new List<TestSuiteResult>();
            //}
        //}

        IEnumerator<TestSuiteResult> IEnumerable<TestSuiteResult>.GetEnumerator()
        {
            return _testSuiteResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _testSuiteResults.GetEnumerator();
        }
    }
}
