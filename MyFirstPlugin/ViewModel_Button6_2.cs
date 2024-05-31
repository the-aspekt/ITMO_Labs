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
using Autodesk.Revit.DB.Mechanical;
using RevitAPITrainingLibrary;

namespace MyFirstPlugin
{
    public class ViewModel_Button6_2
    {
        private ExternalCommandData _commandData;

        public DelegateCommand CreateFurnitureCommand { get; }
        public List<FamilySymbol> FurnitureTypes { get; set; } = new List<FamilySymbol>();
        public FamilySymbol SelectedFurnitureType { get; set; }
        public List<Level> Levels { get; set; } = new List<Level>();
        public Level SelectedLevel { get; set; }
        public XYZ XYZPoint { get; set; }

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

        public ViewModel_Button6_2(ExternalCommandData commandData)
        {
            _commandData = commandData;
            CreateFurnitureCommand = new DelegateCommand(OnCreateFurnitureCommand);
            FurnitureTypes = FurnitureUtils.GetSymbols(_commandData);            
            Levels = LevelsUtils.GetLevels(_commandData);

        }             

        private void OnCreateFurnitureCommand()
        {
            RaiseHideRequest();
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            if (SelectedFurnitureType == null || SelectedLevel == null)
            {
                return;
            }

            XYZPoint = SelectionUtils.GetPoint(_commandData, ObjectSnapTypes.Endpoints);
            FamiliesInstancesUtils.CreateFamilyInstance(_commandData, SelectedFurnitureType, XYZPoint, SelectedLevel, "Создание семейства мебели");

            RaiseShowRequest();
        }
    }
}
