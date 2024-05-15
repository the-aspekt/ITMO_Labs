using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CountAirChannels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            var allChannels = new FilteredElementCollector(document)
                .OfClass(typeof(Duct))
                .Cast<Duct>()
                .ToList();

            var allFlexChannels = new FilteredElementCollector(document)
                .OfClass(typeof(FlexDuct))
                .Cast<FlexDuct>()
                .ToList();

            TaskDialog.Show("Завершено", $"Количество воздуховодов в модели: {allChannels.Count} {Environment.NewLine} Количество гибких воздуховодов в модели: {allFlexChannels.Count} ");


            return Result.Succeeded;
        }
    }
}
