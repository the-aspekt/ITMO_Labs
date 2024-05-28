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

namespace MyFirstPlugin
{
    public class ViewModel_Button4_2
    {

        private ExternalCommandData _commandData;

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
            WallTypeApplyCommand = new DelegateCommand(OnWallTypeApplyCommand);
            WallTypes = GetWallTypes();
            SelectedWalls = SelectWalls();
        }

        public List<WallType> GetWallTypes()
        {
            WallTypes = new FilteredElementCollector(_commandData.Application.ActiveUIDocument.Document)
               .OfClass(typeof(WallType))
               .Cast<WallType>()
               .ToList();
            return WallTypes;
        }

        public List<Wall> SelectWalls()
        {
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            Selection currentSelection = uIDocument.Selection;

            List<Wall> walls = new List<Wall>();

            if (currentSelection.GetElementIds().Count < 1)
            {
                TaskDialog.Show("Первое действие", "Выберите стены");
                WallsSelectionFilter wsf = new WallsSelectionFilter();
                List<Reference> pickedElement;

                try
                {
                    pickedElement = currentSelection.PickObjects(ObjectType.Element, wsf, "Выберите стены").ToList();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return null;
                }

                foreach (Reference element in pickedElement)
                {
                    Wall wall = document.GetElement(element) as Wall;
                    walls.Add(wall);
                }
            }
            else
            {
                var currentSelectionElementIDs = currentSelection.GetElementIds();
                walls = new FilteredElementCollector(document, currentSelectionElementIDs)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(Wall))
                    .Cast<Wall>()
                    .ToList();
            }

            return walls;
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
                    wall.WallType = SelectedWallType;
                }
                TaskDialog.Show("Завершено", $"Обработано стен: {SelectedWalls.Count}");
                t.Commit();
            }

            RaiseShowRequest();
        }


    }
}
