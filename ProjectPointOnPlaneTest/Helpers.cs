using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace ProjectPointOnPlaneTest
{
    public static class Helpers
    {
        #region Project point on plane

        // http://thebuildingcoder.typepad.com/blog/2014/09/planes-projections-and-picking-points.html
        /// <summary>
        /// Return signed distance from plane to a given point.
        /// </summary>
        public static double SignedDistanceTo(
            this Plane plane,
            XYZ p)
        {
            Debug.Assert(
                IsEqual(plane.Normal.GetLength(), 1),
                "expected normalised plane normal");

            XYZ v = p - plane.Origin;
            return plane.Normal.DotProduct(v);
        }
        /// <summary>
        /// Project given 3D XYZ point onto plane.
        /// </summary>
        public static XYZ ProjectOntoWithMinus(
            this Plane plane,
            XYZ p)
        {
            double d = plane.SignedDistanceTo(p);

            XYZ q = p - d * plane.Normal;

            Debug.Assert(
                IsZero(plane.SignedDistanceTo(q)),
                "WITH MINUS: expected point on plane to have zero distance to plane");

            return q;
        }
        public static XYZ ProjectOntoWithPlus(
            this Plane plane,
            XYZ p)
        {
            double d = plane.SignedDistanceTo(p);

            XYZ q = p + d * plane.Normal;

            Debug.Assert(
                IsZero(plane.SignedDistanceTo(q)),
                "WITH PLUS: expected point on plane to have zero distance to plane");

            return q;
        }
        public static bool IsZero(
            double a,
            double tolerance)
        {
            return tolerance > Math.Abs(a);
        }
        public static bool IsZero(double a)
        {
            return IsZero(a, Eps);
        }

        public static bool IsEqual(double a, double b)
        {
            return IsZero(b - a);
        }
        private const double Eps = 1.0e-9;

        #endregion

        /// <summary>Retrieve all faces belonging to the specified opening in the given wall</summary>
        public static List<Face> GetWallOpeningFaces(
            Wall wall,
            ElementId openingId)
        {
            List<Face> faceList = new List<Face>();
            List<Solid> solidList = new List<Solid>();
            Options geomOptions = new Options();
            //geomOptions.ComputeReferences = true; // expensive, avoid if not needed
            //geomOptions.DetailLevel = ViewDetailLevel.Fine;
            //geomOptions.IncludeNonVisibleObjects = false;
            GeometryElement geoElem = wall.get_Geometry(geomOptions);
            if (geoElem != null)
                foreach (GeometryObject geomObj in geoElem)
                    if (geomObj is Solid)
                        solidList.Add(geomObj as Solid);

            foreach (Solid solid in solidList)
            foreach (Face face in solid.Faces)
                if (wall.GetGeneratingElementIds(face).Any(x => x == openingId))
                    faceList.Add(face);

            return faceList;
        }
        /// <summary>Получение наружного Face для стены</summary>
        /// <param name="wall">Стена</param>
        /// <param name="shellLayerType">Тип получаемого Face</param>
        /// <returns></returns>
        public static Face GetSideFaceFromWall(Wall wall, ShellLayerType shellLayerType)
        {
            Face face = null;
            IList<Reference> sideFaces = null;
            if (shellLayerType == ShellLayerType.Exterior)
            {
                sideFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
            }
            if (shellLayerType == ShellLayerType.Interior)
            {
                sideFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Interior);
            }
            if (sideFaces != null)
            {
                face = wall.GetGeometryObjectFromReference(sideFaces[0]) as Face;
            }
            return face;
        }
    }
}
