namespace ModPlus_Revit.App
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using ModPlusAPI;
    using ModPlusAPI.LicenseServer;
    using ModPlusAPI.Windows;
    using ModPlusStyle.Controls.Dialogs;

    public partial class MpMainSettings
    {
        private const string LangItem = "RevitDlls";
        private Language.LangItem _curLangItem;

        public MpMainSettings()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");
            FillAndSetLanguages();
            SetLanguageValues();
            FillThemesAndColors();
            LoadSettingsFromConfigFileAndRegistry();
            Closed += MpMainSettings_OnClosed;

            // license server
            if (ClientStarter.IsClientWorking())
            {
                BtStopConnectionToLicenseServer.IsEnabled = true;
                BtRestoreConnectionToLicenseServer.IsEnabled = false;
                TbLocalLicenseServerIpAddress.IsEnabled = false;
                TbLocalLicenseServerPort.IsEnabled = false;
            }
            else
            {
                BtStopConnectionToLicenseServer.IsEnabled = false;
                BtRestoreConnectionToLicenseServer.IsEnabled = true;
                TbLocalLicenseServerIpAddress.IsEnabled = true;
                TbLocalLicenseServerPort.IsEnabled = true;
            }
        }

        private void FillAndSetLanguages()
        {
            var languagesByFiles = ModPlusAPI.Language.GetLanguagesByFiles();
            CbLanguages.ItemsSource = languagesByFiles;
            CbLanguages.SelectedItem = languagesByFiles.FirstOrDefault(li => li.Name == ModPlusAPI.Language.CurrentLanguageName);
            _curLangItem = (Language.LangItem)CbLanguages.SelectedItem;
        }

        private void SetLanguageValues()
        {
            // Так как элементы окна по Серверу лицензий ссылаются на узел ModPlusAPI
            // присваиваю им значения в коде, после установки языка
            var li = "ModPlusAPI";
            GroupBoxLicenseServer.Header = ModPlusAPI.Language.GetItem(li, "h16");
            TbLocalLicenseServerIpAddressHeader.Text = ModPlusAPI.Language.GetItem(li, "h17");
            TbLocalLicenseServerPortHeader.Text = ModPlusAPI.Language.GetItem(li, "h18");
            BtCheckLocalLicenseServerConnection.Content = ModPlusAPI.Language.GetItem(li, "h19");
            BtStopConnectionToLicenseServer.Content = ModPlusAPI.Language.GetItem(li, "h23");
            BtRestoreConnectionToLicenseServer.Content = ModPlusAPI.Language.GetItem(li, "h24");
            ChkDisableConnectionWithLicenseServer.Content = ModPlusAPI.Language.GetItem(li, "h25");
        }

        private void FillThemesAndColors()
        {
            MiTheme.ItemsSource = ModPlusStyle.ThemeManager.Themes;
            var pluginStyle = ModPlusStyle.ThemeManager.Themes.First();
            var savedPluginStyleName = Regestry.GetValue("PluginStyle");
            if (!string.IsNullOrEmpty(savedPluginStyleName))
            {
                var theme = ModPlusStyle.ThemeManager.Themes.Single(t => t.Name == savedPluginStyleName);
                if (theme != null)
                    pluginStyle = theme;
            }

            MiTheme.SelectedItem = pluginStyle;
        }
        
        // Загрузка данных из файла конфигурации
        // которые требуется отобразить в окне
        private void LoadSettingsFromConfigFileAndRegistry()
        {
            // Separator
            var separator = Regestry.GetValue("Separator");
            CbSeparatorSettings.SelectedIndex = string.IsNullOrEmpty(separator) ? 0 : int.Parse(separator);
            ChkDisableConnectionWithLicenseServer.IsChecked =
                bool.TryParse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "DisableConnectionWithLicenseServerInRevit"), out var b) && b; // false
            TbLocalLicenseServerIpAddress.Text = Regestry.GetValue("LocalLicenseServerIpAddress");
            TbLocalLicenseServerPort.Value = int.TryParse(Regestry.GetValue("LocalLicenseServerPort"), out var i) ? i : 0;
        }
        
        // Выбор разделителя целой и дробной части для чисел
        private void CbSeparatorSettings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Regestry.SetValue("Separator", ((ComboBox)sender).SelectedIndex.ToString(CultureInfo.InvariantCulture));
        }
        // Выбор темы
        private void MiTheme_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = (ModPlusStyle.Theme)e.AddedItems[0];
            Regestry.SetValue("PluginStyle", theme.Name);
            ModPlusStyle.ThemeManager.ChangeTheme(this, theme);
        }
        
        private void MpMainSettings_OnClosed(object sender, EventArgs e)
        {
            try
            {
                // Так как эти значения хранятся в переменных, то их нужно перезаписать
                Regestry.SetValue("Separator", CbSeparatorSettings.SelectedIndex.ToString(CultureInfo.InvariantCulture));
                
                // License server
                UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "DisableConnectionWithLicenseServerInRevit",
                    // ReSharper disable once PossibleInvalidOperationException
                    ChkDisableConnectionWithLicenseServer.IsChecked.Value.ToString(), true);
                Regestry.SetValue("LocalLicenseServerIpAddress", TbLocalLicenseServerIpAddress.Text);
                Regestry.SetValue("LocalLicenseServerPort", TbLocalLicenseServerPort.Value.ToString());

                if (_restartClientOnClose)
                {
                    // reload server
                    ClientStarter.StopConnection();
                    ClientStarter.StartConnection(ProductLicenseType.Revit);
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }

        }
        
        private async void BtCheckLocalLicenseServerConnection_OnClick(object sender, RoutedEventArgs e)
        {
            // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
            await this.ShowMessageAsync(
                ClientStarter.IsLicenseServerAvailable()
                    ? ModPlusAPI.Language.GetItem("ModPlusAPI", "h21")
                    : ModPlusAPI.Language.GetItem("ModPlusAPI", "h20"),
                ModPlusAPI.Language.GetItem("ModPlusAPI", "h22") + " " +
                TbLocalLicenseServerIpAddress.Text + ":" + TbLocalLicenseServerPort.Value).ConfigureAwait(true);
        }

        private bool _restartClientOnClose = true;

        private void BtStopConnectionToLicenseServer_OnClick(object sender, RoutedEventArgs e)
        {
            ClientStarter.StopConnection();
            BtRestoreConnectionToLicenseServer.IsEnabled = true;
            BtStopConnectionToLicenseServer.IsEnabled = false;
            TbLocalLicenseServerIpAddress.IsEnabled = true;
            TbLocalLicenseServerPort.IsEnabled = true;
            _restartClientOnClose = false;
        }

        private void BtRestoreConnectionToLicenseServer_OnClick(object sender, RoutedEventArgs e)
        {
            ClientStarter.StartConnection(ProductLicenseType.Revit);
            BtRestoreConnectionToLicenseServer.IsEnabled = false;
            BtStopConnectionToLicenseServer.IsEnabled = true;
            TbLocalLicenseServerIpAddress.IsEnabled = false;
            TbLocalLicenseServerPort.IsEnabled = false;
            _restartClientOnClose = true;
        }

        private void CbLanguages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // fill image
            if (e.AddedItems[0] is Language.LangItem li)
            {
                ModPlusAPI.Language.SetCurrentLanguage(li.Name);
                this.SetLanguageProviderForModPlusWindow();
                SetLanguageValues();
                if (TbMessageAboutLanguage != null && _curLangItem != null)
                {
                    TbMessageAboutLanguage.Visibility = li.Name == _curLangItem.Name
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                }
                try
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri($"pack://application:,,,/ModPlus_Revit_{MpVersionData.CurRevitVers};component/Resources/Flags/{li.Name}.png");
                    bi.EndInit();
                    LanguageImage.Source = bi;
                }
                catch
                {
                    LanguageImage.Source = null;
                }
            }
        }
    }
}
