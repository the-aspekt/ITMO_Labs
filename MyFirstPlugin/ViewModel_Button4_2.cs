using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using RevitAPITrainingLibrary;

namespace MyFirstPlugin
{
    public class ViewModel_Button4_2
    {

        private ExternalCommandData _commandData;
        private Document _doc;

        public DelegateCommand WallTypeApplyCommand { get; }
        public List<Wall> SelectedWalls { get; set; } = new List<Wall>();
        public List<WallType> WallTypes { get; set; } = new List<WallType>();
        public WallType SelectedWallType { get; set; }

        public event EventHandler HideRequest;
        private void RaiseHideRequest()
        {
            HideRequest?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ShowRequest;
        private void RaiseShowRequest()
        {
            ShowRequest?.Invoke(this, EventArgs.Empty);
        }

        public ViewModel_Button4_2(ExternalCommandData commandData)
        {
            _commandData = commandData;

            _doc = _commandData.Application.ActiveUIDocument.Document;
            WallTypeApplyCommand = new DelegateCommand(OnWallTypeApplyCommand);
            WallTypes = WallsUtils.GetTypes(_doc);
            SelectedWalls = SelectionUtils.SelectWalls(_commandData);
        }       

        private void OnWallTypeApplyCommand()
        {
            RaiseHideRequest();
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            if (SelectedWallType == null || SelectedWalls == null)
            {
                return;
            }

            using (Transaction t = new Transaction(document))
            {
                t.Start($"Корректировка типов стен");
                foreach (Wall wall in SelectedWalls)
                {
                    wall.ChangeTypeId(SelectedWallType.Id);
                }
                TaskDialog.Show("Завершено", $"Обработано стен: {SelectedWalls.Count}");
                t.Commit();
            }

            RaiseShowRequest();
        }


    }
}
