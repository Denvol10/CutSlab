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

        #region Начальный профиль тела выдавливания
        private ReferenceArray _startMainProfile;
        public ReferenceArray StartMainProfile
        {
            get => _startMainProfile;
            set => _startMainProfile = value;
        }
        #endregion

        #region Конечный профиль тела выдавливания
        private ReferenceArray _endMainProfile;
        public ReferenceArray EndMainProfile
        {
            get => _endMainProfile;
            set => _endMainProfile = value;
        }
        #endregion

        #region Начальный профиль промежуточного тела
        private ReferenceArray _startTransitProfile;
        public ReferenceArray StartTransitProfile
        {
            get => _startTransitProfile;
            set => _startTransitProfile = value;
        }
        #endregion

        #region Конечный профиль промежуточного тела
        private ReferenceArray _endTransitProfile;
        public ReferenceArray EndTransitProfile
        {
            get => _endTransitProfile;
            set => _endTransitProfile = value;
        }
        #endregion

        public CuttingSolid() 
        {
            BeamToplines = new List<DirectShape>();
            BoundLines = new List<ModelLine>();
        }

        public void CreateCutSolidForm(Document doc)
        {
            Plane startPlane = GetPlaneByLine(BoundLines.First());
            Plane endPlane = GetPlaneByLine(BoundLines.Last());

            var topLines = BeamToplines.Select(d => d.get_Geometry(new Options())).Select(g => g.First() as Line);
            var startPoints = topLines.Select(l => LinePlaneIntersection(l, startPlane, out _));
            var endPoints = topLines.Select(l => LinePlaneIntersection(l, endPlane, out _));

            using (Transaction trans = new Transaction(doc, "Create Form Under Beams"))
            {
                trans.Start();
                ReferencePointArray refStartPoints = new ReferencePointArray();
                ReferenceArrayArray profileArray = new ReferenceArrayArray();

                foreach (var point in startPoints)
                {
                    refStartPoints.Append(doc.FamilyCreate.NewReferencePoint(point));
                }

                bool isSortedStartPoints = CurveByPoints.SortPoints(refStartPoints);
                if (isSortedStartPoints)
                {
                    StartMainProfile = CreateMainProfileByPoints(doc, refStartPoints);
                    StartTransitProfile = CreateTransitProfileByPoints(doc, refStartPoints);
                    profileArray.Append(StartMainProfile);
                }

                ReferencePointArray refEndPoints = new ReferencePointArray();
                foreach (var point in endPoints)
                {
                    refEndPoints.Append(doc.FamilyCreate.NewReferencePoint(point));
                }

                bool isSortedEndPoints = CurveByPoints.SortPoints(refEndPoints);
                if (isSortedEndPoints)
                {
                    EndMainProfile = CreateMainProfileByPoints(doc, refEndPoints);
                    EndTransitProfile = CreateTransitProfileByPoints(doc, refEndPoints);
                    profileArray.Append(EndMainProfile);
                }

                var loftForm = doc.FamilyCreate.NewLoftForm(false, profileArray);

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

        private ReferenceArray CreateMainProfileByPoints(Document doc, ReferencePointArray points)
        {
            var referenceArray = new ReferenceArray();

            for (int i = 0; i < points.Size - 1; i ++)
            {
                var referencePointsArray = new ReferencePointArray();
                referencePointsArray.Append(points.get_Item(i));
                referencePointsArray.Append(points.get_Item(i + 1));

                var topLine = doc.FamilyCreate.NewCurveByPoints(referencePointsArray);

                referenceArray.Append(topLine.GeometryCurve.Reference);
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
            referenceArray.Append(sideLine1.GeometryCurve.Reference);

            var side2Points = new ReferencePointArray();
            side2Points.Append(points.get_Item(points.Size - 1));
            side2Points.Append(endBaseReferencePoint);
            var sideLine2 = doc.FamilyCreate.NewCurveByPoints(side2Points);
            referenceArray.Append(sideLine2.GeometryCurve.Reference);

            var baseSidePoints = new ReferencePointArray();
            baseSidePoints.Append(secondBaseReferencePoint);
            baseSidePoints.Append(endBaseReferencePoint);
            var baseLine = doc.FamilyCreate.NewCurveByPoints(baseSidePoints);
            referenceArray.Append(baseLine.GeometryCurve.Reference);

            return referenceArray;
        }

        private ReferenceArray CreateTransitProfileByPoints(Document doc, ReferencePointArray points)
        {
            var referenceArray = new ReferenceArray();

            XYZ firstPoint = points.get_Item(0).Position;
            XYZ fourthPoint = points.get_Item(points.Size - 1).Position;
            XYZ firstBasePoint = new XYZ(firstPoint.X, firstPoint.Y, 0);
            XYZ fourthBasePoint = new XYZ(fourthPoint.X, fourthPoint.Y, 0);

            for (int i = 0; i < points.Size - 1; i++)
            {
                var referencePointsArray = new ReferencePointArray();

                XYZ point1 = points.get_Item(i).Position;
                XYZ point2 = points.get_Item(i + 1).Position;

                var refPoint1 = doc.FamilyCreate.NewReferencePoint(point1);
                var refPoint2 = doc.FamilyCreate.NewReferencePoint(point2);

                referencePointsArray.Append(refPoint1);
                referencePointsArray.Append(refPoint2);

                var topLine = doc.FamilyCreate.NewCurveByPoints(referencePointsArray);

                referenceArray.Append(topLine.GeometryCurve.Reference);
            }

            var firstRefPoint = doc.FamilyCreate.NewReferencePoint(firstPoint);
            var fourthRefPoint = doc.FamilyCreate.NewReferencePoint(fourthPoint);
            var firstBaseRefPoint = doc.FamilyCreate.NewReferencePoint(firstBasePoint);
            var fourthBaseRefPoint = doc.FamilyCreate.NewReferencePoint(fourthBasePoint);

            var side1Points = new ReferencePointArray();
            side1Points.Append(firstRefPoint);
            side1Points.Append(firstBaseRefPoint);
            var side1Line = doc.FamilyCreate.NewCurveByPoints(side1Points);
            referenceArray.Append(side1Line.GeometryCurve.Reference);

            var side2Points = new ReferencePointArray();
            side2Points.Append(fourthRefPoint);
            side2Points.Append(fourthBaseRefPoint);
            var side2Line = doc.FamilyCreate.NewCurveByPoints(side2Points);
            referenceArray.Append(side2Line.GeometryCurve.Reference);

            var basePoints = new ReferencePointArray();
            basePoints.Append(firstBaseRefPoint);
            basePoints.Append(fourthBaseRefPoint);
            var baseLine = doc.FamilyCreate.NewCurveByPoints(basePoints);
            referenceArray.Append(baseLine.GeometryCurve.Reference);

            return referenceArray;
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
