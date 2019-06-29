namespace ModPlus_Revit.App
{
    using System;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI.Windows;

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class UserInfoCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                ModPlusAPI.UserInfo.UserInfoService.ShowUserInfo();
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }
    }
}
