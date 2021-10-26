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

using Countdown.ViewModels;

namespace Countdown.Views
{
    internal sealed partial class Clock : UserControl
    {
        private static CompositionClock? sCompositionClock;

        public Clock()
        {
            this.InitializeComponent();

            Loaded += (s, e) =>
            {
                Clock xamlClock = (Clock)s;

                if (sCompositionClock is null)
                    sCompositionClock = new CompositionClock(xamlClock);
                else
                    sCompositionClock.XamlClock = xamlClock;

                ElementCompositionPreview.SetElementChildVisual(xamlClock, sCompositionClock.Visual);
            };
        }


        public StopwatchState State
        {
            get { return (StopwatchState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }


        public static readonly DependencyProperty StateProperty =
                DependencyProperty.Register(nameof(State),
                typeof(StopwatchState),
                typeof(Clock),
                new PropertyMetadata(StopwatchState.Undefined, StatePropertyChanged));


        private static void StatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (sCompositionClock is not null)
            {
                StopwatchState oldValue = (StopwatchState)e.OldValue;

                if (oldValue == StopwatchState.Undefined) // a new page has been loaded
                    return;

                switch ((StopwatchState)e.NewValue)
                {
                    case StopwatchState.Running: sCompositionClock.Animations.StartForwardAnimations(); break;
                    case StopwatchState.Rewinding: sCompositionClock.Animations.StartRewindAnimations(); break;
                    case StopwatchState.Stopped: sCompositionClock.Animations.StopAnimations(); break;

                    case StopwatchState.AtStart:
                    case StopwatchState.Undefined: break;

                    default: throw new InvalidOperationException();
                }
            }
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            return CompositionClock.ContainerSize.ToSize();
        }


        private class CompositionClock
        {
            public ContainerVisual Visual { get; }
            public static Vector2 ContainerSize { get; } = new Vector2(200);
            public AnimationList Animations { get; }
            public Clock XamlClock { get; set; }

            public CompositionClock(Clock xamlClock)
            {
                Compositor compositor = ElementCompositionPreview.GetElementVisual(xamlClock).Compositor;
                Visual = compositor.CreateContainerVisual();
                Animations = new AnimationList(compositor);
                XamlClock = xamlClock;

                // allow room for the drop shadow
                float clockSize = ContainerSize.Y * 0.95f;
                Vector2 center = new Vector2(ContainerSize.X * 0.5f);

                CreateFace(compositor, center, clockSize);
                CreateTickTrail(compositor, center, clockSize);
                CreateFaceTickMarks(compositor, center, clockSize);
                CreateHand(compositor, center, clockSize);
            }


            private const float cOuterFrameStrokePercentage = 0.01f;
            private const float cInnerFrameStrokePercentage = 0.02f;

            private const float cTickMarksStrokePercentage = 0.01f;
            private const float cTickMarksOuterRadiusPercentage = 0.83f;
            private const float cTickMarksInnerRadiusPercentage = 0.74f;

            private const float cHandStrokePercentage = 0.01f;
            private const float cHandTipRadiusPercentage = 0.90f;
            private const float cHandSectorAngle = 25.0f;
            private const float cHandArcRadiusPercentage = 0.055f;

            private const float cTickTrailOuterRadiusPercent = 0.92f;
            private const float cTickTrailInnerRadiusPercent = 0.38f;

            // TODO: dark mode?
            private static readonly Color OuterFrameColour = Colors.LightGray;
            private static readonly Color InnerFrameColour = Color.FromArgb(0xFF, 0x00, 0x68, 0xC7);
            private static readonly Color TickMarksColour = Colors.DarkGray;
            private static readonly Color FaceColour = Colors.Ivory;
            private static readonly Color TickTrailColour = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xD2);
            private static readonly Color HandFillColour = Color.FromArgb(0xFF, 0x00, 0x8B, 0xCE);
            private static readonly Color HandStrokeColour = Colors.Gray;

            private void CreateTickTrail(Compositor compositor, Vector2 center, float clockSize)
            {
                float radius = clockSize * 0.5f;
                float outerRadius = radius * cTickTrailOuterRadiusPercent;
                float innerRadius = radius * cTickTrailInnerRadiusPercent;

                const float startAngle = 179.5f;
                const float endAngle = 174.5f;

                Vector topLeft = new Vector(outerRadius, startAngle, center);
                Vector topRight = new Vector(outerRadius, endAngle, center);
                Vector bottomLeft = new Vector(innerRadius, startAngle, center);
                Vector bottomRight = new Vector(innerRadius, endAngle, center);

                using CanvasPathBuilder builder = new CanvasPathBuilder(null);

                builder.BeginFigure(topLeft.Cartesian);
                builder.AddArc(topRight.Cartesian, outerRadius, outerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

                builder.AddLine(bottomRight.Cartesian);
                builder.AddLine(bottomLeft.Cartesian);
                builder.EndFigure(CanvasFigureLoop.Closed);

                // create a composition geometry from the canvas path data
                using CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
                CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
                pathGeometry.Path = new CompositionPath(canvasGeometry);

                CompositionBrush brush = compositor.CreateColorBrush(TickTrailColour);

                // create every trail element visual, then change its opacity as required
                for (int segment = 0; segment < 30; segment++)
                {
                    // create a shape from the geometry
                    CompositionSpriteShape tickSegment = compositor.CreateSpriteShape(pathGeometry);
                    tickSegment.FillBrush = brush;
                    tickSegment.CenterPoint = center;
                    tickSegment.RotationAngleInDegrees = segment * 6f;  // one second is 6 degrees

                    // create a visual for the shape
                    ShapeVisual shapeVisual = compositor.CreateShapeVisual();
                    shapeVisual.Size = ContainerSize;
                    shapeVisual.Shapes.Add(tickSegment);
                    shapeVisual.Opacity = 0.0f;

                    Animations.AddTickTrailSegment(shapeVisual, segment);

                    Visual.Children.InsertAtTop(shapeVisual);
                }
            }



            private void CreateFace(Compositor compositor, Vector2 center, float clockSize)
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
                float radius = clockSize * 0.5f;
                float outerFrameStroke = clockSize * cOuterFrameStrokePercentage;
                radius -= outerFrameStroke * 0.5f;

                shapeContainer.Shapes.Add(CreateCircle(radius, outerFrameStroke, center, Colors.Transparent, OuterFrameColour));

                // inner frame
                float innerFrameStroke = clockSize * cInnerFrameStrokePercentage;
                radius -= (outerFrameStroke + innerFrameStroke) * 0.5f;

                shapeContainer.Shapes.Add(CreateCircle(radius, innerFrameStroke, center, Colors.Transparent, InnerFrameColour));

                // tick marks around inner frame
                float tickRadius = innerFrameStroke / 5.0f;      // TODO constants, and color static

                for (float degrees = 0; degrees < 360.0; degrees += 30.0f)
                    shapeContainer.Shapes.Add(CreateCircle(tickRadius, 0f, new Vector(radius, degrees, center).Cartesian, Colors.Silver, Colors.Transparent));

                // clock face fill
                radius -= innerFrameStroke * 0.5f;
                shapeContainer.Shapes.Add(CreateCircle(radius, 0f, center, FaceColour, Colors.Transparent));

                // create a visual for the shape container
                ShapeVisual shapeVisual = compositor.CreateShapeVisual();
                shapeVisual.Size = ContainerSize;
                shapeVisual.Shapes.Add(shapeContainer);

                // create a surface brush to use as a mask for the drop shadow
                CompositionVisualSurface surface = compositor.CreateVisualSurface();
                surface.SourceSize = ContainerSize;
                surface.SourceVisual = shapeVisual;   // TODO it may be quicker to use a simpler shape

                // create the drop shadow
                DropShadow shadow = compositor.CreateDropShadow();
                shadow.Mask = compositor.CreateSurfaceBrush(surface);
                shadow.Offset = new Vector3(new Vector2(1.5f), 0f);
                shadow.Color = Colors.DimGray;

                // create a visual for the shadow
                SpriteVisual shadowVisual = compositor.CreateSpriteVisual();
                shadowVisual.Size = ContainerSize;
                shadowVisual.Shadow = shadow;

                // insert into the tree 
                Visual.Children.InsertAtBottom(shapeVisual);
                Visual.Children.InsertAtBottom(shadowVisual);
            }

            private void CreateFaceTickMarks(Compositor compositor, Vector2 center, float clockSize)
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
                float stroke = clockSize * cTickMarksStrokePercentage;
                float startLength = clockSize * 0.5f * cTickMarksInnerRadiusPercentage;
                float endLength = clockSize * 0.5f * cTickMarksOuterRadiusPercentage;
                CompositionStrokeCap endCap = CompositionStrokeCap.Round;

                for (int degrees = 30; degrees < 360; degrees += 30)
                {
                    if (degrees % 90 > 0)
                    {
                        Vector2 inner = new Vector(startLength, degrees, center).Cartesian;
                        Vector2 outer = new Vector(endLength, degrees, center).Cartesian;
                        shapeContainer.Shapes.Add(CreateLine(inner, outer, stroke, brush, endCap));
                    }
                }

                // now the cross hairs
                float radius = (clockSize * 0.5f) - (clockSize * (cOuterFrameStrokePercentage + cInnerFrameStrokePercentage));

                // horizontal cross hair
                Vector2 start = new Vector2(center.X - radius, center.Y);
                Vector2 end = new Vector2(center.X + radius, center.Y);
                shapeContainer.Shapes.Add(CreateLine(start, end, stroke, brush, CompositionStrokeCap.Flat));

                // vertical cross hair
                start = new Vector2(center.X, center.Y - radius);
                end = new Vector2(center.X, center.Y + radius);
                shapeContainer.Shapes.Add(CreateLine(start, end, stroke, brush, CompositionStrokeCap.Flat));

                // create a visual for the shapes
                ShapeVisual shapeVisual = compositor.CreateShapeVisual();
                shapeVisual.Size = ContainerSize;
                shapeVisual.Shapes.Add(shapeContainer);

                // insert into tree
                Visual.Children.InsertAtTop(shapeVisual);
            }

