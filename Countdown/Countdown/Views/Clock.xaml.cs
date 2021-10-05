using System;
using System.Numerics;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;

using Windows.Foundation;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    internal sealed partial class Clock : UserControl
    {
        private readonly Compositor compositor;
        private readonly ContainerVisual containerVisual;
        private Size previousSize = Size.Empty;

        public Clock()
        {
            this.InitializeComponent();

            compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            containerVisual = compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(this, containerVisual);

            SizeChanged += (s, e) =>
            {
                if (e.NewSize != previousSize)
                {
                    previousSize = e.NewSize;

                    Clock clock = (Clock)s;
                    clock.containerVisual.Children.RemoveAll();

                    clock.InitialiseDrawingParams();
                    clock.CreateFace();
                    clock.CreateTickTrail();
                    clock.CreateFaceTickMarks();
                    clock.CreateHand();
                }
            };
        }


        private const float cOuterFrameStrokePercentage = 0.01f;
        private const float cInnerFrameStrokePercentage = 0.02f;

        private const float cTickMarksStrokePercentage = 0.01f;
        private const float cTickMarksOuterRadiusPercentage = 0.83f;
        private const float cTickMarksInnerRadiusPercentage = 0.74f;

        private const float cHandStrokePercentage = 0.01f;
        private const float cHandTipRadiusPercentage = 0.90f;
        private const double cHandSectorAngle = 25.0;
        private const float cHandArcRadiusPercentage = 0.055f;

        private const float cTickTrailOuterRadiusPercent = 0.92f;
        private const float cTickTrailInnerRadiusPercent = 0.38f;

        private const string TrickTrailComment = "t";
        private const string SecondHandComment = "s";

        // TODO: dark mode?
        private static readonly Color OuterFrameColour = Colors.LightGray;
        private static readonly Color InnerFrameColour = Color.FromArgb(0xFF, 0x00, 0x68, 0xC7);
        private static readonly Color TickMarksColour = Colors.DarkGray;
        private static readonly Color FaceColour = Colors.Ivory;
        private static readonly Color TickTrailColour = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xD2);
        private static readonly Color HandFillColour = Color.FromArgb(0xFF, 0x00, 0x8B, 0xCE);
        private static readonly Color HandStrokeColour = Colors.Gray;


        private float ClockSize { get; set; }
        private Vector2 Center { get; set; }
        private Vector2 DropShadowOffset { get; set; }
        private bool IsSmallClock { get; set; }


        public long Ticks
        {
            get { return (long)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        public static readonly DependencyProperty TicksProperty =
                DependencyProperty.Register(nameof(Ticks),
                typeof(long),
                typeof(Clock),
                new PropertyMetadata(0L, OnTicksPropertyChanged));

        private static void OnTicksPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Clock clock = (Clock)d;
            clock.UpdateSecondHandAndTickTrail();
        }

        private int CalculateVisibleTickTrailSegmentCount()
        {
            return (int)((Ticks + (TimeSpan.TicksPerSecond / 12)) / TimeSpan.TicksPerSecond);
        }

        private void UpdateSecondHandAndTickTrail()
        {
            try
            {
                long segments = CalculateVisibleTickTrailSegmentCount();
                int index = 0;

                foreach (Visual visual in containerVisual.Children)
                {
                    if (visual.Comment == TrickTrailComment)
                        visual.IsVisible = index++ < segments;
                    else if (visual.Comment == SecondHandComment)
                        visual.RotationAngleInDegrees = CalculateSecondHandAngle();
                }
            }
            catch
            {
                // If the application is closed while the animation is running, WinRT will dispose of the 
                // local copies of composition objects even if I'm holding references to them. Hence an
                // object disposed exception. Not a big deal, we're bailing... 
            }
        }

        private void InitialiseDrawingParams()
        {
            IsSmallClock = ActualSize.Y < 300;
            DropShadowOffset = new Vector2(ActualSize.Y / 200.0f);
            ClockSize = ActualSize.Y * 0.95f;
            Center = new Vector2(ActualSize.X * 0.5f, ActualSize.Y * 0.5f);
        }

        private void CreateTickTrail()
        {
            float radius = ClockSize * 0.5f;
            float outerRadius = radius * cTickTrailOuterRadiusPercent;
            float innerRadius = radius * cTickTrailInnerRadiusPercent;

            const double startAngle = 179.5;
            const double endAngle = 174.5;

            Vector topLeft = new Vector(outerRadius, startAngle, Center);
            Vector topRight = new Vector(outerRadius, endAngle, Center);
            Vector bottomLeft = new Vector(innerRadius, startAngle, Center);
            Vector bottomRight = new Vector(innerRadius, endAngle, Center);

            using CanvasPathBuilder builder = new CanvasPathBuilder(null);

            builder.BeginFigure(topLeft.Cartesian);

            if (IsSmallClock)
                builder.AddLine(topRight.Cartesian);
            else
                builder.AddArc(topRight.Cartesian, outerRadius, outerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            builder.AddLine(bottomRight.Cartesian);
            builder.AddLine(bottomLeft.Cartesian);
            builder.EndFigure(CanvasFigureLoop.Closed);

            // create a composition geometry from the canvas path data
            using CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
            CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
            pathGeometry.Path = new CompositionPath(canvasGeometry);

            CompositionBrush brush = compositor.CreateColorBrush(TickTrailColour);
            long visibleSegments = CalculateVisibleTickTrailSegmentCount();

            // create every trail element visual, then change its visibility as required
            for (int segment = 0; segment < 30; segment++)
            {
                // create a shape from the geometry
                CompositionSpriteShape tickSegment = compositor.CreateSpriteShape(pathGeometry);
                tickSegment.FillBrush = brush;
                tickSegment.CenterPoint = Center;
                tickSegment.RotationAngleInDegrees = segment * 6f;  // one second is 6 degrees

                // create a visual for the shape
                ShapeVisual shapeVisual = compositor.CreateShapeVisual();
                shapeVisual.Size = ActualSize;
                shapeVisual.Shapes.Add(tickSegment);
                shapeVisual.IsVisible = segment < visibleSegments;
                shapeVisual.Comment = TrickTrailComment; // used to identify this shape

                containerVisual.Children.InsertAtTop(shapeVisual);
            }
        }



        private void CreateFace()
        {
            CompositionSpriteShape CreateCircle(double radius, float stroke, Vector2 offset, Color fillColour, Color strokeColour)
            {
                CompositionEllipseGeometry circleGeometry = compositor.CreateEllipseGeometry();
                circleGeometry.Radius = new Vector2((float)radius);

                CompositionSpriteShape circleShape = compositor.CreateSpriteShape(circleGeometry);
                circleShape.Offset = offset;
                circleShape.FillBrush = compositor.CreateColorBrush(fillColour);

                if (stroke > 0.0f)
                {
                    circleShape.StrokeThickness = stroke;
                    circleShape.StrokeBrush = compositor.CreateColorBrush(strokeColour);
                }

                return circleShape;
            }

            CompositionContainerShape shapeContainer = compositor.CreateContainerShape();

            // outer frame
            double radius = ClockSize * 0.5f;
            float outerFrameStroke = ClockSize * cOuterFrameStrokePercentage;
            radius -= outerFrameStroke * 0.5f;

            shapeContainer.Shapes.Add(CreateCircle(radius, outerFrameStroke, Center, Colors.Transparent, OuterFrameColour));

            // inner frame
            float innerFrameStroke = ClockSize * cInnerFrameStrokePercentage;
            radius -= (outerFrameStroke + innerFrameStroke) * 0.5f;

            shapeContainer.Shapes.Add(CreateCircle(radius, innerFrameStroke, Center, Colors.Transparent, InnerFrameColour));

            // tick marks around inner frame
            double tickRadius = innerFrameStroke / 5.0;      // TODO constants, and color static

            for (double degrees = 0; degrees < 360.0; degrees += 30.0)
                shapeContainer.Shapes.Add(CreateCircle(tickRadius, 0f, new Vector(radius, degrees, Center).Cartesian, Colors.Silver, Colors.Transparent));

            // clock face fill
            radius -= innerFrameStroke * 0.5f;
            shapeContainer.Shapes.Add(CreateCircle(radius, 0f, Center, FaceColour, Colors.Transparent));

            // create a visual for the shape container
            ShapeVisual shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Size = ActualSize;
            shapeVisual.Shapes.Add(shapeContainer);

            // create a surface brush to use as a mask for the drop shadow
            CompositionVisualSurface surface = compositor.CreateVisualSurface();
            surface.SourceSize = ActualSize;
            surface.SourceVisual = shapeVisual;   // TODO it may be quicker to use a simpler shape

            // create the drop shadow
            DropShadow shadow = compositor.CreateDropShadow();
            shadow.Mask = compositor.CreateSurfaceBrush(surface);
            shadow.Offset = new Vector3(DropShadowOffset, 0f);
            shadow.Color = Colors.DimGray;

            // create a visual for the shadow
            SpriteVisual shadowVisual = compositor.CreateSpriteVisual();
            shadowVisual.Size = ActualSize;
            shadowVisual.Shadow = shadow;

            // insert into the tree 
            containerVisual.Children.InsertAtBottom(shapeVisual);
            containerVisual.Children.InsertAtBottom(shadowVisual);
        }

        private void CreateFaceTickMarks()
        {
            CompositionSpriteShape CreateLine(Vector2 start, Vector2 end, float thickness, CompositionColorBrush brush, CompositionStrokeCap endCap)
            {
                CompositionLineGeometry lineGeometry = compositor.CreateLineGeometry();
                lineGeometry.Start = start;
                lineGeometry.End = end;

                CompositionSpriteShape lineShape = compositor.CreateSpriteShape(lineGeometry);
                lineShape.StrokeThickness = thickness;
                lineShape.StrokeBrush = brush;
                lineShape.StrokeEndCap = endCap;
                lineShape.StrokeStartCap = endCap;

                return lineShape;
            }

            CompositionContainerShape shapeContainer = compositor.CreateContainerShape();
            CompositionColorBrush brush = compositor.CreateColorBrush(TickMarksColour);

            // add the 5 second tick marks
            float stroke = ClockSize * cTickMarksStrokePercentage;
            double startLength = ClockSize * 0.5f * cTickMarksInnerRadiusPercentage;
            double endLength = ClockSize * 0.5f * cTickMarksOuterRadiusPercentage;
            CompositionStrokeCap endCap = IsSmallClock ? CompositionStrokeCap.Flat : CompositionStrokeCap.Round;

            for (int degrees = 30; degrees < 360; degrees += 30)
            {
                if (degrees % 90 > 0)
                {
                    Vector2 inner = new Vector(startLength, degrees, Center).Cartesian;
                    Vector2 outer = new Vector(endLength, degrees, Center).Cartesian;
                    shapeContainer.Shapes.Add(CreateLine(inner, outer, stroke, brush, endCap));
                }
            }

            // now the cross hairs
            float radius = (ClockSize * 0.5f) - (ClockSize * (cOuterFrameStrokePercentage + cInnerFrameStrokePercentage));

            // horizontal cross hair
            Vector2 start = new Vector2(Center.X - radius, Center.Y);
            Vector2 end = new Vector2(Center.X + radius, Center.Y);
            shapeContainer.Shapes.Add(CreateLine(start, end, stroke, brush, CompositionStrokeCap.Flat));

            // vertical cross hair
            start = new Vector2(Center.X, Center.Y - radius);
            end = new Vector2(Center.X, Center.Y + radius);
            shapeContainer.Shapes.Add(CreateLine(start, end, stroke, brush, CompositionStrokeCap.Flat));

            // create a visual for the shapes
            ShapeVisual shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Size = ActualSize;
            shapeVisual.Shapes.Add(shapeContainer);

            // insert into tree
            containerVisual.Children.InsertAtTop(shapeVisual);
        }

        private void CreateHand()
        {
            using CanvasPathBuilder builder = new CanvasPathBuilder(null);

            Vector tip = new Vector(ClockSize * 0.5 * cHandTipRadiusPercentage, 0.0, Center);
            builder.BeginFigure(tip.Cartesian);

            float radius = ClockSize * cHandArcRadiusPercentage;

            Vector arcStartPoint = new Vector(radius, -cHandSectorAngle, Center);
            builder.AddLine(arcStartPoint.Cartesian);

            Vector arcEndPoint = new Vector(radius, cHandSectorAngle, Center);
            builder.AddArc(arcEndPoint.Cartesian, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Large);
            builder.EndFigure(CanvasFigureLoop.Closed);

            float handStroke = ClockSize * cHandStrokePercentage;

            // create a composition geometry from the canvas path data
            using CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
            CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
            pathGeometry.Path = new CompositionPath(canvasGeometry);

            // create a shape from the geometry
            CompositionSpriteShape secondHand = compositor.CreateSpriteShape(pathGeometry);
            secondHand.FillBrush = compositor.CreateColorBrush(HandFillColour);
            secondHand.StrokeThickness = handStroke;
            secondHand.StrokeLineJoin = CompositionStrokeLineJoin.Round;
            secondHand.StrokeBrush = compositor.CreateColorBrush(HandStrokeColour);

            // create a visual for the shape
            ShapeVisual shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Size = ActualSize;
            shapeVisual.Shapes.Add(secondHand);
            shapeVisual.CenterPoint = new Vector3(Center, 0f);
            shapeVisual.RotationAngleInDegrees = CalculateSecondHandAngle();
            shapeVisual.Comment = SecondHandComment; // used to identify this shape

            // add to visual tree
            containerVisual.Children.InsertAtTop(shapeVisual);
        }

        private float CalculateSecondHandAngle()
        {
            // one second is 6 degrees
            // zero degrees is at 6 o'clock and sweeps clockwise
            return 180.0f + ((Ticks * 6.0f) / TimeSpan.TicksPerSecond);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Infinity means this is inside a scrollable control, stack panel etc.
            if (double.IsInfinity(availableSize.Height) && double.IsInfinity(availableSize.Width))
            {
                const double cDefaultSize = 250.0;
                availableSize.Width = availableSize.Height = cDefaultSize;
            }
            else
            {
                // The availableSize will already have been adjusted to take account of
                // min and max Size properties
                availableSize.Width = Math.Min(availableSize.Width, availableSize.Height);
                availableSize.Height = availableSize.Width;
            }

            return availableSize;
        }

        private readonly struct Vector
        {
            private readonly double length;
            private readonly double degrees;
            private readonly Vector2 offset;

            public Vector(double length, double degrees, Vector2 offset)
            {
                this.length = length;
                this.degrees = degrees;
                this.offset = offset;
            }

            public Vector2 Cartesian
            {
                get
                {
                    double radians = degrees * (Math.PI / 180.0);
                    return new Vector2((float)Math.FusedMultiplyAdd(length, Math.Sin(radians), offset.X),
                                        (float)Math.FusedMultiplyAdd(length, Math.Cos(radians), offset.Y));
                }
            }
        }
    }
}
