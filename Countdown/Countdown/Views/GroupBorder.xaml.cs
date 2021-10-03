using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Countdown.Views
{
    [Microsoft.UI.Xaml.Markup.ContentProperty(Name = "ChildContent")]
    public sealed partial class GroupBorder : UserControl
    {
        private readonly Compositor compositor;
        private readonly ContainerVisual containerVisual;
        private Size previousSize = Size.Empty;

        private const float cCornerRadius = 4.0f;   // make a dp along with stroke size??


        public GroupBorder()              // TODO check dpi changes, force measure pass InvalidateMeasure ?
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

                    GroupBorder gb = (GroupBorder)s;
                    gb.containerVisual.Children.RemoveAll();
                    gb.containerVisual.Children.InsertAtBottom(CreateBorderRoundedRect());
                }
            };
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
            new PropertyMetadata(string.Empty));

        public string Heading
        {
            get { return (string)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }


        public static readonly DependencyProperty GroupBorderColourProperty =
              DependencyProperty.Register("GroupBorderColour",
              typeof(Color),
              typeof(GroupBorder),
              new PropertyMetadata(Colors.LightGray));

        public Color GroupBorderColour
        {
            get { return (Color)GetValue(GroupBorderColourProperty); }
            set { SetValue(GroupBorderColourProperty, value); }
        }

        private ShapeVisual CreateBorderRoundedRect()
        {
            // space between the end of the border line and the start of the text
            const float cTextStartPadding = 2.0f;
            // space between the end of the text and the start of the border line
            const float cTextEndPadding = 4.0f;

            const float cBorderStrokeThickness = 1.0f;
            const float cHalfStrokeThickness = cBorderStrokeThickness * 0.5f;

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
            builder.BeginFigure(textRHS, fontCenter, CanvasFigureFill.DoesNotAffectFills);
            builder.AddLine(ActualSize.X - (cCornerRadius + cHalfStrokeThickness), fontCenter);

            // top right corner
            Vector2 arcEnd = new Vector2(ActualSize.X - cHalfStrokeThickness, fontCenter + cCornerRadius + cHalfStrokeThickness);
            builder.AddArc(arcEnd, cCornerRadius, cCornerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            builder.AddLine(arcEnd.X, ActualSize.Y - (cCornerRadius + cHalfStrokeThickness));

            // bottom right corner
            arcEnd = new Vector2(ActualSize.X - (cCornerRadius + cHalfStrokeThickness), ActualSize.Y - cHalfStrokeThickness);
            builder.AddArc(arcEnd, cCornerRadius, cCornerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            builder.AddLine(cCornerRadius + cHalfStrokeThickness, arcEnd.Y);

            // bottom left corner
            arcEnd = new Vector2(cHalfStrokeThickness, ActualSize.Y - (cCornerRadius + cHalfStrokeThickness));
            builder.AddArc(arcEnd, cCornerRadius, cCornerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            builder.AddLine(arcEnd.X, fontCenter + cCornerRadius + cHalfStrokeThickness);

            // top left corner
            arcEnd = new Vector2(cCornerRadius + cHalfStrokeThickness, fontCenter);
            builder.AddArc(arcEnd, cCornerRadius, cCornerRadius, 0f, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);

            builder.AddLine(textLHS, arcEnd.Y);
            builder.EndFigure(CanvasFigureLoop.Open);

            // create a composition geometry from the canvas path data
            CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(builder);
            CompositionPathGeometry pathGeometry = compositor.CreatePathGeometry();
            pathGeometry.Path = new CompositionPath(canvasGeometry);

            // create a shape from the geometry
            CompositionSpriteShape spriteShape = compositor.CreateSpriteShape(pathGeometry);
            spriteShape.FillBrush = compositor.CreateColorBrush(Colors.Transparent);
            spriteShape.StrokeThickness = cBorderStrokeThickness;
            spriteShape.StrokeBrush = compositor.CreateColorBrush(GroupBorderColour);

            // create a visual for the shape
            ShapeVisual shapeVisual = compositor.CreateShapeVisual();
            shapeVisual.Size = ActualSize;
            shapeVisual.Shapes.Add(spriteShape);

            return shapeVisual;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);

            // Using the System to measure the text height isn't ideal, but it
            // will always work. This results in another measure pass, but that 
            // isn't a particularly expensive operation.
            const double inset = 1;
            ChildPresenter.Padding = new Thickness(cCornerRadius + inset, HeadingText.DesiredSize.Height + inset, cCornerRadius + inset, cCornerRadius + inset);

            return size;
        }
    }
}

