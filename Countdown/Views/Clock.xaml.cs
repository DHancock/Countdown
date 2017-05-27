using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;



namespace Countdown.Views
{
    /// <summary>
    /// Interaction logic for Clock.xaml
    /// </summary>
    public partial class Clock : UserControl
    {
    

        public double Ticks
        {
            get { return (double)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }


        public static readonly DependencyProperty TicksProperty =
                DependencyProperty.Register(nameof(Ticks),
                typeof(double),
                typeof(Clock),
                new PropertyMetadata(0.0, OnTicksPropertyChanged, TicksCoerceValue));




        private static object TicksCoerceValue(DependencyObject d, object baseValue)
        {
            if (baseValue is double ticks)
            {
                ticks = Math.Max(0.0, ticks);
                ticks = Math.Min(180.0, ticks);

                return ticks;
            }

            return 0.0D;
        }


        private static void OnTicksPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Clock clock)
            {
                clock.tickTrail.Tick = clock.clockHand.Tick = clock.Ticks;

                clock.tickTrail.InvalidateVisual();
                clock.clockHand.InvalidateVisual();
            }
        }

        


        public Clock()
        {
            InitializeComponent();

            outerFrame.InsetPercentage = 0.00;
            outerFrame.StrokePercentage = 0.01;

            innerFrame.InsetPercentage = outerFrame.StrokePercentage;
            innerFrame.StrokePercentage = 0.02;

            tickMarks.InsetPercentage = innerFrame.InsetPercentage + innerFrame.StrokePercentage;
            tickMarks.StrokePercentage = 0.01;

            tickTrail.InsetPercentage = tickMarks.InsetPercentage;

            clockHand.InsetPercentage = tickMarks.InsetPercentage + 0.02;
            clockHand.StrokePercentage = 0.01;
        }
    }


    



    internal sealed class Clock_TickTrail : ShapeBase
    {
        public double Tick { get; set; }

        protected override void DrawToStream(StreamGeometryContext stream, double radius, Point offset)
        {
            const double outerRadiusPercent = 0.99;
            const double innerRadiusPercent = 0.45;

            double outerRadius = radius * outerRadiusPercent;
            double innerRadius = radius * innerRadiusPercent;

            Size innerSize = new Size(innerRadius, innerRadius);
            Size outerSize = new Size(outerRadius, outerRadius);

            double angle = 180.0 - Tick; // degrees, zero is at 6 o'clock and sweeps counter clockwise  

            Vector topLeft = new Vector(outerRadius, 180.0, offset);
            Vector topRight = new Vector(outerRadius, angle, offset);

            Vector bottomLeft = new Vector(innerRadius, 180.0, offset);
            Vector bottomRight = new Vector(innerRadius, angle, offset);

            stream.BeginFigure(topLeft.Cartesian, true, true);
            stream.ArcTo(topRight.Cartesian, outerSize, 0.0, false, SweepDirection.Clockwise, true, false);
            stream.LineTo(bottomRight.Cartesian, true, true);
            stream.ArcTo(bottomLeft.Cartesian, innerSize, 0.0, false, SweepDirection.Counterclockwise, true, false);
        }
    }

    

    internal sealed class Clock_Hand : ShapeBase
    {
        public double Tick { get; set; }

        protected override void DrawToStream(StreamGeometryContext stream, double radius, Point offset)
        {
            double angle = 180.0 - Tick; // degrees, zero is at 6 o'clock and sweeps counter clockwise  

            Vector tip = new Vector(radius, angle, offset);
            stream.BeginFigure(tip.Cartesian, true, true);

            const double innerRadius = 0.13;
            const double sectorAngle = 30.0;

            radius *= innerRadius;

            Vector arcPoint = new Vector(radius, angle - sectorAngle , offset);
            stream.LineTo(arcPoint.Cartesian, true, true);

            // draw the circle round the center
            arcPoint.SetAngle(angle + sectorAngle);
            stream.ArcTo(arcPoint.Cartesian, new Size(radius, radius), 0.0, true, SweepDirection.Clockwise, true, true);
        }
        
    }






    internal sealed class Clock_TickMarks : ShapeBase
    {

        protected override void DrawToStream(StreamGeometryContext stream, double radius, Point offset)
        {
            // because this isn't drawing a circle the radius needs to be 
            // increased by half the stroke width so lines but up against 
            // the previous shape, same as setting the line end cap to square
            radius += StrokeThickness / 2.0;

            const double startCheck = 0.75;
            const double endCheck = 0.90;

            // draw the 5 minute tick marks, at 30 degree intervals
            Vector startPos = new Vector(radius * startCheck, 0, offset);
            Vector endPos = new Vector(radius * endCheck, 0, offset);

            DrawCheck(30D);
            DrawCheck(60D);
            DrawCheck(120D);
            DrawCheck(150D);
            DrawCheck(210D);
            DrawCheck(240D);
            DrawCheck(300D);
            DrawCheck(330D);
           
            void DrawCheck (double angle)
            {
                startPos.SetAngle(angle);
                endPos.SetAngle(angle);

                stream.BeginFigure(startPos.Cartesian, false, false);
                stream.LineTo(endPos.Cartesian, true, false);
            }

            // vertical cross hair
            stream.BeginFigure(new Point(offset.X, offset.Y - radius), false, false);
            stream.LineTo(new Point(offset.X, radius + offset.Y), true, false);

            // horizontal cross hair
            stream.BeginFigure(new Point(offset.X - radius, offset.Y), false, false);
            stream.LineTo(new Point(offset.X + radius, offset.Y), true, false);
        }
    }




    internal sealed class Clock_Frame : ShapeBase
    {
        // a simple circle
        protected override void DrawToStream(StreamGeometryContext stream, double radius, Point offset)
        {
            Point startPoint = new Point(offset.X, offset.Y - radius);
            Point midPoint = new Point(offset.X, offset.Y + radius);
            Size size = new Size(radius, radius);

            stream.BeginFigure(startPoint, true, false);
            stream.ArcTo(midPoint, size, 0.0, true, SweepDirection.Clockwise, true, false);
            stream.ArcTo(startPoint, size, 0.0, true, SweepDirection.Clockwise, true, false);
        }
    }



    internal abstract class ShapeBase : Shape
    {
        public double InsetPercentage { get; set; }
        public double StrokePercentage { get; set; }


        protected override Geometry DefiningGeometry
        {
            get
            {
                double size = Math.Min(RenderSize.Width, RenderSize.Height);

                double radius = size / 2.0;
                Point offset = new Point(radius, radius);

                StrokeThickness = size * StrokePercentage;

                // adjust drawing radius
                radius -= (size * InsetPercentage);
                radius -= StrokeThickness / 2.0 ;

                StreamGeometry geometry = new StreamGeometry()
                {
                    FillRule = FillRule.Nonzero
                };

                using (StreamGeometryContext stream = geometry.Open())
                {
                    if (stream != null)
                        DrawToStream(stream, radius, offset);
                }

                geometry.Freeze();
                return geometry;
            }
        }



        protected abstract void DrawToStream(StreamGeometryContext ctx, double radius, Point offset);
        


        protected override Size MeasureOverride(Size constraint)
        {
            // when presented with infinites use this size
            const double cDefaultSize = 50;

            if (Double.IsInfinity(constraint.Height) || Double.IsInfinity(constraint.Width))
            {
                // Infinity means this part is inside a scrollable control, stack
                // panel etc. The other dimension could either be the size of the 
                // container, the xaml specified size or also infinity too

                if (Double.IsInfinity(constraint.Height))
                {
                    if (Double.IsInfinity(constraint.Width)) // both sizes are unbound, use a default
                        constraint.Width = constraint.Height = cDefaultSize;
                    else
                        constraint.Height = constraint.Width;
                }
                else
                    constraint.Width = constraint.Height;
            }

            // Clock elements are always square, use the minimum size.
            // The constraint has already been adjusted to take account of
            // min and max size properties.

            if (constraint.Width > constraint.Height)
                constraint.Width = constraint.Height;
            else
                constraint.Height = constraint.Width;

            return constraint;
        }
    }



    internal sealed class Vector
    {
        private double radius;
        private double radians;
        private Point offset;


        public Vector(double radius, double degrees, Point offset)
        {
            this.radius = radius;
            this.offset = offset;
            SetAngle(degrees);
        }


        public void SetAngle(double degrees)
        {
            radians = degrees * (Math.PI / 180.0);
        }


        public Point Cartesian
        {
            get
            {
                return new Point((radius * Math.Sin(radians)) + offset.X, (radius * Math.Cos(radians)) + offset.Y);
            }
        }

        public override string ToString() => $"({Cartesian.X}, {Cartesian.Y})";
    }
}
