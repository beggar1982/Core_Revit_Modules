namespace ModPlus_Revit
{
    using System;
    using System.Linq;
    using System.Windows;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    /// <inheritdoc/>
    public class RevitEvent : IExternalEventHandler
    {
        private Action _doAction;
        private Document _doc;
        private readonly ExternalEvent _exEvent;
        private bool _skipFailures;

        /// <summary>
        /// Initializes a new instance of the <see cref="RevitEvent"/> class.
        /// </summary>
        public RevitEvent()
        {
            _exEvent = ExternalEvent.Create(this);
        }

        /// <summary>
        /// Запустить внешнее событие
        /// </summary>
        /// <param name="doAction">Действие, которое необходимо выполнить в событии</param>
        /// <param name="skipFailures">Пропускать ли предупреждения, возникшие при фиксации транзакции</param>
        /// <param name="doc">Документ. Если не указан, берется текущий документ</param>
        public void Run(Action doAction, bool skipFailures, Document doc = null)
        {
            _doAction = doAction;
            _skipFailures = skipFailures;
            _doc = doc;
            _exEvent.Raise();
        }

        /// <inheritdoc/>
        public void Execute(UIApplication app)
        {
            try
            {
                if (_doAction != null)
                {
                    if (_doc == null)
                        _doc = app.ActiveUIDocument.Document;

                    if (_skipFailures)
                        app.Application.FailuresProcessing += Application_FailuresProcessing;
                    
                    _doAction();

                    if (_skipFailures)
                        app.Application.FailuresProcessing -= Application_FailuresProcessing;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + exception.StackTrace);
                if (_skipFailures)
                    app.Application.FailuresProcessing -= Application_FailuresProcessing;
            }
        }

        private static void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            // Inside event handler, get all warnings
            var failList = e.GetFailuresAccessor().GetFailureMessages();
            if (failList.Any())
            {
                // skip all failures
                e.GetFailuresAccessor().DeleteAllWarnings();
                e.SetProcessingResult(FailureProcessingResult.Continue);
            }
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return "RevitEvent";
        }
    }
}
