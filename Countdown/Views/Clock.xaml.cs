using Countdown.ViewModels;

namespace Countdown.Views;

internal sealed partial class Clock : UserControl
{
    private static CompositionClock? sCompositionClock;
    private static AudioHelper? sAudioHelper;

    public Clock()
    {
        this.InitializeComponent();

        Loaded += (s, e) =>
        {
            Clock xamlClock = (Clock)s;

            if (sCompositionClock is null)
            {
                sCompositionClock = new CompositionClock(xamlClock);
                sAudioHelper = new AudioHelper();
                State = StopwatchState.AtStart;
            }
            else
            {
                sCompositionClock.XamlClock = xamlClock;
            }

            if (ElementCompositionPreview.GetElementChildVisual(xamlClock) is null)
            {
                ElementCompositionPreview.SetElementChildVisual(xamlClock, sCompositionClock.Visual);
            }
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
        if (sCompositionClock is null || !ReferenceEquals(d, sCompositionClock.XamlClock))
        {
            return;
        }
        
        StopwatchState oldState = (StopwatchState)e.OldValue;
        StopwatchState newState = (StopwatchState)e.NewValue;

        if (oldState == StopwatchState.Undefined) // a new page has been loaded
        {
            return;
        }

        if (oldState == newState) // bindings have been re-evaluated
        {
            return;
        }

        switch (newState)
        {
            case StopwatchState.Running:
                {
                    sCompositionClock.Animations.StartForwardAnimations();
                    sAudioHelper?.Start();
                    break;
                }
            
            case StopwatchState.Stopped: // the user halted the countdown 
                {
                    sCompositionClock.Animations.StopAnimations();
                    sAudioHelper?.Stop();
                    break;
                }
                
            case StopwatchState.Rewinding:
                {
                    sCompositionClock.Animations.StartRewindAnimations();
                    break;
                }

            case StopwatchState.Completed: // let the audio play out, it's not synchronized with the animation
            case StopwatchState.AtStart:
            case StopwatchState.Initializing: break;

            default: throw new Exception($"invalid state: {newState}");
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return CompositionClock.ContainerSize.ToSize();
    }

    public Color FaceColor
    {
        get { return (Color)GetValue(FaceColorProperty); }
        set { SetValue(FaceColorProperty, value); }
    }

    public static readonly DependencyProperty FaceColorProperty =
        DependencyProperty.Register(nameof(FaceColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.Face, (Color)e.NewValue)));

    public Color TickTrailColor
    {
        get { return (Color)GetValue(TickTrailColorProperty); }
        set { SetValue(TickTrailColorProperty, value); }
    }

    public static readonly DependencyProperty TickTrailColorProperty =
        DependencyProperty.Register(nameof(TickTrailColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.TickTrail, (Color)e.NewValue)));

    public Color InnerFrameColor
    {
        get { return (Color)GetValue(InnerFrameColorProperty); }
        set { SetValue(InnerFrameColorProperty, value); }
    }

