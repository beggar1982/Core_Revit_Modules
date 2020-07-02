namespace ModPlus_Revit.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class RevitAddinsLocation
    {
        /// <summary>
        /// Directory of User Addins
        /// </summary>
        public static string User
        {
            get
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Autodesk", "Revit", "Addins");
                CreateDirectoryNoCatch(directory);
                if (Directory.Exists(directory))
                    return directory;

                var variable = Environment.GetEnvironmentVariable("appdata");
                if (variable != null)
                    directory = Path.Combine(variable, "Autodesk", "Revit", "Addins");
                CreateDirectoryNoCatch(directory);
                if (Directory.Exists(directory))
                    return directory;
                directory = Path.Combine("C:\\Users", GetUserName(), "AppData", "Roaming", "Autodesk", "Revit", "Addins");
                CreateDirectoryNoCatch(directory);
                if (Directory.Exists(directory))
                    return directory;

                return string.Empty;
            }
        }

        /// <summary>
        /// Directory of Machine Addins
        /// </summary>
        public static string Machine
        {
            get
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "Revit", "Addins");
                CreateDirectoryNoCatch(directory);
                if (Directory.Exists(directory))
                    return directory;
                return string.Empty;
            }
        }

        /// <summary>
        /// Get all directories (User, Machine)
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllVariants()
        {
            return new List<string> { User, Machine };
        }

        /// <summary>
        /// Get all directories for Revit version
        /// </summary>
        /// <param name="revitVersion">External Revit version</param>
        /// <returns></returns>
        public static List<string> GetAllVariants(string revitVersion)
        {
            var variants = new List<string>();
            foreach (var s in GetAllVariants())
            {
                var directory = Path.Combine(s, revitVersion);
                CreateDirectoryNoCatch(directory);
                variants.Add(directory);
            }

            return variants;
        }

        /// <summary>
        /// Get directory for Revit
        /// </summary>
        /// <param name="revitVersion">External version of Revit</param>
        /// <returns>First try to get User directory, then Machine</returns>
        public static string GetAddinFolder(string revitVersion)
        {
            if (!string.IsNullOrEmpty(User))
            {
                var directory = Path.Combine(User, revitVersion);
                CreateDirectoryNoCatch(directory);
                return directory;
            }

            if (!string.IsNullOrEmpty(Machine))
            {
                var directory = Path.Combine(Machine, revitVersion);
                CreateDirectoryNoCatch(directory);
                return directory;
            }

            return string.Empty;
        }

        private static string GetUserName()
        {
            var identityName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            if (!string.IsNullOrEmpty(identityName))
            {
                var split = identityName.Split('\\');
                if (split.Length > 1)
                    return split[1];

                return identityName;
            }

            return Environment.UserName;
        }

        private static void CreateDirectoryNoCatch(string directory)
        {
            if (Directory.Exists(directory))
                return;
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch
            {
                // ignore
            }
        }
    }
}
