using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Newtonsoft.Json;
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
    public class OutputApertures : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /* 
             * Выведите в файл JSON следующие значения для всех окон и дверей: имя типа окна или двери, высота окна или двери, ширина окна или двери.
             */
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string currentDate = DateTime.Now.ToString("HH-mm");
            string filename = "apertures_data_" + currentDate + ".json";

            string jsonPath = Path.Combine(desktopPath, filename);
            List<Aperture> all = new List<Aperture>();

            //ElementCategoryFilter doorsCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            //ElementCategoryFilter windowsCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
            //LogicalOrFilter aperturesFilter = new LogicalOrFilter(windowsCategoryFilter, doorsCategoryFilter);

            //List<FamilyInstance> allApertures = new FilteredElementCollector(document)
            //    .WhereElementIsNotElementType()
            //    .WherePasses(aperturesFilter)
            //    .Cast<FamilyInstance>()
            //    .ToList();

            //foreach (FamilyInstance element in allApertures)
            //{
            //    Aperture aperture = new Aperture();
            //    aperture.Name = element.Name;
            //    if (element.get_Parameter(BuiltInParameter.ELEM_CATEGORY_PARAM).AsValueString() == "Окна")
            //    {
            //        aperture.Width = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble(), UnitTypeId.Meters);
            //        aperture.Height = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble(), UnitTypeId.Meters);
            //    }
            //    else
            //    {
            //        aperture.Width = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Meters);
            //        aperture.Height = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Meters);
            //    }
            //    all.Add(aperture);
            //}

            List<FamilyInstance> doors = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (FamilyInstance element in doors)
            {
                Aperture aperture = new Aperture();
                aperture.Name = element.Name;              
                aperture.Width = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Meters);
                if (aperture.Width == 0)                
                    aperture.Width = UnitUtils.ConvertFromInternalUnits(element.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Meters);                
                aperture.Height = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Meters);
                if (aperture.Height == 0)
                    aperture.Height = UnitUtils.ConvertFromInternalUnits(element.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Meters);
                aperture.FamilyType = BuiltInCategory.OST_Doors.ToString();
                all.Add(aperture);
            }

            List<FamilyInstance> windows = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (FamilyInstance element in doors)
            {
                Aperture aperture = new Aperture();
                aperture.Name = element.Name;
                aperture.Width = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble(), UnitTypeId.Meters);
                if (aperture.Width == 0)
                    aperture.Width = UnitUtils.ConvertFromInternalUnits(element.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble(), UnitTypeId.Meters);
                aperture.Height = UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble(), UnitTypeId.Meters);
                if (aperture.Height == 0)
                    aperture.Height = UnitUtils.ConvertFromInternalUnits(element.Symbol.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble(), UnitTypeId.Meters);
                aperture.FamilyType = BuiltInCategory.OST_Windows.ToString();
                all.Add(aperture);
            }

            if (all.Count == 0)
            {
                TaskDialog.Show("Завершено", $"Отсутствуют двери или окна");
                return Result.Cancelled;
            }

            string json = JsonConvert.SerializeObject(all, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
            TaskDialog.Show("Завершено", $"Количество дверей и окон: {all.Count}");
            return Result.Succeeded;
        }
    }
}