            private void CreateHand(Compositor compositor, Vector2 center, float clockSize)
            {
                using CanvasPathBuilder builder = new CanvasPathBuilder(null);

                Vector tip = new Vector(clockSize * 0.5f * cHandTipRadiusPercentage, 0.0f, center);
                builder.BeginFigure(tip.Cartesian);

                float radius = clockSize * cHandArcRadiusPercentage;

                Vector arcStartPoint = new Vector(radius, -cHandSectorAngle, center);
                builder.AddLine(arcStartPoint.Cartesian);

                Vector arcEndPoint = new Vector(radius, cHandSectorAngle, center);
                builder.AddArc(arcEndPoint.Cartesian, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Large);
                builder.EndFigure(CanvasFigureLoop.Closed);

                float handStroke = clockSize * cHandStrokePercentage;

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
                shapeVisual.Size = ContainerSize;
                shapeVisual.Shapes.Add(secondHand);
                shapeVisual.CenterPoint = new Vector3(center, 0f);
                shapeVisual.RotationAngleInDegrees = 180.0f;

                Animations.AddHand(shapeVisual);

                // add to visual tree
                Visual.Children.InsertAtTop(shapeVisual);
            }

            public class AnimationList
            {
                private const float cOneDegreeTime = 1.0f / 180.0f;
                private readonly (Visual visual, KeyFrameAnimation animation)[] list = new (Visual visual, KeyFrameAnimation animation)[31];

