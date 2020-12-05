namespace ModPlus_Revit.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Autodesk.Revit.DB;
    using JetBrains.Annotations;

    /// <summary>
    /// Вспомогательные утилиты работы с геометрией
    /// </summary>
    [PublicAPI]
    public static class GeometryUtils
    {
        private const double Tolerance = 0.0001;

        /// <summary>
        /// Создание отрезка с учетом ограничений
        /// </summary>
        /// <param name="firstPoint">Первая точка</param>
        /// <param name="secondPoint">Вторая точка</param>
        /// <returns>Экземпляр <see cref="Line"/> или null</returns>
        [CanBeNull]
        public static Line TryCreateLine(XYZ firstPoint, XYZ secondPoint)
        {
            if (firstPoint.DistanceTo(secondPoint) < 1.MmToFt())
                return null;
            return Line.CreateBound(firstPoint, secondPoint);
        }

        /// <summary>
        /// Объединить прямые линии в <see cref="CurveLoop"/>
        /// </summary>
        /// <param name="curveLoop"><see cref="CurveLoop"/></param>
        public static CurveLoop MergeStraightLines(CurveLoop curveLoop)
        {
            return CurveLoop.Create(MergeStraightLines(curveLoop.ToList(), true));
        }

        /// <summary>
        /// Объединить прямые линии в коллекции кривых
        /// </summary>
        /// <param name="curveLoop">Коллекция кривых</param>
        /// <param name="orient">Ориентировать коллекцию кривых</param>
        public static List<Curve> MergeStraightLines(List<Curve> curveLoop, bool orient = false)
        {
            var curves = new List<Curve>();
            
            foreach (var curve in curveLoop)
            {
                if (curves.Count == 0)
                {
                    curves.Add(curve);
                }
                else
                {
                    if (curves.Last() is Line line && curve is Line currentLine && line.NeedMerge(currentLine))
                    {
                        var points = GetFurthestPoints(line, currentLine);
                        curves[curves.Count - 1] = Line.CreateBound(points.Item1, points.Item2);
                    }
                    else
                    {
                        curves.Add(curve);
                    }
                }
            }

            if (orient)
                curves.Orient();
            
            return curves;
        }

        /// <summary>
        /// Возвращает пару наиболее удаленных друг от друга точек двух кривых
        /// </summary>
        /// <param name="curve1">Первая кривая</param>
        /// <param name="curve2">Вторя кривая</param>
        public static Tuple<XYZ, XYZ> GetFurthestPoints(Curve curve1, Curve curve2)
        {
            var points = new List<XYZ>();
            points.AddRange(curve1.Tessellate());
            points.AddRange(curve2.Tessellate());
            return points.GetFurthestPoints();
        }

        /// <summary>
        /// Возвращает пару наиболее удаленных друг от друга точек
        /// </summary>
        /// <param name="points">Коллекция точек</param>
        public static Tuple<XYZ, XYZ> GetFurthestPoints(this IList<XYZ> points)
        {
            Tuple<XYZ, XYZ> result = default;
            var dist = double.NaN;
            for (var i = 0; i < points.Count; i++)
            {
                var pt1 = points[i];
                for (var j = 0; j < points.Count; j++)
                {
                    if (i == j)
                        continue;
                    var pt2 = points[j];
                    var d = pt1.DistanceTo(pt2);
                    if (double.IsNaN(dist) || d > dist)
                    {
                        result = new Tuple<XYZ, XYZ>(pt1, pt2);
                        dist = d;
                    }
                }
            }

            return result;
        }
        
        /// <summary>
        /// Создание нового отрезка, на основе данного, с уменьшением длины с двух концов на указанное значение
        /// </summary>
        /// <param name="line">Исходный отрезок</param>
        /// <param name="reduce">Расстояние уменьшения каждого конца, футы</param>
        /// <returns>Null - если невозможно уменьшить отрезок на данное значение</returns>
        [CanBeNull]
        public static Line GetReduced(this Line line, double reduce)
        {
            if (line.Length <= reduce * 2)
                return null;
            var p1 = line.GetEndPoint(0);
            var p2 = line.GetEndPoint(1);
            var v = (p2 - p1).Normalize();
            var newP1 = p1 + (v * reduce);
            var newP2 = p2 + (v.Negate() * reduce);

            return TryCreateLine(newP1, newP2);
        }

        /// <summary>
        /// Создание нового отрезка, на основе данного, с увеличением длины двух концов на указанное значение
        /// </summary>
        /// <param name="line">Исходный отрезок</param>
        /// <param name="extend">Расстояние увеличения каждого конца, футы</param>
        [CanBeNull]
        public static Line GetExtended(this Line line, double extend)
        {
            var p1 = line.GetEndPoint(0);
            var p2 = line.GetEndPoint(1);
            var v = (p2 - p1).Normalize();
            var newP1 = p1 + (v.Negate() * extend);
            var newP2 = p2 + (v * extend);

            return TryCreateLine(newP1, newP2);
        }

        /// <summary>
        /// Создание новой кривой, с обратным порядком концевых точек текущей кривой
        /// </summary>
        /// <param name="curve">Исходная кривая</param>
        public static Curve GetSwapped(this Curve curve)
        {
            if (curve is Line line)
                return GetSwapped(line);
            if (curve is Arc arc)
                return GetSwapped(arc);

            throw new ArgumentOutOfRangeException(nameof(curve), $"The type {curve.GetType().Name} not supported");
        }

        /// <summary>
        /// Создание нового отрезка, с обратным порядком концевых точек текущего отрезка
        /// </summary>
        /// <param name="line">Исходный отрезок</param>
        public static Line GetSwapped(this Line line)
        {
            var p1 = line.GetEndPoint(0);
            var p2 = line.GetEndPoint(1);
            return Line.CreateBound(p2, p1);
        }

        /// <summary>
        /// Создание новой дуги, с обратным порядком концевых точек текущей дуги
        /// </summary>
        /// <param name="arc">Исходная дуга</param>
        public static Arc GetSwapped(this Arc arc)
        {
            var p1 = arc.GetEndPoint(0);
            var p2 = arc.GetEndPoint(1);
            var pOn = arc.Evaluate(0.5, true);
            return Arc.Create(p2, p1, pOn);
        }

        /// <summary>
        /// Является ли указанная точка концевой для линии
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="point">The point.</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static bool IsEndPoint(this Line line, XYZ point, double tolerance = Tolerance)
        {
            if (Math.Abs(line.GetEndPoint(0).DistanceTo(point)) < tolerance)
                return true;
            if (Math.Abs(line.GetEndPoint(1).DistanceTo(point)) < tolerance)
                return true;

            return false;
        }

        /// <summary>
        /// Возвращает концевую точку, расположенную ближе к указанной точке
        /// </summary>
        /// <param name="line">Отрезок</param>
        /// <param name="pt">Точка</param>
        public static XYZ NearestEndTo(this Line line, XYZ pt)
        {
            var end0 = line.GetEndPoint(0);
            var end1 = line.GetEndPoint(1);
            return end0.DistanceTo(pt) <= end1.DistanceTo(pt) ? end0 : end1;
        }

        /// <summary>
        /// Проверка наличия точки в списке через сверку расстояния
        /// </summary>
        /// <param name="points">Список точек</param>
        /// <param name="point">Проверяемая точка</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static bool HasSimilarPoint(this List<XYZ> points, XYZ point, double tolerance = Tolerance)
        {
            foreach (var xyz in points)
            {
                if (Math.Abs(xyz.DistanceTo(point)) < tolerance)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Проверка что текущая кривая и проверяемая кривая имею одну общую концевую точку
        /// </summary>
        /// <param name="firstCurve">Текущая кривая</param>
        /// <param name="secondCurve">Проверяемая кривая</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static bool HasSameEndPoint(this Curve firstCurve, Curve secondCurve, double tolerance = Tolerance)
        {
            if (firstCurve.GetEndPoint(0).DistanceTo(secondCurve.GetEndPoint(0)) < tolerance ||
                firstCurve.GetEndPoint(1).DistanceTo(secondCurve.GetEndPoint(0)) < tolerance ||
                firstCurve.GetEndPoint(0).DistanceTo(secondCurve.GetEndPoint(1)) < tolerance ||
                firstCurve.GetEndPoint(1).DistanceTo(secondCurve.GetEndPoint(1)) < tolerance)
                return true;
            return false;
        }

        /// <summary>
        /// Средняя точка линии
        /// </summary>
        /// <param name="line">The line.</param>
        public static XYZ GetCenterPoint(this Line line)
        {
            return line.Evaluate(0.5, true);
        }

        /// <summary>
        /// Проверка параллельности двух отрезков
        /// </summary>
        /// <param name="line">Первый отрезок</param>
        /// <param name="checkedLine">Второй отрезок</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static bool IsParallelTo(this Line line, Line checkedLine, double tolerance = Tolerance)
        {
            return IsParallelTo(line.Direction, checkedLine.Direction, tolerance);
        }

        /// <summary>
        /// Проверка параллельности отрезка и вектора
        /// </summary>
        /// <param name="line">Отрезок</param>
        /// <param name="checkedVector">Вектор</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static bool IsParallelTo(this Line line, XYZ checkedVector, double tolerance = Tolerance)
        {
            return IsParallelTo(line.Direction, checkedVector, tolerance);
        }

        /// <summary>
        /// Проверка параллельности двух векторов
        /// </summary>
        /// <param name="vector">Первый вектор</param>
        /// <param name="checkedVector">Второй вектор</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static bool IsParallelTo(this XYZ vector, XYZ checkedVector, double tolerance = Tolerance)
        {
            return Math.Abs(Math.Abs(vector.DotProduct(checkedVector)) - 1.0) < tolerance;
        }

        /// <summary>
        /// Лежат ли текущий и проверяемый отрезок на одной прямой. 
        /// </summary>
        /// <param name="firstLine">Текущий отрезок</param>
        /// <param name="secondLine">Проверяемый отрезок</param>
        /// <param name="tolerance">Допуск на сравнение чисел</param>
        public static bool IsLieOnSameStraightLine(this Line firstLine, Line secondLine, double tolerance = Tolerance)
        {
            // Два отрезка лежат на одной прямой если каждый единичный вектор, построенный через любую пару
            // концевых точек двух отрезков, коллинеарен единичному вектору направления одного из отрезков 

            if (!IsParallelTo(firstLine, secondLine))
                return false;

            var firstLinePoints = firstLine.Tessellate();
            var secondLinePoints = secondLine.Tessellate();
            var fv = firstLine.Direction;
            foreach (var firstLinePoint in firstLinePoints)
            {
                foreach (var secondLinePoint in secondLinePoints)
                {
                    if (Math.Abs(firstLinePoint.DistanceTo(secondLinePoint)) < tolerance)
                        continue;
                    var v = (secondLinePoint - firstLinePoint).Normalize();
                    if (!IsParallelTo(fv, v))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Возвращает 3D точку <see cref="XYZ"/>, спроецированную на плоскость <see cref="Plane"/> 
        /// </summary>
        /// <remarks>
        /// http://thebuildingcoder.typepad.com/blog/2014/09/planes-projections-and-picking-points.html
        /// </remarks>
        /// <param name="plane">The plane.</param>
        /// <param name="p">The point.</param>
        public static XYZ ProjectOnto(this Plane plane, XYZ p)
        {
            var d = plane.SignedDistanceTo(p);

            var q = p - (d * plane.Normal);

            Debug.Assert(
                IsZero(plane.SignedDistanceTo(q)),
                "expected point on plane to have zero distance to plane");

            return q;
        }

        /// <summary>
        /// Возвращает отрезок, спроецированный на плоскость
        /// </summary>
        /// <param name="plane">Плоскость</param>
        /// <param name="line">Исходный отрезок</param>
        public static Line ProjectOnto(this Plane plane, Line line)
        {
            return Line.CreateBound(
                ProjectOnto(plane, line.GetEndPoint(0)),
                ProjectOnto(plane, line.GetEndPoint(1)));
        }

        /// <summary>
        /// Получите ориентированный и непрерывный список кривых
        /// </summary>
        /// <remarks>
        /// http://thebuildingcoder.typepad.com/blog/2013/03/sort-and-orient-curves-to-form-a-contiguous-loop.html
        /// </remarks>
        /// <param name="curves">Коллекция кривых</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        [Obsolete("Use Orient method")]
        public static void OrientAndContiguous(ref List<Curve> curves, double tolerance = Tolerance)
        {
            var n = curves.Count;
            for (var i = 0; i < n; i++)
            {
                var curve = curves[i];
                var endPoint = curve.GetEndPoint(1);

                // Find curve with start point = end point
                var found = i + 1 >= n;
                for (var j = i + 1; j < n; ++j)
                {
                    var p = curves[j].GetEndPoint(0);

                    // If there is a match end->start, 
                    // this is the next curve
                    if (p.DistanceTo(endPoint) < tolerance)
                    {
                        if (i + 1 != j)
                        {
                            var tmp = curves[i + 1];
                            curves[i + 1] = curves[j];
                            curves[j] = tmp;
                        }

                        found = true;
                        break;
                    }

                    p = curves[j].GetEndPoint(1);

                    // If there is a match end->end, 
                    // reverse the next curve
                    if (p.DistanceTo(endPoint) < tolerance)
                    {
                        if (i + 1 == j)
                        {
                            curves[i + 1] = GetSwapped(curves[j]);
                        }
                        else
                        {
                            var tmp = curves[i + 1];
                            curves[i + 1] = GetSwapped(curves[j]);
                            curves[j] = tmp;
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception("SortCurvesContiguous:" + " non-contiguous input curves");
                }
            }
        }

        /// <summary>
        /// Ориентирование коллекции кривых
        /// </summary>
        /// <remarks>
        /// http://thebuildingcoder.typepad.com/blog/2013/03/sort-and-orient-curves-to-form-a-contiguous-loop.html
        /// </remarks>
        /// <param name="curves">Коллекция кривых</param>
        /// <param name="tolerance">Допуск расстояния при проверке</param>
        public static void Orient(this List<Curve> curves, double tolerance = Tolerance)
        {
            var n = curves.Count;
            for (var i = 0; i < n; i++)
            {
                var curve = curves[i];
                var endPoint = curve.GetEndPoint(1);

                // Find curve with start point = end point
                var found = i + 1 >= n;
                for (var j = i + 1; j < n; ++j)
                {
                    var p = curves[j].GetEndPoint(0);

                    // If there is a match end->start, this is the next curve
                    if (p.DistanceTo(endPoint) < tolerance)
                    {
                        if (i + 1 != j)
                        {
                            var tmp = curves[i + 1];
                            curves[i + 1] = curves[j];
                            curves[j] = tmp;
                        }

                        found = true;
                        break;
                    }

                    p = curves[j].GetEndPoint(1);

                    // If there is a match end->end, reverse the next curve
                    if (p.DistanceTo(endPoint) < tolerance)
                    {
                        if (i + 1 == j)
                        {
                            curves[i + 1] = GetSwapped(curves[j]);
                        }
                        else
                        {
                            var tmp = curves[i + 1];
                            curves[i + 1] = GetSwapped(curves[j]);
                            curves[j] = tmp;
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception("SortCurvesContiguous: non-contiguous input curves");
                }
            }
        }

        /// <summary>
        /// Получение наружного <see cref="Face"/> для стены
        /// </summary>
        /// <param name="wall">Стена</param>
        /// <param name="shellLayerType">Тип получаемого Face</param>
        public static Face GetSideFaceFromWall(this Wall wall, ShellLayerType shellLayerType)
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

        private static double SignedDistanceTo(this Plane plane, XYZ p)
        {
            Debug.Assert(
                IsEqual(plane.Normal.GetLength(), 1),
                "expected normalized plane normal");

            var v = p - plane.Origin;
            return plane.Normal.DotProduct(v);
        }

        private static bool IsZero(double a, double tolerance = 1.0e-9)
        {
            return tolerance > Math.Abs(a);
        }

        private static bool IsEqual(double a, double b)
        {
            return IsZero(b - a);
        }

        private static bool NeedMerge(this Line line, Line otherLine)
        {
            return IsLieOnSameStraightLine(line, otherLine) &&
                   HasSameEndPoint(line, otherLine);
        }
    }
}
