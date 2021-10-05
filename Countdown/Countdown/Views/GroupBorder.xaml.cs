using System;
using System.Numerics;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    [Microsoft.UI.Xaml.Markup.ContentProperty(Name = "ChildContent")]
    public sealed partial class GroupBorder : UserControl
    {
        private readonly Compositor compositor;
        private readonly ContainerVisual containerVisual;

        public GroupBorder()
        {
            this.InitializeComponent();

            compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            containerVisual = compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(this, containerVisual);

            // the padding is dependent on the corner radius
            ChildPresenter.Padding = CalculateContentPresenterPadding();

            // reusing the following properties to define the group border
            RegisterPropertyChangedCallback(CornerRadiusProperty, CornerRadiusPropertyChanged);
            RegisterPropertyChangedCallback(BorderThicknessProperty, BorderThicknessPropertyChanged);

            Loaded += (s, e) =>
            {
                HeadingText.SizeChanged += (s, e) => RedrawBorder();
                SizeChanged += (s, e) => RedrawBorder();

                RedrawBorder(); // first draw
            };
        }

        private void RedrawBorder()
        {
            containerVisual.Children.RemoveAll();
            containerVisual.Children.InsertAtBottom(CreateBorderRoundedRect());
        }

        private void CornerRadiusPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            Thickness newPadding = CalculateContentPresenterPadding();

            if (ChildPresenter.Padding != newPadding)
                ChildPresenter.Padding = newPadding;
            else
                RedrawBorder();
        }

        private Thickness CalculateContentPresenterPadding()
        {
            // a non uniform corner radius is unlikely, but possible
            const double inset = 1.0;
            return new Thickness(Math.Max(CornerRadius.TopLeft, CornerRadius.BottomLeft) + inset,
                                        Math.Max(CornerRadius.TopLeft, CornerRadius.TopRight) + inset,
                                        Math.Max(CornerRadius.TopRight, CornerRadius.BottomRight) + inset,
                                        Math.Max(CornerRadius.BottomLeft, CornerRadius.BottomRight) + inset);
        }

        private void BorderThicknessPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            RedrawBorder();
        }

        public static readonly DependencyProperty ChildContentProperty =
            DependencyProperty.Register("ChildContent",
            typeof(object),
            typeof(GroupBorder),
            new PropertyMetadata(null));

        public object ChildContent
        {
            get { return (object)GetValue(ChildContentProperty); }
            set { SetValue(ChildContentProperty, value); }
        }


        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading",
            typeof(string),
            typeof(GroupBorder),
            new PropertyMetadata(string.Empty, HeadingTextPropertyChanged));

        public string Heading
        {
            get { return (string)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }

        private static void HeadingTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GroupBorder)d).HeadingText.Text = e.NewValue as string ?? string.Empty;
        }

        public static readonly DependencyProperty GroupBorderColourProperty =
              DependencyProperty.Register("GroupBorderColour",
              typeof(Color),
              typeof(GroupBorder),
              new PropertyMetadata(Colors.LightGray, GroupBorderColourPropertyChanged));

        public Color GroupBorderColour
        {
            get { return (Color)GetValue(GroupBorderColourProperty); }
            set { SetValue(GroupBorderColourProperty, value); }
        }

        private static void GroupBorderColourPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GroupBorder gb = (GroupBorder)d;

            if (gb.IsLoaded)
                gb.RedrawBorder();
        }


        private ShapeVisual CreateBorderRoundedRect()
        {
            // space between the end of the border line and the start of the text
            const float cTextStartPadding = 3.0f;
            // space between the end of the text and the start of the border line
            const float cTextEndPadding = 4.0f;

            // non uniform border thicknesses aren't supported
            float borderStrokeThickness = (float)BorderThickness.Left;
            float halfStrokeThickness = borderStrokeThickness * 0.5f;

            // How far down the TextBlock the border line is draw. If 0.0, it
            // would be drawn at the top of the TextBlock. At 1.0, it would be drawn
            // at the bottom. 
            const float cBaseLineRatio = 0.65f;   // TODO dp? and test for dpi change

            float textRHS;
            float textLHS;

            if (FlowDirection == FlowDirection.LeftToRight)
            {
                textLHS = (float)HeadingText.Margin.Left - cTextStartPadding;
                textRHS = (float)(HeadingText.Margin.Left + HeadingText.ActualWidth + cTextEndPadding);
            }
            else
            {
                textLHS = (float)(ActualSize.X - (HeadingText.Margin.Left + HeadingText.ActualWidth + cTextEndPadding));
                textRHS = (float)(ActualSize.X - HeadingText.Margin.Left) + cTextStartPadding;
            }

            float fontCenter = (float)(HeadingText.ActualHeight * cBaseLineRatio);

            using CanvasPathBuilder builder = new CanvasPathBuilder(null);

            // right hand side of text
            float radius = (float)CornerRadius.TopRight;
            builder.BeginFigure(textRHS, fontCenter, CanvasFigureFill.DoesNotAffectFills);
            builder.AddLine(ActualSize.X - (radius + halfStrokeThickness), fontCenter);

            // top right corner
            Vector2 arcEnd = new Vector2(ActualSize.X - halfStrokeThickness, fontCenter + radius + halfStrokeThickness);

            if (radius > 0)
                builder.AddArc(arcEnd, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            // bottom right corner
            radius = (float)CornerRadius.BottomRight;
            builder.AddLine(arcEnd.X, ActualSize.Y - (radius + halfStrokeThickness));
            arcEnd = new Vector2(ActualSize.X - (radius + halfStrokeThickness), ActualSize.Y - halfStrokeThickness);

            if (radius > 0)
                builder.AddArc(arcEnd, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            // bottom left corner
            radius = (float)CornerRadius.BottomLeft;
            builder.AddLine(radius + halfStrokeThickness, arcEnd.Y);
            arcEnd = new Vector2(halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness));

            if (radius > 0)
                builder.AddArc(arcEnd, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            // top left corner
            radius = (float)CornerRadius.TopLeft;
            builder.AddLine(arcEnd.X, fontCenter + radius);
            arcEnd = new Vector2(radius + halfStrokeThickness, fontCenter);

            if (radius > 0)
                builder.AddArc(arcEnd, radius, radius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            builder.AddLine(textLHS, arcEnd.Y);
            builder.EndFigure(CanvasFigureLoop.Open);

            // create a composition geometry from the canvas path data
            CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
            CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
            pathGeometry.Path = new CompositionPath(canvasGeometry);

            // create a shape from the geometry
            CompositionSpriteShape spriteShape = compositor.CreateSpriteShape(pathGeometry);
            spriteShape.FillBrush = compositor.CreateColorBrush(Colors.Transparent);
            spriteShape.StrokeThickness = borderStrokeThickness;
            spriteShape.StrokeBrush = compositor.CreateColorBrush(GroupBorderColour);

            // create a visual for the shape
            ShapeVisual shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Size = ActualSize;
            shapeVisual.Shapes.Add(spriteShape);

            return shapeVisual;
        }
    }
}
