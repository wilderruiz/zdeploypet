using System.Windows;
using System.Windows.Media;

namespace ZDeployPet.App;

internal static class ProductIconFactory
{
    private static readonly ImageSource CachedIcon = BuildIcon();

    public static ImageSource CreateWindowIcon() => CachedIcon;

    private static ImageSource BuildIcon()
    {
        DrawingGroup group = new();

        Brush red = new SolidColorBrush(Color.FromRgb(236, 74, 87));
        Brush shell = new SolidColorBrush(Color.FromRgb(24, 27, 32));
        Brush white = Brushes.White;
        Pen outline = new(new SolidColorBrush(Color.FromRgb(88, 96, 110)), 1.5);

        group.Children.Add(new GeometryDrawing(
            red,
            null,
            new RectangleGeometry(new Rect(0, 0, 64, 64), 12, 12)));

        group.Children.Add(new GeometryDrawing(
            shell,
            outline,
            CreatePolygon(new Point(16, 24), new Point(22, 11), new Point(29, 25))));
        group.Children.Add(new GeometryDrawing(
            shell,
            outline,
            CreatePolygon(new Point(35, 25), new Point(42, 11), new Point(48, 24))));

        group.Children.Add(new GeometryDrawing(
            shell,
            outline,
            new RectangleGeometry(new Rect(14, 20, 36, 31), 13, 13)));

        group.Children.Add(new GeometryDrawing(white, null, new EllipseGeometry(new Point(27, 33), 2.2, 2.2)));
        group.Children.Add(new GeometryDrawing(white, null, new EllipseGeometry(new Point(39, 33), 2.2, 2.2)));
        group.Children.Add(new GeometryDrawing(white, null, new EllipseGeometry(new Point(33, 40), 2.1, 1.6)));

        StreamGeometry mouth = new();
        using (StreamGeometryContext context = mouth.Open())
        {
            context.BeginFigure(new Point(33, 41.5), false, false);
            context.BezierTo(new Point(30.5, 45), new Point(28, 44.5), new Point(27, 43), true, false);
            context.BeginFigure(new Point(33, 41.5), false, false);
            context.BezierTo(new Point(35.5, 45), new Point(38, 44.5), new Point(39, 43), true, false);
        }
        mouth.Freeze();
        group.Children.Add(new GeometryDrawing(null, new Pen(white, 1.5), mouth));

        DrawingImage image = new(group);
        image.Freeze();
        return image;
    }

    private static StreamGeometry CreatePolygon(params Point[] points)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext context = geometry.Open();
        context.BeginFigure(points[0], true, true);
        if (points.Length > 1)
            context.PolyLineTo(points.AsSpan(1).ToArray(), true, false);
        geometry.Freeze();
        return geometry;
    }
}
