using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using TcUnit.TcUnit_Runner.PluginManager;
using TcUnit.TcUnit_Runner.PluginManager.Options;

namespace TcUnit_Runner_PluginManager_Test
{
    [TestClass]
    public class PluginOptionsTests
    {

        private static ILog log = LogManager.GetLogger("TcUnit-Runner-PluginManager-Test");
        [TestMethod]
        public void ShouldSuccessfullyLoadOptionsFromFile()
        {
            PluginLoader sut = new PluginLoader(log);
            sut.LoadOptions("test-files/.TcUnitRunner");
            Assert.AreEqual(sut.Options.Options.ChildNodes.Count, 2);
        }

        [TestMethod]
        public void ShouldReturnNodeWithNameTestPlugin()
        {
            PluginLoader sut = new PluginLoader(log);
            sut.LoadOptions("test-files/.TcUnitRunner");
            var actual = sut.Options.GetPluginNodeByAttributeName("TestPlugin");
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void ShouldReturnNodeValueWithNameTestNode()
        {
            PluginLoader sut = new PluginLoader(log);
            sut.LoadOptions("test-files/.TcUnitRunner");
            var node = sut.Options.GetPluginNodeByAttributeName("TestPlugin");
            var actual = node.GetPluginNodeValueByName("TestNode");
            Assert.IsNotNull(actual);
            Assert.AreEqual("test-value", actual);
        }

        [TestMethod]
        public void ShouldReturnAttributeValuePluginsDir()
        {
            PluginLoader sut = new PluginLoader(log);
            sut.LoadOptions("test-files/.TcUnitRunner");
            var actual = sut.Options.GetPluginsDirectory();
            Assert.IsNotNull(actual);
            Assert.AreEqual("./plugins", actual);
        }
    }
}
