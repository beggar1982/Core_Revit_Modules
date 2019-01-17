namespace ModPlus_Revit.App
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Autodesk.Revit.DB;
    using ModPlusAPI;
    using ModPlusAPI.LicenseServer;
    using ModPlusAPI.Windows;
    using ModPlusStyle.Controls.Dialogs;

    public partial class MpMainSettings
    {
        private const string LangItem = "RevitDlls";

        public MpMainSettings()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");
            SetLanguageValues();
            FillThemesAndColors();
            SetAppRegistryKeyForCurrentUser();
            LoadSettingsFromConfigFileAndRegistry();
            GetDataByVars();
            Closing += MpMainSettings_Closing;
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
        
        // Заполнение поля Ключ продукта
        private void SetAppRegistryKeyForCurrentUser()
        {
            TbRegistryKey.Text = Variables.RegistryKey;
            var regVariant = Regestry.GetValue("RegestryVariant");
            if (!string.IsNullOrEmpty(regVariant))
            {
                TbAboutRegKey.Visibility = System.Windows.Visibility.Visible;
                if (regVariant.Equals("0"))
                    TbAboutRegKey.Text = ModPlusAPI.Language.GetItem(LangItem, "h10") + " " +
                        Regestry.GetValue("HDmodel");
                else if (regVariant.Equals("1"))
                    TbAboutRegKey.Text = ModPlusAPI.Language.GetItem(LangItem, "h11") + " " +
                        Regestry.GetValue("gName");
            }
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
        /// <summary>
        /// Получение значений из глобальных переменных плагина
        /// </summary>
        private void GetDataByVars()
        {
            try
            {
                // email
                TbEmailAdress.Text = Variables.UserEmail;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
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
        
        private void MpMainSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(TbEmailAdress.Text))
            {
                if (IsValidEmail(TbEmailAdress.Text))
                    TbEmailAdress.BorderBrush = FindResource("BlackBrush") as Brush;
                else
                {
                    TbEmailAdress.BorderBrush = Brushes.Red;
                    ModPlusAPI.Windows.MessageBox.Show(
                        ModPlusAPI.Language.GetItem(LangItem, "tt4"));
                    TbEmailAdress.Focus();
                    e.Cancel = true;
                }
            }
        }
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        private void MpMainSettings_OnClosed(object sender, EventArgs e)
        {
            try
            {
                // Так как эти значения хранятся в переменных, то их нужно перезаписать
                Regestry.SetValue("Separator", CbSeparatorSettings.SelectedIndex.ToString(CultureInfo.InvariantCulture));
                // Сохраняем в реестр почту, если изменилась
                Variables.UserEmail = TbEmailAdress.Text;

                // License server
                UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "DisableConnectionWithLicenseServerInRevit",
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
        private void TbEmailAdress_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (IsValidEmail(tb.Text))
                    tb.BorderBrush = FindResource("BlackBrush") as Brush;
                else tb.BorderBrush = Brushes.Red;
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async void BtCheckLocalLicenseServerConnection_OnClick(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync(
                ClientStarter.IsLicenseServerAvailable()
                    ? ModPlusAPI.Language.GetItem("ModPlusAPI", "h21")
                    : ModPlusAPI.Language.GetItem("ModPlusAPI", "h20"),
                ModPlusAPI.Language.GetItem("ModPlusAPI", "h22") + " " +
                TbLocalLicenseServerIpAddress.Text + ":" + TbLocalLicenseServerPort.Value);
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
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class MpMainSettingsFunction : Autodesk.Revit.UI.IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(Autodesk.Revit.UI.ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var win = new MpMainSettings();
            win.ShowDialog();
            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}
