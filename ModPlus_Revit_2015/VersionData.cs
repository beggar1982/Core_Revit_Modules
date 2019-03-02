namespace ModPlus_Revit
{
    // Данные, зависящие от версии
    public class MpVersionData
    {
#if R2015
        public const string CurRevitVers = "2015";
#elif R2016
        public const string CurRevitVers = "2016";
#elif R2017
        public const string CurRevitVers = "2017";
#elif R2018
        public const string CurRevitVers = "2018";
#elif R2019
        public const string CurRevitVers = "2019";
#elif R2020
        public const string CurRevitVers = "2020";
#endif
    }
}
