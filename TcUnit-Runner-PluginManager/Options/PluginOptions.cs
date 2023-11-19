using log4net;
using System;
using System.IO;
using System.Xml;

namespace TcUnit.TcUnit_Runner.PluginManager.Options
{
    public class PluginOptions
    {

        private XmlDocument _options;
        public XmlDocument Options { get { return _options; } }

        private PluginOptions(XmlDocument xmlDoc)
        {
            _options = xmlDoc;
        }

        /// <summary>
        /// Load all options for plugins from a XML file.
        /// </summary>
        /// <param name="xmlOptionsFilePath"></param>
        /// <returns></returns>
        public static PluginOptions LoadPluginOptions(ILog log, string xmlOptionsFilePath)
        {
            try
            {
                var fi = new FileInfo(Path.GetFullPath(xmlOptionsFilePath));

                if (!fi.Exists)
                {
                    log.Warn(string.Format("Plugin configuration file doesn't exist: {0}", fi.FullName));
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fi.FullName);
                return new PluginOptions(xmlDoc);
            }
             catch (Exception ex)
            {
                log.Error("Error loading plugin options", ex);
            }

            return new PluginOptions(new XmlDocument());
        }
    }
}