                private readonly Compositor compositor;
                private readonly LinearEasingFunction linearEasingFunction;
                private CompositionScopedBatch? batch;

                public AnimationList(Compositor compositor)
                {
                    this.compositor = compositor;
                    linearEasingFunction = compositor.CreateLinearEasingFunction();
                }

                public void AddHand(Visual visual)
                {
                    ScalarKeyFrameAnimation animation = compositor.CreateScalarKeyFrameAnimation();

                    animation.InsertKeyFrame(0.00f, 180.0f);
                    animation.InsertKeyFrame(1.00f, 360.0f, linearEasingFunction);
                    animation.Target = nameof(visual.RotationAngleInDegrees);

                    list[0].visual = visual;
                    list[0].animation = animation;
                }

                public void AddTickTrailSegment(Visual visual, int index)
                {
                    float onTime = (6.0f * ++index * cOneDegreeTime) - (cOneDegreeTime / 2.0f);

                    ScalarKeyFrameAnimation animation = compositor.CreateScalarKeyFrameAnimation();

                    animation.InsertKeyFrame(0.0f, 0.0f);
                    animation.InsertKeyFrame(onTime - 0.001f, 0.0f);
                    animation.InsertKeyFrame(onTime, 1.0f, linearEasingFunction);
                    animation.InsertKeyFrame(1.0f, 1.0f);
                    animation.Target = nameof(visual.Opacity);

                    list[index].visual = visual;
                    list[index].animation = animation;
                }

