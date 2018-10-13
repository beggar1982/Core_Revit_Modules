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
    using ModPlusAPI.Windows;

    public partial class MpMainSettings
    {
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
        private void GetDataFromConfigFile()
        {
            // Separator
            var separator = Regestry.GetValue("Separator");
            CbSeparatorSettings.SelectedIndex = string.IsNullOrEmpty(separator) ? 0 : int.Parse(separator);
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
