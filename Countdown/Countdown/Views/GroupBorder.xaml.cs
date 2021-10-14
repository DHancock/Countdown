using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    [Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Children))]
    public sealed partial class GroupBorder : UserControl
    {
        public GroupBorder()
        {
            this.InitializeComponent();

            // offset the text block from the control edge
            HeadingPresenter.Margin = new Thickness(HeadingMargin, 0, 0, 0);

            // the padding is dependent on the corner radius
            ChildPresenter.Padding = CalculateContentPresenterPadding();

            BorderPath.Stroke = BorderBrush;
            BorderPath.StrokeThickness = BorderThickness.Left;

            // reuse the following properties to define the group border
            RegisterPropertyChangedCallback(CornerRadiusProperty, BorderPropertyChanged);
            RegisterPropertyChangedCallback(BorderThicknessProperty, BorderPropertyChanged);
            RegisterPropertyChangedCallback(BorderBrushProperty, BorderPropertyChanged);
            RegisterPropertyChangedCallback(FontSizeProperty, FontPropertyChanged);
            RegisterPropertyChangedCallback(FontFamilyProperty, FontPropertyChanged);
            RegisterPropertyChangedCallback(FontWeightProperty, FontPropertyChanged);
            RegisterPropertyChangedCallback(FontStyleProperty, FontPropertyChanged);
            RegisterPropertyChangedCallback(FontStretchProperty, FontPropertyChanged);

            Loaded += (s, e) =>
            {
                HeadingPresenter.SizeChanged += (s, e) => RedrawBorder();
                SizeChanged += (s, e) => RedrawBorder();

                RedrawBorder(); // first draw
            };
        }

        private void RedrawBorder()
        {
            if (IsLoaded)
                CreateBorderRoundedRect();
        }

        private void BorderPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            Thickness newPadding = CalculateContentPresenterPadding();

            if (ChildPresenter.Padding != newPadding)
                ChildPresenter.Padding = newPadding;

            BorderPath.Stroke = BorderBrush;
            BorderPath.StrokeThickness = BorderThickness.Left;

            RedrawBorder();
        }

        private Thickness CalculateContentPresenterPadding()
        {
            static double Max(double a, double b, double c) => Math.Max(Math.Max(a, b), c);

            // a non uniform corner radius is unlikely, but possible
            // a non uniform border thickness isn't supported
            return new Thickness(Max(CornerRadius.TopLeft, CornerRadius.BottomLeft, BorderThickness.Left),
                                    Max(CornerRadius.TopLeft, CornerRadius.TopRight, BorderThickness.Left),
                                    Max(CornerRadius.TopRight, CornerRadius.BottomRight, BorderThickness.Left),
                                    Max(CornerRadius.BottomLeft, CornerRadius.BottomRight, BorderThickness.Left));
        }

        private void FontPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            HeadingPresenter.FontFamily = FontFamily;
            HeadingPresenter.FontSize = FontSize;
            HeadingPresenter.FontStyle = FontStyle;
            HeadingPresenter.FontWeight = FontWeight;
            HeadingPresenter.FontStretch = FontStretch;
        }

        public static readonly DependencyProperty ChildrenProperty =
            DependencyProperty.Register(nameof(Children),
            typeof(object),
            typeof(GroupBorder),
            new PropertyMetadata(null, (d, e) => ((GroupBorder)d).ChildPresenter.Content = e.NewValue));

        public object Children
        {
            get { return GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register(nameof(Heading),
            typeof(object),
            typeof(GroupBorder),
            new PropertyMetadata(null, (d, e) => ((GroupBorder)d).HeadingPresenter.Content = e.NewValue));

        public object Heading
        {
            get { return GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }

        public static readonly DependencyProperty HeadingBaseLineRatioProperty =
            DependencyProperty.Register(nameof(HeadingBaseLineRatio),
            typeof(double),
            typeof(GroupBorder),
            new PropertyMetadata(0.61, (d, e) => ((GroupBorder)d).RedrawBorder()));

        // How far down the heading the border line is drawn.
        // If 0.0, it'll be at the top of the content.
        // If 1.0, it would be drawn at the bottom. 
        public double HeadingBaseLineRatio
        {
            get { return (double)GetValue(HeadingBaseLineRatioProperty); }
            set { SetValue(HeadingBaseLineRatioProperty, value); }
        }

        public static readonly DependencyProperty HeadingMarginProperty =
            DependencyProperty.Register(nameof(HeadingMargin),
            typeof(double),
            typeof(GroupBorder),
            new PropertyMetadata(12.0, HeadingMarginPropertyChanged));

        // The offset from the control edge to the heading presenter. 
        public double HeadingMargin
        {
            get { return (double)GetValue(HeadingMarginProperty); }
            set { SetValue(HeadingMarginProperty, value); }
        }

        private static void HeadingMarginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GroupBorder gb = (GroupBorder)d;

            gb.HeadingPresenter.Margin = new Thickness((double)e.NewValue, 0, 0, 0);
            gb.RedrawBorder();
        }

        public static readonly DependencyProperty BorderEndPaddingProperty =
            DependencyProperty.Register(nameof(BorderEndPadding),
            typeof(double),
            typeof(GroupBorder),
            new PropertyMetadata(3.0, (d, e) => ((GroupBorder)d).RedrawBorder()));

        // Padding between the end of the border and the start of the text.
        // This affects the border, changes won't cause a new measure pass.
        public double BorderEndPadding
        {
            get { return (double)GetValue(BorderEndPaddingProperty); }
            set { SetValue(BorderEndPaddingProperty, value); }
        }

        public static readonly DependencyProperty BorderStartPaddingProperty =
            DependencyProperty.Register(nameof(BorderStartPadding),
            typeof(double),
            typeof(GroupBorder),
            new PropertyMetadata(4.0, (d, e) => ((GroupBorder)d).RedrawBorder()));

        // Padding between the start of the border and the end of the text.
        // This affects the border, changes won't cause a new measure pass.
        public double BorderStartPadding
        {
            get { return (double)GetValue(BorderStartPaddingProperty); }
            set { SetValue(BorderStartPaddingProperty, value); }
        }

        private void CreateBorderRoundedRect()
        {
            static LineSegment LineTo(float x, float y) => new LineSegment() { Point = new Point(x, y), };
            static ArcSegment ArcTo(Point end, float radius) => new ArcSegment() { Point = end, RotationAngle = 90.0, IsLargeArc = false, Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise };

            PathFigure figure = new PathFigure()
            {
                IsClosed = false,
                IsFilled = false,
            };

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(figure);

            float textLHS = (float)(HeadingMargin - BorderEndPadding);
            float textRHS = (float)(HeadingMargin + HeadingPresenter.ActualWidth + BorderStartPadding);

            float halfStrokeThickness = (float)(BorderThickness.Left * 0.5);
            float headingCenter = (float)(HeadingPresenter.ActualHeight * Math.Clamp(HeadingBaseLineRatio, 0.0, 1.0));

            // right hand side of text
            float radius = (float)CornerRadius.TopRight;
            float xArcStart = ActualSize.X - (radius + halfStrokeThickness);

            if (textRHS < xArcStart) // check the first line is required, otherwise start at the arc
            {
                figure.StartPoint = new Point(textRHS, headingCenter);
                figure.Segments.Add(LineTo(xArcStart, headingCenter));
            }
            else
                figure.StartPoint = new Point(xArcStart, headingCenter);

            if (radius > 0) // top right corner
            {
                Point arcEnd = new Point(ActualSize.X - halfStrokeThickness, headingCenter + radius);
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            radius = (float)CornerRadius.BottomRight;
            figure.Segments.Add(LineTo(ActualSize.X - halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness)));

            if (radius > 0) // bottom right corner
            {
                Point arcEnd = new Point(ActualSize.X - (radius + halfStrokeThickness), ActualSize.Y - halfStrokeThickness);
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            radius = (float)CornerRadius.BottomLeft;
            figure.Segments.Add(LineTo(radius + halfStrokeThickness, ActualSize.Y - halfStrokeThickness));

            if (radius > 0) // bottom left corner
            {
                Point arcEnd = new Point(halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness));
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            radius = (float)CornerRadius.TopLeft;
            figure.Segments.Add(LineTo(halfStrokeThickness, headingCenter + radius));

            if (radius > 0) // top left corner
            {
                Point arcEnd = new Point(radius + halfStrokeThickness, headingCenter);
                figure.Segments.Add(ArcTo(arcEnd, radius));
            }

            // check if the last line is required, the arc may be too large
            if (radius + halfStrokeThickness < textLHS)
                figure.Segments.Add(LineTo(textLHS, headingCenter));

            // add the new path geometry in to the visual tree
            BorderPath.Data = pathGeometry;
        }
    }
}
