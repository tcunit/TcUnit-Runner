using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace TcUnit.TcUnit_Runner
{
    class Utilities
    {
        // Singleton constructor
        private Utilities()
        { }

        /// <summary>
        /// Deletes all the files and directories inside a directory before removing
        /// the target_dir.
        /// </summary>
        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        /// <summary>
        /// Sets the folder permission to full control
        /// </summary>
        public static void SetFolderPermission(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var directorySecurity = directoryInfo.GetAccessControl();
            var currentUserIdentity = WindowsIdentity.GetCurrent();
            var fileSystemRule = new FileSystemAccessRule(currentUserIdentity.Name,
                                                          FileSystemRights.FullControl,
                                                          InheritanceFlags.ObjectInherit |
                                                          InheritanceFlags.ContainerInherit,
                                                          PropagationFlags.InheritOnly,
                                                          AccessControlType.Allow);

            directorySecurity.AddAccessRule(fileSystemRule);
            directoryInfo.SetAccessControl(directorySecurity);
        }

        /// <summary>
        /// Gets the build date of the application
        /// </summary>
        public static DateTime GetBuildDate(Assembly assembly)
        {
            var location = assembly.Location;
            const int headerOffset = 60;
            const int linkerTimestampOffset = 8;
            var buffer = new byte[2048];
            Stream stream = null;

            try
            {
                stream = new FileStream(location, FileMode.Open, FileAccess.Read);
                stream.Read(buffer, 0, 2048);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            var i = BitConverter.ToInt32(buffer, headerOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, i + linkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        /// <summary>
        /// Returns the string between firstString and lastString
        /// </summary>
        public static string GetStringBetween(string str, string firstString, string lastString)
        {
            string finalString;
            int pos1 = str.IndexOf(firstString) + firstString.Length;
            int pos2 = str.IndexOf(lastString);
            finalString = str.Substring(pos1, pos2 - pos1);
            return finalString;
        }

        /// <summary>
        /// Gets a string up until stopAt
        /// </summary>
        /// <returns>Empty if nothing found</returns>
        public static string GetUntilOrEmpty(string text, string stopAt)
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Compare two version strings to see if Version >= baseVersion
        /// </summary>
        /// <returns>True if Version >= baseVersion</returns>
        public static bool CompareVersionStrings(string baseVersion, string Version)
        {
            var vBase = new Version(baseVersion);
            var vComp = new Version(Version);

            return vBase.CompareTo(vComp) <= 0;
        }
    }
}
