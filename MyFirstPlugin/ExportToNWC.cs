using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
    public class ExportToNWC : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uIApplication = commandData.Application;
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string currentDate = DateTime.Now.ToString("HH-mm");
            string filename = "newNWC" + currentDate + ".nwc";

            NavisworksExportOptions ifcExportOptions = new NavisworksExportOptions();

            var default3dview = new FilteredElementCollector(document).
                                    OfClass(typeof(View3D))
                                    .Cast<View3D>()
                                    .FirstOrDefault();

            ifcExportOptions.ViewId = default3dview.Id;

            using (Transaction t = new Transaction(document))
            {
                t.Start($"Экспорт в NWC");
                document.Export(desktopPath, filename, ifcExportOptions);
                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
            
