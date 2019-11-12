﻿namespace ModPlus_Revit.App
{
    using Autodesk.Revit.DB;

    /// <inheritdoc />
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class MpMainSettingsFunction : Autodesk.Revit.UI.IExternalCommand
    {
        /// <inheritdoc />
        public Autodesk.Revit.UI.Result Execute(Autodesk.Revit.UI.ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var win = new MpMainSettings();
            win.ShowDialog();
            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}