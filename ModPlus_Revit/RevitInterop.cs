namespace ModPlus_Revit
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    /// <summary>
    /// Статические ссылки на основные объекты приложения Revit
    /// </summary>
    public static class RevitInterop
    {
        /// <summary>
        /// <see cref="UIApplication"/>
        /// </summary>
        public static UIApplication UiApplication { get; private set; }

        /// <summary>
        /// <see cref="UIDocument"/>
        /// </summary>
        public static UIDocument UiDocument => UiApplication.ActiveUIDocument;

        /// <summary>
        /// <see cref="Document"/>
        /// </summary>
        public static Document Document => UiApplication.ActiveUIDocument.Document;

        /// <summary>
        /// Universal external event <see cref="RevitEvent"/>
        /// </summary>
        public static RevitEvent RevitEvent { get; private set; }
        
        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="uiApplication"><see cref="UIApplication"/></param>
        internal static void Init(UIApplication uiApplication)
        {
            UiApplication = uiApplication;
            RevitEvent = new RevitEvent();
        }
    }
}
