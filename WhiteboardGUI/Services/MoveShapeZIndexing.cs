using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services
{
    public class MoveShapeZIndexing
    {
        ObservableCollection<IShape> Shapes;

        public MoveShapeZIndexing(ObservableCollection<IShape> shapes)
        {
            Shapes = shapes;
        }
        public void MoveShapeBack(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (shape == null || !(bool)Shapes.Any(s => (s as dynamic).ShapeId.Equals(shape.ShapeId))) return;

                Shapes.Remove(Shapes.FirstOrDefault(s => (s as dynamic).ShapeId == shape.ShapeId));
                Shapes.Insert(0, shape);
                UpdateZIndices();
            });
        }

        public void UpdateZIndices()
        {
            for (int i = 0; i < Shapes.Count; i++)
            {
                Shapes[i].ZIndex = i;
            }
        }

        public void MoveShapeBackward(IShape shape)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (shape == null || !(bool)Shapes.Any(s => (s as dynamic).ShapeId.Equals(shape.ShapeId))) return;

                int currentIndex = Shapes.Select((s, index) => new { s, index })
                                 .FirstOrDefault(item => (item.s as dynamic).ShapeId.Equals(shape.ShapeId))?.index ?? -1;
                if (currentIndex <= 0)
                    return; // Already at the bottom

                // Iterate backward to find the first overlapping shape based on stroke
                for (int i = currentIndex - 1; i >= 0; i--)
                {
                    var otherShape = Shapes[i];
                    if (AreShapesOverlapping(shape, otherShape))
                    {
                        // Swap shape with otherShape
                        Shapes.Move(currentIndex, i);
                        UpdateZIndices();
                        break;
                    }
                }
            });
        }

        // Helper method to determine if two shapes' strokes are overlapping
        private bool AreShapesOverlapping(IShape shape1, IShape shape2)
        {
            Geometry strokeGeometry1 = GetShapeStrokeGeometry(shape1);
            Geometry strokeGeometry2 = GetShapeStrokeGeometry(shape2);

            if (strokeGeometry1 == null || strokeGeometry2 == null)
                return false;

            // Combine the two stroke geometries to find their intersection
            // If the intersection is not empty, then the strokes overlap
            var combinedGeometry = Geometry.Combine(
                strokeGeometry1,
                strokeGeometry2,
                GeometryCombineMode.Intersect,
                null);

            return !combinedGeometry.IsEmpty();
        }


        // Helper method to create the Geometry of a shape's stroke
        private Geometry GetShapeStrokeGeometry(IShape shape)
        {
            if (shape == null)
                return null;

            Geometry geometry = null;

            switch (shape)
            {
                case CircleShape circle:
                    {
                        double centerX = circle.Left + circle.Width / 2;
                        double centerY = circle.Top + circle.Height / 2;
                        double radiusX = circle.Width / 2;
                        double radiusY = circle.Height / 2;
                        geometry = new EllipseGeometry(new Point(centerX, centerY), radiusX, radiusY);
                        break;
                    }

                case LineShape line:
                    {
                        geometry = new LineGeometry(new Point(line.StartX, line.StartY), new Point(line.EndX, line.EndY));
                        break;
                    }

                case ScribbleShape scribble:
                    {
                        if (scribble.PointCollection == null || !scribble.PointCollection.Any())
                            return null;

                        StreamGeometry streamGeometry = new StreamGeometry();
                        using (StreamGeometryContext ctx = streamGeometry.Open())
                        {
                            ctx.BeginFigure(scribble.PointCollection.First(), false, false);
                            ctx.PolyLineTo(scribble.PointCollection.Skip(1).ToList(), true, true);
                        }
                        streamGeometry.Freeze(); // Makes the geometry immutable
                        geometry = streamGeometry;
                        break;
                    }

                default:
                    {
                        // Unsupported shape type
                        return null;
                    }
            }

            if (geometry != null)
            {
                try
                {
                    // Create a Pen with the shape's stroke color and thickness
                    Color color = (Color)ColorConverter.ConvertFromString(shape.Color.ToString());
                    Pen pen = new Pen(new SolidColorBrush(color), shape.StrokeThickness);
                    pen.Freeze(); // Makes the pen immutable and improves performance

                    // Use GetWidenedPathGeometry to create a geometry that represents the area covered by the pen's stroke
                    PathGeometry strokeGeometry = geometry.GetWidenedPathGeometry(pen);
                    strokeGeometry.Freeze(); // Optional: Freeze for performance if the geometry won't change

                    return strokeGeometry;
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that might occur during geometry widening
                    Debug.WriteLine($"Error widening geometry: {ex.Message}");
                    return null;
                }
            }

            return null;
        }
    }
}
