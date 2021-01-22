namespace ModPlus_Revit.App
{
    /// <summary>
    /// Окно настроек ModPlus
    /// </summary>
    public partial class SettingsWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("RevitDlls", "h1");
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources, "LangApi");
        }
    }
}
