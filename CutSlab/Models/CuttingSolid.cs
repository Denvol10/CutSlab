using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CutSlab.Models
{
    public class CuttingSolid : INotifyPropertyChanged
    {
        #region Верх балок
        private string _beamTopLinesIds;
        public string BeamTopLinesIds
        {
            get => _beamTopLinesIds; 
            set
            {
                _beamTopLinesIds = value;
                OnPropertyChanged("BeamTopLinesIds");
            }
        }

        private List<DirectShape> _beamTopLines;

        public List<DirectShape> BeamToplines 
        {
            get => _beamTopLines; 
            set
            {
                _beamTopLines = value;
            }
        }
        #endregion

        #region Границы подрезки
        private string _boundLinesIds;
        public string BoundLinesIds
        {
            get => _boundLinesIds;
            set
            {
                _boundLinesIds = value;
                OnPropertyChanged("BoundLinesIds");
            }
        }

        private List<ModelLine> _boundLines;

        public List<ModelLine> BoundLines
        {
            get => _boundLines;
            set
            {
                _boundLines = value;
            }
        }
        #endregion

        public CuttingSolid() 
        {
            BeamToplines = new List<DirectShape>();
            BoundLines = new List<ModelLine>();
        }

        public void CreateTestTopLines(Document doc)
        {
            Plane startPlane = GetPlaneByLine(BoundLines.First());
            Plane endPlane = GetPlaneByLine(BoundLines.Last());

            var topLines = BeamToplines.Select(d => d.get_Geometry(new Options())).Select(g => g.First() as Line);
            var startPoints = topLines.Select(l => LinePlaneIntersection(l, startPlane, out _));
            var endPoints = topLines.Select(l => LinePlaneIntersection(l, endPlane, out _));

            using (Transaction trans = new Transaction(doc, "Create Test Points"))
            {
                trans.Start();
                ReferencePointArray refStartPoints = new ReferencePointArray();
                foreach (var point in startPoints)
                {
                    refStartPoints.Append(doc.FamilyCreate.NewReferencePoint(point));
                }

                bool isSorted = CurveByPoints.SortPoints(refStartPoints);
                if (isSorted)
                {
                    CreateProfileByPoints(doc, refStartPoints);
                }

                trans.Commit();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private Plane GetPlaneByLine(ModelLine modelLine)
        {
            Curve curve = modelLine.GeometryCurve;
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            XYZ thirdPoint = startPoint + XYZ.BasisZ;

            Plane plane = Plane.CreateByThreePoints(startPoint, endPoint, thirdPoint);

            return plane;
        }

        private void CreateProfileByPoints(Document doc, ReferencePointArray points)
        {
            for (int i = 0; i < points.Size - 1; i ++)
            {
                var referencePointsArray = new ReferencePointArray();
                referencePointsArray.Append(points.get_Item(i));
                referencePointsArray.Append(points.get_Item(i + 1));

                doc.FamilyCreate.NewCurveByPoints(referencePointsArray);
            }

            XYZ firstPoint = points.get_Item(0).Position;
            XYZ endPoint = points.get_Item(points.Size - 1).Position;

            XYZ firstBasePoint = new XYZ(firstPoint.X, firstPoint.Y, 0);
            XYZ endBasePoint = new XYZ(endPoint.X, endPoint.Y, 0);

            ReferencePoint secondBaseReferencePoint = doc.FamilyCreate.NewReferencePoint(firstBasePoint);
            ReferencePoint endBaseReferencePoint = doc.FamilyCreate.NewReferencePoint(endBasePoint);

            var side1Points = new ReferencePointArray();
            side1Points.Append(points.get_Item(0));
            side1Points.Append(secondBaseReferencePoint);
            var sideLine1 = doc.FamilyCreate.NewCurveByPoints(side1Points);

            var side2Points = new ReferencePointArray();
            side2Points.Append(points.get_Item(points.Size - 1));
            side2Points.Append(endBaseReferencePoint);
            var sideLine2 = doc.FamilyCreate.NewCurveByPoints(side2Points);

            var baseSidePoints = new ReferencePointArray();
            baseSidePoints.Append(secondBaseReferencePoint);
            baseSidePoints.Append(endBaseReferencePoint);
            var baseLine = doc.FamilyCreate.NewCurveByPoints(baseSidePoints);
        }

        /* Пересечение линии и плоскости
        * (преобразует линию в вектор, поэтому пересекает любую линию не параллельную плоскости)
        */
        private XYZ LinePlaneIntersection(Line line, Plane plane, out double lineParameter)
        {
            XYZ planePoint = plane.Origin;
            XYZ planeNormal = plane.Normal;
            XYZ linePoint = line.GetEndPoint(0);

            XYZ lineDirection = (line.GetEndPoint(1) - linePoint).Normalize();

            // Проверка на параллельность линии и плоскости
            if ((planeNormal.DotProduct(lineDirection)) == 0)
            {
                lineParameter = double.NaN;
                return null;
            }

            lineParameter = (planeNormal.DotProduct(planePoint)
              - planeNormal.DotProduct(linePoint))
                / planeNormal.DotProduct(lineDirection);

            return linePoint + lineParameter * lineDirection;
        }
    }
}
