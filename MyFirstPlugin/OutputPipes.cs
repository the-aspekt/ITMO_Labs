using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class OutputPipes : IExternalCommand
    {
       public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;        

            Selection currentSelection = uIDocument.Selection;
            List<Pipe> pipes = new List<Pipe>();

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string currentDate = DateTime.Now.ToString("HH-mm");
            string filename = "pipes" + currentDate + ".xlsx";

            string xlsxPath = Path.Combine(desktopPath, filename);

            double outerDiameter;
            double innerDiameter;
            double length;
            if (currentSelection.GetElementIds().Count < 1)
            {
                TaskDialog.Show("Первое действие", "Выберите элементы");
                List<Reference> pickedElement;
                try
                {
                    pickedElement = currentSelection.PickObjects(ObjectType.Element, "Выберите элементы").ToList();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                foreach (Reference element in pickedElement)
                {
                    Element thisElement = document.GetElement(element);
                    if(thisElement.GetType() == typeof(Pipe))
                    {
                        Pipe pipe = document.GetElement(element) as Pipe;
                        pipes.Add(pipe);
                    }                    
                }
            }
            else
            {
                var currentSelectionElementIDs = currentSelection.GetElementIds();
                pipes = new FilteredElementCollector(document, currentSelectionElementIDs)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(Pipe))
                    .Cast<Pipe>()
                    .ToList();               
            }

            using (FileStream sm = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("Лист1");

                int rowIndex = 0;
                foreach (Pipe pipe in pipes)
                {
                    outerDiameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble();
                    outerDiameter = Math.Round(UnitUtils.ConvertFromInternalUnits(outerDiameter, UnitTypeId.Millimeters));
                    innerDiameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM).AsDouble();                    
                    innerDiameter = Math.Round(UnitUtils.ConvertFromInternalUnits(innerDiameter, UnitTypeId.Millimeters));
                    length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                    length = UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters);
                    sheet.SetCellValue(rowIndex, 0, pipe.Name);
                    sheet.SetCellValue(rowIndex, 1, outerDiameter);
                    sheet.SetCellValue(rowIndex, 2, innerDiameter);
                    sheet.SetCellValue(rowIndex, 3, length);
                    rowIndex++;
                }
                workbook.Write(sm);
                workbook.Close();                
            }

            if (pipes.Count == 0)
                TaskDialog.Show("Завершено", "Не выбрано ни одной трубы");
            else
                TaskDialog.Show("Завершено", $"Записано {pipes.Count} стен в файл {filename}");

            return Result.Succeeded;
        }

    }
}
