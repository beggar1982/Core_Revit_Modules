using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using MahApps.Metro;
using ModPlusAPI;
using ModPlusAPI.Windows;
using ModPlusAPI.Windows.Helpers;

namespace ModPlus_Revit.App
{
    public partial class MpMainSettings
    {
        private string _curUserEmail = string.Empty;
        private string _curTheme = string.Empty;
        private string _curColor = string.Empty;
        private readonly string _curLang;
        public List<AccentColorMenuData> AccentColors { get; set; }
        public List<AppThemeMenuData> AppThemes { get; set; }
        private const string LangItem = "RevitDlls";

        public MpMainSettings()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");
            FillThemesAndColors();
            SetAppRegistryKeyForCurrentUser();
            GetDataFromConfigFile();
            GetDataByVars();
            Closing += MpMainSettings_Closing;
            Closed += MpMainSettings_OnClosed;
            // fill languages
            CbLanguages.ItemsSource = ModPlusAPI.Language.GetLanguagesByFiles();
            CbLanguages.SelectedItem = ((List<Language.LangItem>)CbLanguages.ItemsSource)
                .FirstOrDefault(x => x.Name.Equals(ModPlusAPI.Language.CurrentLanguageName));
            _curLang = ((Language.LangItem)CbLanguages.SelectedItem)?.Name;
            CbLanguages.SelectionChanged += CbLanguages_SelectionChanged;
        }
        // Change language
        private void CbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is Language.LangItem langItem)
            {
                ModPlusAPI.Language.SetCurrentLanguage(langItem);
                ModPlusAPI.Language.SetLanguageProviderForWindow(this);
            }
        }
        private void FillThemesAndColors()
        {
            // create accent color menu items for the demo
            AccentColors = ThemeManager.Accents
                                            .Select(a => new AccentColorMenuData() { Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush })
                                            .ToList();

            // create metro theme color menu items for the demo
            AppThemes = ThemeManager.AppThemes
                                           .Select(a => new AppThemeMenuData() { Name = a.Name, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush })
                                           .ToList();

            MiColor.ItemsSource = AccentColors;
            MiTheme.ItemsSource = AppThemes;

            // Устанавливаем текущие. На всякий случай "без ошибок"
            try
            {
                _curTheme = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "Theme");
                foreach (var item in MiTheme.Items.Cast<AppThemeMenuData>().Where(item => item.Name.Equals(_curTheme)))
                {
                    MiTheme.SelectedIndex = MiTheme.Items.IndexOf(item);
                }

                _curColor = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "AccentColor");
                foreach (
                    var item in MiColor.Items.Cast<AccentColorMenuData>().Where(item => item.Name.Equals(_curColor)))
                {
                    MiColor.SelectedIndex = MiColor.Items.IndexOf(item);
                }
            }
            catch
            {
                //ignored
            }
        }
        // Заполнение поля Ключ продукта
        private void SetAppRegistryKeyForCurrentUser()
        {
            TbRegistryKey.Text = Variables.RegistryKey;
            var regVariant = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.User, "RegestryVariant");
            if (!string.IsNullOrEmpty(regVariant))
            {
                TbAboutRegKey.Visibility = System.Windows.Visibility.Visible;
                if (regVariant.Equals("0"))
                    TbAboutRegKey.Text = ModPlusAPI.Language.GetItem(LangItem, "h10") + " " +
                        UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.User, "HDmodel");
                else if (regVariant.Equals("1"))
                    TbAboutRegKey.Text = ModPlusAPI.Language.GetItem(LangItem, "h11") + " " +
                        UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.User, "gName");
            }
        }

        private void ChangeWindowTheme()
        {
            //Theme
            try
            {
                ThemeManager.ChangeAppStyle(this,
                    ThemeManager.Accents.First(
                        x => x.Name.Equals(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "AccentColor"))
                        ),
                    ThemeManager.AppThemes.First(
                        x => x.Name.Equals(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "Theme")))
                    );

            }
            catch
            {
                //ignored
            }
        }
        // Загрузка данных из файла конфигурации
        // которые требуется отобразить в окне
        private void GetDataFromConfigFile()
        {
            // Separator
            var separator = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "Separator");
            CbSeparatorSettings.SelectedIndex = string.IsNullOrEmpty(separator) ? 0 : int.Parse(separator);
            // Check updates and new

            // Виды границ окна
            var border = UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "BordersType");
            foreach (ComboBoxItem item in CbWindowsBorders.Items)
            {
                if (item.Tag.Equals(border))
                {
                    CbWindowsBorders.SelectedItem = item; break;
                }
            }
            if (CbWindowsBorders.SelectedIndex == -1) CbWindowsBorders.SelectedIndex = 3;
        }
        /// <summary>
        /// Получение значений из глобальных переменных плагина
        /// </summary>
        private void GetDataByVars()
        {
            try
            {
                // email
                TbEmailAdress.Text = _curUserEmail = Variables.UserEmail;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        // Выбор разделителя целой и дробной части для чисел
        private void CbSeparatorSettings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "Separator",
                ((ComboBox)sender).SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
        }
        // Выбор темы
        private void MiTheme_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "Theme", ((AppThemeMenuData)e.AddedItems[0]).Name, true);
            ChangeWindowTheme();
        }
        // Выбор цвета
        private void MiColor_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "AccentColor", ((AccentColorMenuData)e.AddedItems[0]).Name, true);
            ChangeWindowTheme();
        }
        // windows borders select
        private void CbWindowsBorders_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (!(cb?.SelectedItem is ComboBoxItem cbi)) return;
            this.ChangeWindowBordes(cbi.Tag.ToString());
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "BordersType", cbi.Tag.ToString(), true);
        }

        private void MpMainSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(TbEmailAdress.Text))
            {
                if (IsValidEmail(TbEmailAdress.Text))
                    TbEmailAdress.BorderBrush = FindResource("TextBoxBorderBrush") as Brush;
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
                UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "MainSet", "Separator",
                    CbSeparatorSettings.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                // Сохраняем в реестр почту, если изменилась
                Variables.UserEmail = TbEmailAdress.Text;
                if (!TbEmailAdress.Text.Equals(_curUserEmail))
                {
                    var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("ModPlus");
                    using (key)
                        key?.SetValue("email", TbEmailAdress.Text);
                }
                if (!((Language.LangItem)CbLanguages.SelectedItem).Name.Equals(_curLang))
                {
                    ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "tt5"));
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
                    tb.BorderBrush = FindResource("TextBoxBorderBrush") as Brush;
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
    }

    public class AccentColorMenuData
    {
        public string Name { get; set; }
        public Brush BorderColorBrush { get; set; }
        public Brush ColorBrush { get; set; }

    }
    public class AppThemeMenuData : AccentColorMenuData
    {
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
