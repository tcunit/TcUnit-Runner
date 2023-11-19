using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TcUnit.TcUnit_Runner;

namespace TcUnit_Runner_Test
{
    [TestClass]
    public class TcFileUtilitiesTests
    {
        [TestMethod]
        public void ShouldReturnPathToTsprojFromSlnSuccessfully()
        {
            var sln = @".\test-data\Solution.sln";

            var actual = TcFileUtilities.FindTwinCATProjectFile(sln);

            Assert.AreEqual(@".\test-data\TwinCAT Project.tsproj", actual);
        }
    }
}
