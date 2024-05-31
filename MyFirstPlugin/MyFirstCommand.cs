using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
    public class MyFirstCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document document = commandData.Application.ActiveUIDocument.Document;

            Selection currentSelection = commandData.Application.ActiveUIDocument.Selection;
            FamilyInstance currentFamilyInstance, secondFamilyInstance;
            Reference pickedElement;


            if (currentSelection.GetElementIds().Count < 1)
            {
                TaskDialog.Show("Первое действие", "Выберите семейство, из которого нужно забрать параметры");
                pickedElement = currentSelection.PickObject(ObjectType.Element, "Выберите семейство, из которого нужно забрать параметры");
                currentFamilyInstance = document.GetElement(pickedElement) as FamilyInstance;
            }
            else
            {
                ElementId currentFamilyInstanceElementId = currentSelection.GetElementIds().First();
                currentFamilyInstance = document.GetElement(currentFamilyInstanceElementId) as FamilyInstance;
            }
            
            FamilySymbol firstElementFamilySymbol = currentFamilyInstance.Symbol;
            ParameterSet firstElemParameterSet = firstElementFamilySymbol.Parameters;
            TaskDialog.Show("Второе действие", "Выберите семейство, в которое нужно вставить параметры");
            pickedElement = currentSelection.PickObject(ObjectType.Element, "Выберите семейство, в которое нужно вставить параметры");
            secondFamilyInstance = document.GetElement(pickedElement) as FamilyInstance;
            FamilySymbol secondFamilySymbol = secondFamilyInstance.Symbol;
            ParameterSet secondElemParameterSet = secondFamilySymbol.Parameters;

            List<string> stringsOfElemParameters = new List<string>();            
            List<string> stringsOf2ElemParameters = new List<string>();

            foreach (Parameter item in firstElemParameterSet)
            {               
                if (item.IsReadOnly == false)
                {
                    stringsOfElemParameters.Add(item.Definition.Name);
                }
            }
            foreach (Parameter item in secondElemParameterSet)
            {                
                if (item.IsReadOnly == false)
                {
                    stringsOf2ElemParameters.Add(item.Definition.Name);
                }
            }
            //получаем список совпадающих в 2-х семействах параметров
            List<string> commonStrings = stringsOfElemParameters.Intersect(stringsOf2ElemParameters).ToList();

            //преобразовав его в словарь, оставляем только те параметры, у которых параметры имеют значение и отличаются
            Dictionary<string, Parameter> parametersValue = new Dictionary<string, Parameter>();
            for (int i = 0; i < commonStrings.Count; i++)
            {
                Parameter processedParameter = secondFamilySymbol.LookupParameter(commonStrings[i]);
                Parameter newParameter = firstElementFamilySymbol.LookupParameter(commonStrings[i]);
                if (processedParameter.HasValue 
                   && newParameter.AsValueString() != processedParameter.AsValueString())
                    {
                        parametersValue.Add(processedParameter.Definition.Name, processedParameter);
                    }
            }

            using (Transaction t = new Transaction(document))
            {
                t.Start($"Корректировка параметров семейства {secondFamilySymbol.Name}");
                foreach (var pair in parametersValue)
                {
                    string processedParameterName = pair.Key;
                    Parameter processedParameter = pair.Value;
                    StorageType thisType = processedParameter.StorageType;
                    Parameter newParameter = firstElementFamilySymbol.LookupParameter(processedParameterName);
                    Parameter parameter = secondFamilySymbol.LookupParameter(processedParameterName);
                    //проверяем тип параметра
                        if (thisType == StorageType.Integer)
                        {
                            parameter.Set(newParameter.AsInteger());
                        }
                        else if (thisType == StorageType.Double)
                        {
                            parameter.Set(newParameter.AsDouble());
                        }
                        else if (thisType == StorageType.String)
                        {
                            parameter.Set(newParameter.AsString());
                        }
                        else
                            break;
                }
                t.Commit();
            }

            TaskDialog.Show("Завершено", $"Обработано параметров: {parametersValue.Count}");          

            return Result.Succeeded;
        }
    }
}
