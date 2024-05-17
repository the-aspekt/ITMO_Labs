using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFirstPlugin
{
    internal class WallsSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.GetType().Equals(typeof(Wall));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
