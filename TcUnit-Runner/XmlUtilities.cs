using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TcUnit.TcUnit_Runner
{
    class XmlUtilities
    {
        // Singleton constructor
        private XmlUtilities()
        { }

        /// <summary>
        /// Sets the <Disabled> and <AutoStart>-tags
        /// </summary>
        /// <param name="rtXml">The XML-string coming from the real-time task automation interface object</param>
        /// <returns>String with parameters updated</returns>
        public static string SetDisabledAndAndAutoStartOfRealTimeTaskXml(string rtXml, bool disabled, bool autostart)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(rtXml);
            XmlNode disabledNode = xmlDoc.SelectSingleNode("/TreeItem/" + "Disabled");
            if (disabledNode != null)
            {
                disabledNode.InnerText = disabled.ToString().ToLower();
            }
            else
            {
                return "";
            }

            XmlNode autoStartNode = xmlDoc.SelectSingleNode("/TreeItem/TaskDef/" + "AutoStart");
            if (autoStartNode != null)
            {
                autoStartNode.InnerText = autostart.ToString().ToLower();
            }
            else
            {
                return "";
            }
            return xmlDoc.OuterXml;
        }

        /// <summary>
        /// Gets the <ItemName> from the XML
        /// </summary>
        public static string GetItemNameFromRealTimeTaskXML(string rtXml) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(rtXml);
            XmlNode itemNameNode = xmlDoc.SelectSingleNode("/TreeItem/" + "ItemName");
            if (itemNameNode != null)
            {
                return itemNameNode.InnerText;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Check whether a TwinCAT project is pinned
        /// </summary>
        /// <param name="TwinCATProjectFilePath">The complete file path to the TwinCAT (*.tsproj)-file</param>
        /// <returns>True if pinned, otherwise false</returns>
        public static bool IsTwinCATProjectPinned(string TwinCATProjectFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(TwinCATProjectFilePath);

            XmlNode nodePinnedVersion = xmlDoc.SelectSingleNode("/TcSmProject");
            var attrPinnedVersion = nodePinnedVersion.Attributes["TcVersionFixed"];

            if (nodePinnedVersion == null || attrPinnedVersion == null)
            {
                return false;
            }
            return Convert.ToBoolean(attrPinnedVersion.InnerText);
        }
    }
}
