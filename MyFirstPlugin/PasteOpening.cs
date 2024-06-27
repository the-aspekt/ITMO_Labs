using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MathNet.Numerics.RootFinding;
using NPOI.HSSF.Record.AutoFilter;
using RevitAPITrainingLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class PasteOpening : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string openingFamilyName = "ADSK_ОбобщеннаяМодель_ОтверстиеПрямоугольное_ПоГрани";

            try
            {
                UIDocument uIDocument = commandData.Application.ActiveUIDocument;
                Document arDocument = uIDocument.Document;
                Document ovDocument = arDocument.Application.Documents.OfType<Document>().Where(x => x.Title.Contains("ОВ") || x.Title.Contains("ВК") || x.Title.Contains("сети")).FirstOrDefault();
                if (ovDocument == null)
                {
                    TaskDialog.Show("Ошибка", "Не найден загруженный файл ОВ");
                    return Result.Cancelled;
                }
                List<Duct> ducts = DuctsUtils.GetElements(ovDocument);
                /*
                if(ducts == null || ducts.Count == 0)
                {
                    TaskDialog.Show("Ошибка", "Нет воздуховодов в загруженном файле ОВ");
                    return Result.Cancelled;
                }
                */
                List<Pipe> pipes = PipesUtils.GetElements(ovDocument);
                var allSymbols = FamiliesSymbolsUtils.GetSymbols(arDocument).Where(x => x.FamilyName.Contains(openingFamilyName));

                FamilySymbol familySymbolOV = allSymbols.Where(x => x.Name.Contains("ОВ")).FirstOrDefault();
                FamilySymbol familySymbolVK = allSymbols.Where(x => x.Name.Contains("ВК")).FirstOrDefault();

                if (familySymbolOV == null && familySymbolVK == null)
                {
                    TaskDialog.Show("Ошибка", "Семейства для отверстий не найдены");
                    return Result.Cancelled;
                }

                var view = ViewsUtils.Get3DViews(arDocument).FirstOrDefault();
                if (view == null)
                {
                    TaskDialog.Show("Ошибка", "Подходящий вид не найден");
                    return Result.Cancelled;
                }
                ReferenceIntersector referenceIntersector = new ReferenceIntersector(new ElementClassFilter(typeof(Wall)), FindReferenceTarget.Element, view);

                List<IntersectionContainer> intersectionContainers = new List<IntersectionContainer>();

                if (ducts.Count > 0)
                {
                    foreach (Duct duct in ducts)
                    {
                        Line line = (duct.Location as LocationCurve).Curve as Line;
                        XYZ startPoint = line.GetEndPoint(0);
                        XYZ direction = line.Direction;

                        List<ReferenceWithContext> references = referenceIntersector.Find(startPoint, direction)
                            .Where(x=>x.Proximity <= line.Length)
                            .Distinct(new ReferenceWithContextElementEqualityComparer())
                            .ToList();

                        foreach (ReferenceWithContext r in references)
                        {
                            double proximity = r.Proximity;
                            Reference reference = r.GetReference();
                            Wall wall = arDocument.GetElement(reference.ElementId) as Wall;
                            Level level = arDocument.GetElement(wall.LevelId) as Level;
                            XYZ pointHole = startPoint + direction * proximity;
                            IntersectionContainer intersectionContainer = new IntersectionContainer(pointHole, level, wall, duct);
                            intersectionContainers.Add(intersectionContainer);
                            
                        }
                    }
                }
                if (pipes.Count > 0)
                {
                    foreach (Pipe pipe in pipes)
                    {
                        Line line = (pipe.Location as LocationCurve).Curve as Line;
                        XYZ startPoint = line.GetEndPoint(0);
                        XYZ direction = line.Direction;

                        List<ReferenceWithContext> references = referenceIntersector.Find(startPoint, direction)
                            .Where(x => x.Proximity <= line.Length)
                            .Distinct(new ReferenceWithContextElementEqualityComparer())
                            .ToList();

                        foreach (ReferenceWithContext r in references)
                        {
                            double proximity = r.Proximity;
                            Reference reference = r.GetReference();
                            Wall wall = arDocument.GetElement(reference.ElementId) as Wall;
                            Level level = arDocument.GetElement(wall.LevelId) as Level;
                            XYZ pointHole = startPoint + direction * proximity;
                            IntersectionContainer intersectionContainer = new IntersectionContainer(pointHole, level, wall, null, pipe);
                            intersectionContainers.Add(intersectionContainer);

                        }
                    }
                }

                using (var t = new Transaction(arDocument, $"Создание {intersectionContainers.Count} отверстий"))
                {
                    t.Start();
                    foreach (var i in intersectionContainers)
                    {                       
                        if (i.Duct == null)
                        {
                            if (!familySymbolVK.IsActive)
                                familySymbolVK.Activate();
                           // var hole = arDocument.Create.NewFamilyInstance(i.PointHole, familySymbolVK, i.Wall, i.Level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            Reference reference = HostObjectUtils.GetSideFaces(i.Wall, ShellLayerType.Exterior).FirstOrDefault();
                            XYZ refDirection = new XYZ(0, 0, 1);
                            var hole = arDocument.Create.NewFamilyInstance(reference, i.PointHole, refDirection, familySymbolVK);
                            Parameter width = hole.LookupParameter("ADSK_Отверстие_Ширина");
                            Parameter height = hole.LookupParameter("ADSK_Отверстие_Высота");
                            Parameter depth = hole.LookupParameter("ADSK_Размер_Толщина основы");
                            double holeDiameter = UnitUtils.ConvertToInternalUnits( Math.Ceiling( UnitUtils.ConvertFromInternalUnits(i.Pipe.Diameter,UnitTypeId.Millimeters) / 50) * 50, UnitTypeId.Millimeters);
                            width.Set(holeDiameter);
                            height.Set(holeDiameter);
                            depth.Set(i.Wall.Width);
                        }
                        else
                        {
                            if (!familySymbolOV.IsActive)
                                familySymbolOV.Activate();
                            //var hole = arDocument.Create.NewFamilyInstance(i.PointHole, familySymbolOV, i.Wall, i.Level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);                           
                            Reference reference = HostObjectUtils.GetSideFaces(i.Wall, ShellLayerType.Exterior).FirstOrDefault();
                            XYZ refDirection = new XYZ(0, 0, 1);
                            var hole = arDocument.Create.NewFamilyInstance(reference, i.PointHole, refDirection, familySymbolOV);
                            Parameter width = hole.LookupParameter("ADSK_Отверстие_Ширина");
                            Parameter height = hole.LookupParameter("ADSK_Отверстие_Высота");
                            Parameter depth = hole.LookupParameter("ADSK_Размер_Толщина основы");
                            depth.Set(i.Wall.Width);
                            double diameter = 0;
                            try
                            {
                                diameter = i.Duct.Diameter;
                                width.Set(diameter);
                                height.Set(diameter);
                            }
                            catch (Exception ex) 
                            {
                                if (diameter == 0)
                                {
                                    height.Set(i.Duct.Width);
                                    width.Set(i.Duct.Height);
                                    /*
                                    Transform myTransform = hole.GetTransform();
                                    var face = arDocument.GetElement(reference).GetGeometryObjectFromReference(reference) as Face;
                                    Plane plane = face.GetSurface() as Plane;
                                    var normal = plane.Normal;
                                    Line myLine = Line.CreateUnbound(myTransform.Origin, normal);
                                    ElementTransformUtils.RotateElement(arDocument, hole.Id, myLine, Math.PI / 2);
                                    */
                                }
                            }                            
                        }
                    }
                    t.Commit();
                }

                return Result.Succeeded;

            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

        }

        public class IntersectionContainer
        {
            public IntersectionContainer(XYZ pointHole, Level level, Wall wall, Duct duct = null, Pipe pipe = null)
            {
                PointHole = pointHole;
                Level = level;
                Wall = wall;
                Duct = duct;
                Pipe = pipe;
            }

            public XYZ PointHole { get; }
            public Level Level { get; }
            public Wall Wall { get; }
            public Duct Duct { get; }
            public Pipe Pipe { get; }

        }

        public class ReferenceWithContextElementEqualityComparer : IEqualityComparer<ReferenceWithContext>
        {
            public bool Equals(ReferenceWithContext x, ReferenceWithContext y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(null, x)) return false;
                if (ReferenceEquals(null, y)) return false;

                var xReference = x.GetReference();

                var yReference = y.GetReference();

                return xReference.LinkedElementId == yReference.LinkedElementId
                           && xReference.ElementId == yReference.ElementId;
            }

            public int GetHashCode(ReferenceWithContext obj)
            {
                var reference = obj.GetReference();

                unchecked
                {
                    return (reference.LinkedElementId.GetHashCode() * 397) ^ reference.ElementId.GetHashCode();
                }
            }
        }
    }
}
