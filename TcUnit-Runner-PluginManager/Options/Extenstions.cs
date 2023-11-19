using System.Xml;
using System.Xml.Linq;

namespace TcUnit.TcUnit_Runner.PluginManager.Options
{
    public static class Extensions
    {
        public static XmlNode GetPluginNodeByAttributeName(this PluginOptions pluginOptions, string name)
        {
            return pluginOptions.Options.SelectSingleNode(string.Format("TcUnitRunner/Plugins/Plugin[@name='{0}']", name));
        }
        public static string GetPluginNodeValueByName(this XmlNode node, string name)
        {
            var foundNode = node.SelectSingleNode(string.Format("{0}", name));
            return foundNode != null ? foundNode.InnerText : string.Empty;
        }
        public static string GetPluginsDirectory(this PluginOptions pluginOptions)
{
          var node = pluginOptions.Options.SelectSingleNode("TcUnitRunner/Plugins/@directory");
            return node != null ? node.Value : string.Empty;
        }
    }
}