    public static readonly DependencyProperty InnerFrameColorProperty =
        DependencyProperty.Register(nameof(InnerFrameColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.InnerFrame, (Color)e.NewValue)));

    public Color OuterFrameColor
    {
        get { return (Color)GetValue(OuterFrameColorProperty); }
        set { SetValue(OuterFrameColorProperty, value); }
    }

    public static readonly DependencyProperty OuterFrameColorProperty =
        DependencyProperty.Register(nameof(OuterFrameColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.OuterFrame, (Color)e.NewValue)));

    public Color FrameTickColor
    {
        get { return (Color)GetValue(FrameTickColorProperty); }
        set { SetValue(FrameTickColorProperty, value); }
    }

    public static readonly DependencyProperty FrameTickColorProperty =
        DependencyProperty.Register(nameof(FrameTickColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.FrameTick, (Color)e.NewValue)));

    public Color TickMarksColor
    {
        get { return (Color)GetValue(TickMarksColorProperty); }
        set { SetValue(TickMarksColorProperty, value); }
    }

    public static readonly DependencyProperty TickMarksColorProperty =
        DependencyProperty.Register(nameof(TickMarksColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.TickMarks, (Color)e.NewValue)));

    public Color HandStrokeColor
    {
        get { return (Color)GetValue(HandStrokeColorProperty); }
        set { SetValue(HandStrokeColorProperty, value); }
    }

    public static readonly DependencyProperty HandStrokeColorProperty =
        DependencyProperty.Register(nameof(HandStrokeColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.HandStroke, (Color)e.NewValue)));

    public Color HandFillColor
    {
        get { return (Color)GetValue(HandFillColorProperty); }
        set { SetValue(HandFillColorProperty, value); }
    }

    public static readonly DependencyProperty HandFillColorProperty =
        DependencyProperty.Register(nameof(HandFillColor),
            typeof(Color),
            typeof(Clock),
            new PropertyMetadata(Colors.Transparent, (d, e) => UpdateBrush(BrushId.HandFill, (Color)e.NewValue)));

    private static void UpdateBrush(BrushId brushIndex, Color newColor)
    {
        if (sCompositionClock is null)
        {
            return;
        }

        CompositionColorBrush brush = sCompositionClock.Brushes[brushIndex];

        if (brush is not null && (brush.Color != newColor))
        {
            brush.Color = newColor;
        }
    }

    public bool IsDropShadowVisible
    {
        get { return (bool)GetValue(IsDropShadowVisibleProperty); }
        set { SetValue(IsDropShadowVisibleProperty, value); }
    }

    public static readonly DependencyProperty IsDropShadowVisibleProperty =
        DependencyProperty.Register(nameof(IsDropShadowVisible),
            typeof(bool),
            typeof(Clock),
            new PropertyMetadata(true, (d, e) => UpdateDropShadowVisibility((bool)e.NewValue)));

    private static void UpdateDropShadowVisibility(bool isVisible)
    {
        if (sCompositionClock is null)
        {
            return;
        }

        if (sCompositionClock.IsDropShadowVisible != isVisible)
        {
            sCompositionClock.IsDropShadowVisible = isVisible;
        }
    }


    private sealed class CompositionClock
    {
        public ContainerVisual Visual { get; }
        public static Vector2 ContainerSize { get; } = new Vector2(200);
        public AnimationList Animations { get; }
        public BrushList Brushes { get; }
        public Clock XamlClock { get; set; }
        private SpriteVisual DropShadowVisual { get; }

        public CompositionClock(Clock xamlClock)
        {
            Compositor compositor = App.MainWindow!.Compositor;
            Visual = compositor.CreateContainerVisual();
            Animations = new AnimationList(compositor);
            Brushes = new BrushList(compositor, xamlClock);
            XamlClock = xamlClock;

            // allow room for the drop shadow
            float clockSize = ContainerSize.X * 0.95f;
            Vector2 center = new Vector2(ContainerSize.X * 0.5f);

            DropShadowVisual = CreateFace(compositor, center, clockSize);
            DropShadowVisual.IsVisible = xamlClock.IsDropShadowVisible;

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

        private void CreateTickTrail(Compositor compositor, Vector2 center, float clockSize)
        {
            float radius = clockSize * 0.5f;
            float outerRadius = radius * cTickTrailOuterRadiusPercent;
            float innerRadius = radius * cTickTrailInnerRadiusPercent;

            const float startAngle = 179.5f;
            const float endAngle = 174.5f;

            Vector2 topLeft = VectorToCartesian(outerRadius, startAngle, center);
            Vector2 topRight = VectorToCartesian(outerRadius, endAngle, center);
            Vector2 bottomLeft = VectorToCartesian(innerRadius, startAngle, center);
            Vector2 bottomRight = VectorToCartesian(innerRadius, endAngle, center);

            using CanvasPathBuilder builder = new CanvasPathBuilder(null);

            builder.BeginFigure(topLeft);
            builder.AddArc(topRight, outerRadius, outerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
            builder.AddLine(bottomRight);
            builder.AddLine(bottomLeft);
            builder.EndFigure(CanvasFigureLoop.Closed);

            // create a composition geometry from the canvas path data
            using CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
            CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
            pathGeometry.Path = new CompositionPath(canvasGeometry);

            // create every trail element visual, then change its opacity as required
            for (int segment = 0; segment < 30; segment++)
            {
                // create a shape from the geometry
                CompositionSpriteShape tickSegment = compositor.CreateSpriteShape(pathGeometry);
                tickSegment.FillBrush = Brushes[BrushId.TickTrail];
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

        private SpriteVisual CreateFace(Compositor compositor, Vector2 center, float clockSize)
        {
            CompositionSpriteShape CreateCircle(float radius, float stroke, Vector2 offset, BrushId fillBrushId, BrushId strokeBrushId)
            {
                CompositionEllipseGeometry circleGeometry = compositor.CreateEllipseGeometry();
                circleGeometry.Radius = new Vector2(radius);

                CompositionSpriteShape circleShape = compositor.CreateSpriteShape(circleGeometry);
                circleShape.Offset = offset;
                circleShape.FillBrush = Brushes[fillBrushId];

                if (stroke > 0.0f)
                {
                    circleShape.StrokeThickness = stroke;
                    circleShape.StrokeBrush = Brushes[strokeBrushId];
                }

                return circleShape;
            }

            CompositionContainerShape shapeContainer = compositor.CreateContainerShape();

            // create a visual for the shape container
            ShapeVisual shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Size = ContainerSize;
            shapeVisual.Shapes.Add(shapeContainer);

            // outer frame
            float radius = clockSize * 0.5f;
            shapeContainer.Shapes.Add(CreateCircle(radius, 0f, center, BrushId.OuterFrame, BrushId.OuterFrame));

            // create the drop shadow now from the simplest shape
            SpriteVisual shadowVisual = CreateDropShadow(compositor, shapeVisual);

            // inner frame
            float outerFrameStroke = clockSize * cOuterFrameStrokePercentage;
            float innerFrameStroke = clockSize * cInnerFrameStrokePercentage;
            radius -= outerFrameStroke + (innerFrameStroke * 0.5f);

            shapeContainer.Shapes.Add(CreateCircle(radius, innerFrameStroke, center, BrushId.Transparent, BrushId.InnerFrame));

            // tick marks around inner frame
            float tickRadius = innerFrameStroke / 5.0f;      // TODO constants, and color static

            for (float degrees = 0; degrees < 360.0; degrees += 30.0f)
            {
                shapeContainer.Shapes.Add(CreateCircle(tickRadius, 0f, VectorToCartesian(radius, degrees, center), BrushId.FrameTick, BrushId.Transparent));
            }

            // clock face fill
            radius -= innerFrameStroke * 0.5f;
            shapeContainer.Shapes.Add(CreateCircle(radius, 0f, center, BrushId.Face, BrushId.Transparent));

            // insert into the tree 
            Visual.Children.InsertAtBottom(shapeVisual);
            Visual.Children.InsertAtBottom(shadowVisual);

            return shadowVisual;
        }

        private static SpriteVisual CreateDropShadow(Compositor compositor, ShapeVisual sourceVisual)
        {
            // create a surface brush to use as a mask for the drop shadow
            CompositionVisualSurface surface = compositor.CreateVisualSurface();
            surface.SourceSize = ContainerSize;
            surface.SourceVisual = sourceVisual;

            // create the drop shadow
            DropShadow shadow = compositor.CreateDropShadow();
            shadow.Mask = compositor.CreateSurfaceBrush(surface);
            shadow.Offset = new Vector3(new Vector2(1.5f), 0f);
            shadow.Color = Colors.DimGray;

            // create a visual for the shadow
            SpriteVisual shadowVisual = compositor.CreateSpriteVisual();
            shadowVisual.Size = ContainerSize;
            shadowVisual.Shadow = shadow;

            return shadowVisual;
        }

        private void CreateFaceTickMarks(Compositor compositor, Vector2 center, float clockSize)
        {
            CompositionSpriteShape CreateLine(Vector2 start, Vector2 end, float thickness, BrushId brushId, CompositionStrokeCap endCap)
            {
                CompositionLineGeometry lineGeometry = compositor.CreateLineGeometry();
                lineGeometry.Start = start;
                lineGeometry.End = end;

                CompositionSpriteShape lineShape = compositor.CreateSpriteShape(lineGeometry);
                lineShape.StrokeThickness = thickness;
                lineShape.StrokeBrush = Brushes[brushId];
                lineShape.StrokeEndCap = endCap;
                lineShape.StrokeStartCap = endCap;

                return lineShape;
            }

            CompositionContainerShape shapeContainer = compositor.CreateContainerShape();

            // add the 5 second tick marks
            float stroke = clockSize * cTickMarksStrokePercentage;
            float startLength = clockSize * 0.5f * cTickMarksInnerRadiusPercentage;
            float endLength = clockSize * 0.5f * cTickMarksOuterRadiusPercentage;
            CompositionStrokeCap endCap = CompositionStrokeCap.Round;

            for (int degrees = 30; degrees < 360; degrees += 30)
            {
                if (degrees % 90 > 0)
                {
                    Vector2 inner = VectorToCartesian(startLength, degrees, center);
                    Vector2 outer = VectorToCartesian(endLength, degrees, center);
                    shapeContainer.Shapes.Add(CreateLine(inner, outer, stroke, BrushId.TickMarks, endCap));
                }
            }

            // now the cross hairs
            float radius = (clockSize * 0.5f) - (clockSize * (cOuterFrameStrokePercentage + cInnerFrameStrokePercentage));

            // horizontal cross hair
            Vector2 start = new Vector2(center.X - radius, center.Y);
            Vector2 end = new Vector2(center.X + radius, center.Y);
            shapeContainer.Shapes.Add(CreateLine(start, end, stroke, BrushId.TickMarks, CompositionStrokeCap.Flat));

            // vertical cross hair
            start = new Vector2(center.X, center.Y - radius);
            end = new Vector2(center.X, center.Y + radius);
            shapeContainer.Shapes.Add(CreateLine(start, end, stroke, BrushId.TickMarks, CompositionStrokeCap.Flat));

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

            Vector2 tip = VectorToCartesian(clockSize * 0.5f * cHandTipRadiusPercentage, 0.0f, center);
            builder.BeginFigure(tip);

            float radius = clockSize * cHandArcRadiusPercentage;

            Vector2 arcStartPoint = VectorToCartesian(radius, -cHandSectorAngle, center);
            builder.AddLine(arcStartPoint);

            Vector2 arcEndPoint = VectorToCartesian(radius, cHandSectorAngle, center);
            builder.AddArc(arcEndPoint, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Large);
            builder.EndFigure(CanvasFigureLoop.Closed);

            float handStroke = clockSize * cHandStrokePercentage;

            // create a composition geometry from the canvas path data
            using CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
            CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
            pathGeometry.Path = new CompositionPath(canvasGeometry);

            // create a shape from the geometry
            CompositionSpriteShape secondHand = compositor.CreateSpriteShape(pathGeometry);
            secondHand.FillBrush = Brushes[BrushId.HandFill];
            secondHand.StrokeThickness = handStroke;
            secondHand.StrokeLineJoin = CompositionStrokeLineJoin.Round;
            secondHand.StrokeBrush = Brushes[BrushId.HandStroke];

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

        internal bool IsDropShadowVisible
        {
            set => DropShadowVisual.IsVisible = value;
            get => DropShadowVisual.IsVisible;
        }
    }

    private sealed class AnimationList
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

            animation.InsertKeyFrame(0.0f, 180.0f);
            animation.InsertKeyFrame(1.0f, 360.0f, linearEasingFunction);
            animation.Target = nameof(visual.RotationAngleInDegrees);

            list[0].visual = visual;
            list[0].animation = animation;
        }

        public void AddTickTrailSegment(Visual visual, int index)
        {
            // switch segment zero on at 5.5 degrees, the next at 11.5, then 17.5 etc.
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
            {
                batch.Completed -= Batch_Completed;
            }

            batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += Batch_Completed;

            foreach ((Visual visual, KeyFrameAnimation animation) in list)
            {
                animation.Direction = AnimationDirection.Normal;
                animation.Duration = TimeSpan.FromSeconds(30.0);

                visual.StartAnimation(animation.Target, animation);
            }

            batch.End();
        }

        private void Batch_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            try
            {
                if (sCompositionClock is null)
                {
                    return;
                }

                if (sCompositionClock.XamlClock.State == StopwatchState.Running)
                {
                    sCompositionClock.XamlClock.State = StopwatchState.Completed;
                }

                else if (sCompositionClock.XamlClock.State == StopwatchState.Rewinding)
                {
                    sCompositionClock.XamlClock.State = StopwatchState.AtStart;
                }
            }
            catch (Exception ex)
            {
                // if the app is shutting down, try to fail gracefully
                Debug.WriteLine(ex.ToString());
            }
        }

        public void StopAnimations()
        {
            foreach ((Visual visual, KeyFrameAnimation animation) in list)
            {
                visual.StopAnimation(animation.Target);
            }
        }

        public void StartRewindAnimations()
        {
            if (batch is not null)
            {
                batch.Completed -= Batch_Completed;
            }

            batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += Batch_Completed;

            float startPoint = 1.0f - (cOneDegreeTime * (list[0].visual.RotationAngleInDegrees - 180.0f));

            foreach ((Visual visual, KeyFrameAnimation animation) in list)
            {
                animation.Direction = AnimationDirection.Reverse;
                animation.Duration = TimeSpan.FromSeconds(2);

                visual.StartAnimation(animation.Target, animation);

                AnimationController? ac = visual.TryGetAnimationController(animation.Target);

                if (ac is not null)
                {
                    ac.Progress = startPoint;
                }
            }

            batch.End();
        }
    }

    private enum BrushId { Transparent, Face, TickTrail, InnerFrame, OuterFrame, FrameTick, TickMarks, HandStroke, HandFill }

    private sealed class BrushList
    {
        private readonly CompositionColorBrush[] list = new CompositionColorBrush[9];

        public BrushList(Compositor compositor, Clock xamlClock)
        {
            this[BrushId.Transparent] = compositor.CreateColorBrush(Colors.Transparent);
            this[BrushId.Face] = compositor.CreateColorBrush(xamlClock.FaceColor);
            this[BrushId.TickTrail] = compositor.CreateColorBrush(xamlClock.TickTrailColor);

            this[BrushId.InnerFrame] = compositor.CreateColorBrush(xamlClock.InnerFrameColor);
            this[BrushId.OuterFrame] = compositor.CreateColorBrush(xamlClock.OuterFrameColor);
            this[BrushId.FrameTick] = compositor.CreateColorBrush(xamlClock.FrameTickColor);

            this[BrushId.TickMarks] = compositor.CreateColorBrush(xamlClock.TickMarksColor);
            this[BrushId.HandStroke] = compositor.CreateColorBrush(xamlClock.HandStrokeColor);
            this[BrushId.HandFill] = compositor.CreateColorBrush(xamlClock.HandFillColor);
        }

        public CompositionColorBrush this[BrushId i]
        {
            get => list[(int)i];
            private set => list[(int)i] = value;
        }
    }

    private static Vector2 VectorToCartesian(float length, float angle, Vector2 offset)
    {
        (float sin, float cos) = MathF.SinCos(angle * (MathF.PI / 180.0f));

        return new Vector2(MathF.FusedMultiplyAdd(length, sin, offset.X),
                            MathF.FusedMultiplyAdd(length, cos, offset.Y));
    }

    private sealed class AudioHelper // no IDisposable, it's never garbage collected
    {
        private readonly MediaPlayer mediaPlayer = new MediaPlayer();
        private readonly Stream? stream;

        public AudioHelper()
        {
            stream = typeof(App).Assembly.GetManifestResourceStream("Countdown.Resources.audio.dat");

            if (stream is not null)
            {
                mediaPlayer.SetStreamSource(stream.AsRandomAccessStream());
                mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;
                Settings.Data.VolumeChanged += (s, a) => mediaPlayer.Volume = Settings.Data.VolumePercentage / 100.0;

                Debug.Assert(App.MainWindow is not null);
                App.MainWindow.Closed += (s, e) => Stop();
            }
        }

        public void Start()
        {
            mediaPlayer.Position = TimeSpan.Zero;
            mediaPlayer.Play();
        }

        public void Stop() => mediaPlayer.Pause();
    }
}
