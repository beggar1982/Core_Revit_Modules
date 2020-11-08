namespace ModPlus_Revit
{
    /// <summary>
    /// Данные, зависящие от версии
    /// </summary>
    public class VersionData
    {
#if R2017
        /// <summary>
        /// Current Revit external version
        /// </summary>
        public const string CurrentRevitVersion = "2017";
#elif R2018
        /// <summary>
        /// Current Revit external version
        /// </summary>
        public const string CurrentRevitVersion = "2018";
#elif R2019
        /// <summary>
        /// Current Revit external version
        /// </summary>
        public const string CurrentRevitVersion = "2019";
#elif R2020
        /// <summary>
        /// Current Revit external version
        /// </summary>
        public const string CurrentRevitVersion = "2020";
#elif R2021
        /// <summary>
        /// Current Revit external version
        /// </summary>
        public const string CurrentRevitVersion = "2021";
#endif
    }
}