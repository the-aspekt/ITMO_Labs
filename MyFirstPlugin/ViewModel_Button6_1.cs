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
    public class ViewModel_Button6_1
    {
        private ExternalCommandData _commandData;
        private Document _doc;

        public DelegateCommand CreateAirChannelCommand { get; }
        public List<DuctType> AirChannelTypes { get; set; } = new List<DuctType>();
        public DuctType SelectedAirChannelType { get; set; }
        public List<Level> Levels { get; set; } = new List<Level>();
        public Level SelectedLevel { get; set; }
        public List<MEPSystemType> ductSystemTypes { get; set; } = new List<MEPSystemType>();
        public MEPSystemType SelectedDuctSystemTypes { get; set; }
        public double AirChannelOffset { get; set; }
        public List<XYZ> Points { get; set; } = new List<XYZ>();

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

        public ViewModel_Button6_1(ExternalCommandData commandData)
        {
            _commandData = commandData;

            _doc = _commandData.Application.ActiveUIDocument.Document;

            CreateAirChannelCommand = new DelegateCommand(OnCreateAirChannelCommand);
            AirChannelTypes = DuctsUtils.GetDuctTypes(_doc);
            ductSystemTypes = DuctsUtils.GetDuctSystemTypes(_doc);
            Levels = LevelsUtils.GetLevels(_doc);

            SelectedDuctSystemTypes = ductSystemTypes.FirstOrDefault(m => m.SystemClassification == MEPSystemClassification.SupplyAir);
            AirChannelOffset = 0;

            Points = SelectionUtils.Get2Points(_commandData, ObjectSnapTypes.Endpoints);
        }             

        private void OnCreateAirChannelCommand()
        {
            RaiseHideRequest();
            UIApplication uIApplication = _commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            if (SelectedAirChannelType == null || SelectedLevel == null)
            {
                return;
            }

            using (Transaction t = new Transaction(document))
            {
                t.Start($"Создание воздуховода");
                 
                Duct createdDuct = Duct.Create(document, SelectedDuctSystemTypes.Id, SelectedAirChannelType.Id, SelectedLevel.Id, Points[0], Points[1]);
                createdDuct.LevelOffset = UnitUtils.ConvertToInternalUnits(AirChannelOffset, UnitTypeId.Millimeters);

                t.Commit();
            }

            RaiseShowRequest();
        }
    }
}
