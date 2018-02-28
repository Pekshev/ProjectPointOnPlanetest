using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ProjectPointOnPlaneTest
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ProjectWindowPoints : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var selection = commandData.Application.ActiveUIDocument.Selection;
            // you need to select arc-wall with window near Origin
            try
            {
                var selectionResult = selection.PickObject(ObjectType.Element, new WallSelectionFilter(),
                    "pick arc-wall with window");
                var wall = (Wall)doc.GetElement(selectionResult);
                // get wall exterior face
                var wallFace = Helpers.GetSideFaceFromWall(wall, ShellLayerType.Exterior);
                // get wall openings
                var openingsIds = wall.FindInserts(true, false, false, false);
                foreach (ElementId openingsId in openingsIds)
                {
                    // get opening curves on wall's exterior side face
                    List<Curve> curves = new List<Curve>();
                    foreach (Face openingFace in Helpers.GetWallOpeningFaces(wall, openingsId))
                    {
                        foreach (CurveLoop curveLoop in openingFace.GetEdgesAsCurveLoops())
                        {
                            foreach (Curve curve in curveLoop)
                            {
                                if (wallFace.Intersect(curve) == SetComparisonResult.Subset)
                                {
                                    curves.Add(curve);
                                }
                            }
                        }
                    }
                    if (curves.Any())
                    {
                        // project curves onto plane
                        FamilyInstance familyInstance = (FamilyInstance)doc.GetElement(openingsId);
                        Plane plane = Plane.CreateByNormalAndOrigin(familyInstance.FacingOrientation, new XYZ(0, 0, 0));
                        List<Curve> projectedCurvesWithPlus = new List<Curve>();
                        foreach (Curve curve in curves)
                        {
                            XYZ previosPoint = null;
                            for (var i = 0; i < curve.Tessellate().Count; i++)
                            {
                                XYZ xyzProjected = plane.ProjectOntoWithPlus(curve.Tessellate()[i]);
                                if (i == 0)
                                    previosPoint = plane.ProjectOntoWithPlus(xyzProjected);
                                else
                                {
                                    projectedCurvesWithPlus.Add(Line.CreateBound(previosPoint, xyzProjected));
                                    previosPoint = xyzProjected;
                                }
                            }
                        }
                        List<Curve> projectedCurvesWithMinus = new List<Curve>();
                        foreach (Curve curve in curves)
                        {
                            XYZ previosPoint = null;
                            for (var i = 0; i < curve.Tessellate().Count; i++)
                            {
                                XYZ xyzProjected = plane.ProjectOntoWithMinus(curve.Tessellate()[i]);
                                if (i == 0)
                                    previosPoint = plane.ProjectOntoWithMinus(xyzProjected);
                                else
                                {
                                    projectedCurvesWithMinus.Add(Line.CreateBound(previosPoint, xyzProjected));
                                    previosPoint = xyzProjected;
                                }
                            }
                        }
                        // print results
                        foreach (Curve curve in projectedCurvesWithPlus)
                        {
                            Debug.Print("Progected point with Plus sign: " + curve.GetEndPoint(0));
                            Debug.Print("Progected point with Plus sign: " + curve.GetEndPoint(1));
                        }
                        foreach (Curve curve in projectedCurvesWithMinus)
                        {
                            Debug.Print("Progected point with Minus sign: " + curve.GetEndPoint(0));
                            Debug.Print("Progected point with Minus sign: " + curve.GetEndPoint(1));
                        }
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception exception)
            {
                message += exception.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

    }

    internal class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