                public void StartForwardAnimations()
                {
                    if (batch is not null)
                        batch.Completed -= Batch_Completed;

                    batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
                    batch.Completed += Batch_Completed;

                    for (int index = 0; index < list.Length; index++)
                    {
                        list[index].animation.Direction = AnimationDirection.Normal;
                        list[index].animation.Duration = TimeSpan.FromSeconds(30.0);

                        list[index].visual.StartAnimation(list[index].animation.Target, list[index].animation);
                    }

                    batch.End();
                }

                private void Batch_Completed(object sender, CompositionBatchCompletedEventArgs args)
                {
                    if (sCompositionClock is not null)
                    {
                        if (sCompositionClock.XamlClock.State == StopwatchState.Running)
                        {
                            Utils.User32Sound.PlayExclamation();
                            sCompositionClock.XamlClock.State = StopwatchState.Stopped;
                        }
                        else if (sCompositionClock.XamlClock.State == StopwatchState.Rewinding)
                            sCompositionClock.XamlClock.State = StopwatchState.AtStart;
                    }
                }

                public void StopAnimations()
                {
                    for (int index = 0; index < list.Length; index++)
                        list[index].visual.StopAnimation(list[index].animation.Target);
                }

                public void StartRewindAnimations()
                {
                    if (batch is not null)
                        batch.Completed -= Batch_Completed;

                    batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
                    batch.Completed += Batch_Completed;

                    float startPoint = 1.0f - (cOneDegreeTime * (list[0].visual.RotationAngleInDegrees - 180.0f));

                    for (int index = 0; index < list.Length; index++)
                    {
                        list[index].animation.Direction = AnimationDirection.Reverse;
                        list[index].animation.Duration = TimeSpan.FromSeconds(1.0);

                        list[index].visual.StartAnimation(list[index].animation.Target, list[index].animation);

                        AnimationController? ac = list[index].visual.TryGetAnimationController(list[index].animation.Target);

                        if (ac is not null)
                            ac.Progress = startPoint;
                    }

                    batch.End();
                }
            }

            private readonly struct Vector
            {
                private readonly float length;
                private readonly float degrees;
                private readonly Vector2 offset;

                public Vector(float length, float degrees, Vector2 offset)
                {
                    this.length = length;
                    this.degrees = degrees;
                    this.offset = offset;
                }

                public Vector2 Cartesian
                {
                    get
                    {
                        float radians = degrees * (MathF.PI / 180.0f);
                        return new Vector2((float)MathF.FusedMultiplyAdd(length, MathF.Sin(radians), offset.X),
                                            (float)MathF.FusedMultiplyAdd(length, MathF.Cos(radians), offset.Y));
                    }
                }
            }
        }
    }
}
