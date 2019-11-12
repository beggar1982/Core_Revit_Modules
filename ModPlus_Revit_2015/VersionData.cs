namespace ModPlus_Revit
{
    /// <summary>
    /// Данные, зависящие от версии
    /// </summary>
    public class MpVersionData
    {
#if R2015
        public const string CurrentRevitVersion = "2015";
#elif R2016
        public const string CurrentRevitVersion = "2016";
#elif R2017
        public const string CurrentRevitVersion = "2017";
#elif R2018
        public const string CurrentRevitVersion = "2018";
#elif R2019
        public const string CurrentRevitVersion = "2019";
#elif R2020
        public const string CurrentRevitVersion = "2020";
#endif
    }
}
