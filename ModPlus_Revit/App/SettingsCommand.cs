namespace ModPlus_Revit.App
{
    using Autodesk.Revit.DB;

    /// <inheritdoc />
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SettingsCommand : Autodesk.Revit.UI.IExternalCommand
    {
        /// <inheritdoc />
        public Autodesk.Revit.UI.Result Execute(Autodesk.Revit.UI.ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var win = new SettingsWindow();
            var viewModel = new SettingsViewModel(win);
            win.DataContext = viewModel;
            win.Closed += (sender, args) => viewModel.ApplySettings();
            win.ShowDialog();
            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}