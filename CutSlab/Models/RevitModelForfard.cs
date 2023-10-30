using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using CutSlab.Models.Filters;
using CutSlab.Models;

namespace CutSlab
{
    public class RevitModelForfard
    {
        private UIApplication Uiapp { get; set; } = null;
        private Application App { get; set; } = null;
        private UIDocument Uidoc { get; set; } = null;
        private Document Doc { get; set; } = null;

        public RevitModelForfard(UIApplication uiapp)
        {
            Uiapp = uiapp;
            App = uiapp.Application;
            Uidoc = uiapp.ActiveUIDocument;
            Doc = uiapp.ActiveUIDocument.Document;
        }

        public List<DirectShape> GetTopLines()
        {
            Selection sel = Uiapp.ActiveUIDocument.Selection;
            var selectedLineElements = sel.PickElementsByRectangle(new DirectShapeClassFilter(), "Select Beam Top Lines");
            var directShapeLines = selectedLineElements.OfType<DirectShape>().ToList();

            return directShapeLines;
        }

        public List<ModelLine> GetBoundLines()
        {
            Selection sel = Uiapp.ActiveUIDocument.Selection;
            var references = sel.PickObjects(ObjectType.Element, new ModelLineClassFilter(), "Select Bound Lines");
            var lines = references.Select(r => Doc.GetElement(r)).OfType<ModelLine>().ToList();

            return lines;
        }

        public void CreateCuttingSolid(IEnumerable<CuttingSolid> cuttingSolids)
        {
            foreach (var solid in cuttingSolids)
            {
                solid.CreateTestTopLines(Doc);
            }
        }

        public void CreateSolidBetweenBeams(IEnumerable<CuttingSolid> cuttingSolids)
        {
            for (int i = 0; i < cuttingSolids.Count() - 1; i ++)
            {
                var referenceArrayArray = new ReferenceArrayArray();

                var firstProfile = cuttingSolids.ElementAt(i).EndProfile;
                var secondProfile = cuttingSolids.ElementAt(i + 1).StartProfile;

                referenceArrayArray.Append(firstProfile);
                referenceArrayArray.Append(secondProfile);

                using (Transaction trans = new Transaction(Doc, "Form Between Beams Created"))
                {
                    trans.Start();
                    var loftForm = Doc.FamilyCreate.NewLoftForm(true, referenceArrayArray);
                    trans.Commit();
                }
            }
        }

        // Метод получения строки с ElementId
        public string ElementIdToString(IEnumerable<Element> elements)
        {
            var stringArr = elements.Select(e => "Id" + e.Id.IntegerValue.ToString()).ToArray();
            string resultString = string.Join(", ", stringArr);

            return resultString;
        }

    }
}
