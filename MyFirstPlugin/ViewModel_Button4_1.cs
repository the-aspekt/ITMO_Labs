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
    public class ViewModel_Button4_1
    {
        private ExternalCommandData _commandData;

        public DelegateCommand CountAllPipesCommand { get; }
        public DelegateCommand CountAllWallsVolumeCommand { get; }
        public DelegateCommand CountAllDoorsCommand { get; }

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

        public ViewModel_Button4_1(ExternalCommandData commandData)
        {
            _commandData = commandData;
            CountAllPipesCommand = new DelegateCommand(OnCountAllPipesCommand);
            CountAllWallsVolumeCommand = new DelegateCommand(OnCountAllWallsVolumeCommand);
            CountAllDoorsCommand = new DelegateCommand(OnCountAllDoorsCommand);
        }

        private void OnCountAllDoorsCommand()
        {
            RaiseHideRequest();

            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            List<FamilyInstance> doors = new FilteredElementCollector(document)
               .OfCategory(BuiltInCategory.OST_Doors)
               .WhereElementIsNotElementType()
               .Cast<FamilyInstance>()
               .ToList();

            TaskDialog.Show("Завершено", $"Количество дверей в модели: {doors.Count}");

            RaiseShowRequest();
        }



        private void OnCountAllPipesCommand()
        {
            RaiseHideRequest();

            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            var allPipes = new FilteredElementCollector(document, document.ActiveView.Id)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .ToList();

            TaskDialog.Show("Завершено", $"Количество труб на активном виде: {allPipes.Count}");

            RaiseShowRequest();
        }


        private void OnCountAllWallsVolumeCommand()
        {
            RaiseHideRequest();

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
                    return;
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
            double volume = walls.Sum(wall => wall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble());
            volume = UnitUtils.ConvertFromInternalUnits(volume, UnitTypeId.CubicMeters);

            string finalMessage = $"Объем выбранных стен {volume}";

            TaskDialog.Show("Завершено", finalMessage);

            RaiseShowRequest();
        }
    }
}
