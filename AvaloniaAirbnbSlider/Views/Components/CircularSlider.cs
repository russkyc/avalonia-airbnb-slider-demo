using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaAirbnbSlider.Views.Components;

public class CircularSlider : Canvas
{
    // Visual Controls
    private readonly Path _trackPath;
    private readonly Path _ticksPath;
    private readonly Path _glowPath;
    private readonly Path _activeArcPath;
    private readonly Path _centerCapPath;
    private readonly Path _handlePath;

    private readonly Grid _textOverlay;
    private readonly TextBlock _monthNumberText;
    private readonly TextBlock _monthLabelText;

    // State Fields
    private int _month = 4;
    private double _t = 4.0 / 12.0;
    private bool _isDragging;
    private const double HandleSize = 48;

    public CircularSlider()
    {
        // Initialize visual elements
        _trackPath = CreateTrackLayer();
        _ticksPath = CreateTicksLayer();
        _glowPath = CreateGlowLayer();
        _activeArcPath = CreateActiveArcLayer();
        _centerCapPath = CreateCenterCapLayer();
        _handlePath = CreateHandleLayer();
        _textOverlay = CreateTextOverlay(out _monthNumberText, out _monthLabelText);

        // Add to Visual Tree in Z-Order (Bottom to Top)
        Children.Add(_trackPath);
        Children.Add(_ticksPath);
        Children.Add(_glowPath);
        Children.Add(_activeArcPath);
        Children.Add(_centerCapPath);
        Children.Add(_textOverlay);
        Children.Add(_handlePath);

        // Initial State
        UpdateTextDisplay();
        UpdateGeometry();
    }

    private Path CreateTrackLayer()
    {
        return new Path
        {
            Fill = new RadialGradientBrush
            {
                GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                GradientStops =
                [
                    new GradientStop(Color.Parse("#F6F6F6"), 0.0),
                    new GradientStop(Color.Parse("#F6F6F6"), 0.80),
                    new GradientStop(Color.Parse("#CCCCCC"), 1.0)
                ]
            }
        };
    }

    private Path CreateTicksLayer()
    {
        return new Path { Fill = new SolidColorBrush(Color.Parse("#40000000")) };
    }

    private Path CreateGlowLayer()
    {
        return new Path
        {
            Fill = new SolidColorBrush(Color.Parse("#FF385C")),
            Opacity = 0.4,
            Effect = new DropShadowEffect
            {
                BlurRadius = 30, Color = Color.Parse("#FF385C"), Opacity = 1.0, OffsetX = 0, OffsetY = 0
            }
        };
    }

    private Path CreateActiveArcLayer()
    {
        return new Path
        {
            Effect = new DropShadowEffect
            {
                BlurRadius = 35, Color = Color.Parse("#FF0036"), Opacity = 0.9, OffsetX = 0, OffsetY = 0
            }
        };
    }

    private Path CreateCenterCapLayer()
    {
        return new Path
        {
            Fill = Brushes.White,
            Effect = new DropShadowEffect
            {
                BlurRadius = 15, Color = Colors.Black, Opacity = 0.15, OffsetX = 0, OffsetY = 2
            }
        };
    }

    private Path CreateHandleLayer()
    {
        return new Path
        {
            Data = new EllipseGeometry(new Rect(0, 0, HandleSize, HandleSize)),
            Fill = new RadialGradientBrush
            {
                GradientOrigin = new RelativePoint(0.5, 0.2, RelativeUnit.Relative),
                Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
                GradientStops = [new GradientStop(Colors.White, 0.0), new GradientStop(Color.Parse("#EEEEEE"), 1.0)]
            },
            RenderTransform = new ScaleTransform(1.0, 1.0),
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            Effect = new DropShadowEffect
            {
                BlurRadius = 8, Color = Colors.Black, Opacity = 0.3, OffsetX = 0, OffsetY = 2
            }
        };
    }

    private Grid CreateTextOverlay(out TextBlock numberText, out TextBlock labelText)
    {
        numberText = new TextBlock
        {
            Text = _month.ToString(),
            FontSize = 80,
            LineHeight = 80,
            FontWeight = FontWeight.ExtraBold,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        labelText = new TextBlock
        {
            Text = "months",
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, -5, 0, 0)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { numberText, labelText },
            IsVisible = true
        };

        var grid = new Grid { IsHitTestVisible = false };
        grid.Children.Add(stack);
        return grid;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty) UpdateGeometry();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isDragging = true;
        e.Pointer.Capture(this);
        SetHandlePressedState(true);
        UpdateDragPosition(e.GetPosition(this));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isDragging) UpdateDragPosition(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
        SetHandlePressedState(false);
        SnapToNearestMonth();
    }

    private void SetHandlePressedState(bool isPressed)
    {
        if (_handlePath.RenderTransform is ScaleTransform scale)
        {
            var targetScale = isPressed ? 0.93 : 1.0;
            scale.ScaleX = targetScale;
            scale.ScaleY = targetScale;
        }

        if (_handlePath.Effect is DropShadowEffect shadow)
        {
            shadow.BlurRadius = isPressed ? 3 : 8;
            shadow.OffsetY = isPressed ? 1 : 2;
        }
    }

    private void UpdateDragPosition(Point pos)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var delta = center - pos;

        var angle = Math.Atan2(delta.Y, delta.X) - Math.PI / 2;
        if (angle < 0) angle += 2 * Math.PI;

        var rawT = angle / (2 * Math.PI);

        // Wrap-around logic
        if (_t > 0.9 && rawT < 0.1) rawT = 1.0;
        else if (_t < 0.1 && rawT > 0.9) rawT = 0.0;

        // Clamp logic
        var minT = 1.0 / 12.0;
        if (rawT < minT) rawT = minT;
        if (rawT > 1.0) rawT = 1.0;

        _t = rawT;
        CheckMagneticSnap(rawT);
        UpdateGeometry();
    }

    private void CheckMagneticSnap(double rawT)
    {
        var totalMonths = 12.0;
        var currentMonthFloat = rawT * totalMonths;
        var nearestMonthInt = (int)Math.Round(currentMonthFloat);
        var distanceToSnap = Math.Abs(currentMonthFloat - nearestMonthInt);

        const double snapThreshold = 0.3;
        if (distanceToSnap < snapThreshold)
        {
            _month = Math.Clamp(nearestMonthInt, 1, 12);
            UpdateTextDisplay();
        }
    }

    private void SnapToNearestMonth()
    {
        var totalMonths = 12.0;
        _month = Math.Clamp((int)Math.Round(_t * totalMonths), 1, 12);
        _t = _month / totalMonths;

        UpdateTextDisplay();
        UpdateGeometry();
    }

    private void UpdateTextDisplay()
    {
        _monthNumberText.Text = _month.ToString();
        _monthLabelText.Text = _month == 1 ? "month" : "months";
    }

    private void UpdateGeometry()
    {
        if (Bounds.Width == 0 || Bounds.Height == 0) return;

        // 1. Calculate Shared Metrics
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        double innerRadius = 90;
        double outerRadius = 150;
        var arcRadius = innerRadius + (outerRadius - innerRadius) / 2;

        // 2. Delegate to component updaters
        UpdateActiveArcGradient(center, outerRadius);
        UpdateTrackGeometry(center, innerRadius, outerRadius);
        UpdateActiveArcGeometry(center, innerRadius, outerRadius);
        UpdateCenterCapGeometry(center, innerRadius);
        UpdateHandlePosition(center, arcRadius);
        UpdateTicksGeometry(center, arcRadius);

        // 3. Resize Overlay
        _textOverlay.Width = Bounds.Width;
        _textOverlay.Height = Bounds.Height;
    }

    private void UpdateActiveArcGradient(Point center, double outerRadius)
    {
        var gradientRadius = outerRadius * 1.3;
        _activeArcPath.Fill = new RadialGradientBrush
        {
            Center = new RelativePoint(center.X, center.Y, RelativeUnit.Absolute),
            GradientOrigin = new RelativePoint(center.X, center.Y - 50, RelativeUnit.Absolute),
            RadiusX = new RelativeScalar(gradientRadius, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(gradientRadius, RelativeUnit.Absolute),
            GradientStops =
                [new GradientStop(Color.Parse("#FF4D75"), 0.0), new GradientStop(Color.Parse("#E10036"), 1.0)]
        };
    }

    private void UpdateTrackGeometry(Point center, double innerRadius, double outerRadius)
    {
        _trackPath.Data = CreateRingGeometry(center, innerRadius, outerRadius);
    }

    private void UpdateActiveArcGeometry(Point center, double innerRadius, double outerRadius)
    {
        if (_t >= 0.9999)
        {
            // Full Circle: Re-use Ring Geometry logic
            var fullRing = CreateRingGeometry(center, innerRadius, outerRadius);
            _activeArcPath.Data = fullRing;
            _glowPath.Data = fullRing;
        }
        else if (_t <= 0.001)
        {
            _activeArcPath.Data = null;
            _glowPath.Data = null;
        }
        else
        {
            // Partial Arc
            var startAngle = -Math.PI / 2;
            var endAngle = startAngle + _t * 2 * Math.PI;
            var arcGeo = CreateDonutArcGeometry(center, innerRadius, outerRadius, startAngle, endAngle, true);
            _activeArcPath.Data = arcGeo;
            _glowPath.Data = arcGeo;
        }
    }

    private void UpdateCenterCapGeometry(Point center, double innerRadius)
    {
        _centerCapPath.Data = new EllipseGeometry(new Rect(center.X - innerRadius, center.Y - innerRadius,
            innerRadius * 2, innerRadius * 2));
    }

    private void UpdateHandlePosition(Point center, double arcRadius)
    {
        var handleAngle = -Math.PI / 2 + _t * 2 * Math.PI;
        var handleCenterPos = new Point(center.X + Math.Cos(handleAngle) * arcRadius,
            center.Y + Math.Sin(handleAngle) * arcRadius);

        SetLeft(_handlePath, handleCenterPos.X - HandleSize / 2);
        SetTop(_handlePath, handleCenterPos.Y - HandleSize / 2);
    }

    private void UpdateTicksGeometry(Point center, double arcRadius)
    {
        var ticksGroup = new GeometryGroup();
        for (var i = 0; i < 12; i++)
        {
            var tickAngle = i / 12.0 * 2 * Math.PI - Math.PI / 2;
            var dotPos = new Point(center.X + Math.Cos(tickAngle) * arcRadius,
                center.Y + Math.Sin(tickAngle) * arcRadius);
            ticksGroup.Children.Add(new EllipseGeometry(new Rect(dotPos.X - 2, dotPos.Y - 2, 4, 4)));
        }

        _ticksPath.Data = ticksGroup;
    }

    private Geometry CreateRingGeometry(Point center, double innerRadius, double outerRadius)
    {
        var outer = new EllipseGeometry(new Rect(center.X - outerRadius, center.Y - outerRadius, outerRadius * 2,
            outerRadius * 2));
        var inner = new EllipseGeometry(new Rect(center.X - innerRadius, center.Y - innerRadius, innerRadius * 2,
            innerRadius * 2));
        return new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner);
    }

    private Geometry CreateDonutArcGeometry(Point center, double innerRadius, double outerRadius, double startAngle,
        double endAngle, bool roundCaps)
    {
        var geometry = new PathGeometry { FillRule = FillRule.NonZero };

        var outerStart = new Point(center.X + Math.Cos(startAngle) * outerRadius,
            center.Y + Math.Sin(startAngle) * outerRadius);
        var outerEnd = new Point(center.X + Math.Cos(endAngle) * outerRadius,
            center.Y + Math.Sin(endAngle) * outerRadius);
        var innerStart = new Point(center.X + Math.Cos(startAngle) * innerRadius,
            center.Y + Math.Sin(startAngle) * innerRadius);
        var innerEnd = new Point(center.X + Math.Cos(endAngle) * innerRadius,
            center.Y + Math.Sin(endAngle) * innerRadius);

        var isLargeArc = endAngle - startAngle > Math.PI;
        var capRadius = (outerRadius - innerRadius) / 2.0;
        var capSize = new Size(capRadius, capRadius);

        var figure = new PathFigure();
        figure.StartPoint = outerStart;
        figure.IsClosed = true;

        // Outer Arc
        figure.Segments?.Add(new ArcSegment
        {
            Point = outerEnd, Size = new Size(outerRadius, outerRadius), RotationAngle = 0, IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise
        });

        // End Cap
        if (roundCaps)
            figure.Segments?.Add(new ArcSegment
            {
                Point = innerEnd, Size = capSize, RotationAngle = 0, IsLargeArc = false,
                SweepDirection = SweepDirection.Clockwise
            });
        else figure.Segments?.Add(new LineSegment { Point = innerEnd });

        // Inner Arc
        figure.Segments?.Add(new ArcSegment
        {
            Point = innerStart, Size = new Size(innerRadius, innerRadius), RotationAngle = 0,
            IsLargeArc = isLargeArc, SweepDirection = SweepDirection.CounterClockwise
        });

        // Start Cap
        if (roundCaps)
            figure.Segments?.Add(new ArcSegment
            {
                Point = outerStart, Size = capSize, RotationAngle = 0, IsLargeArc = false,
                SweepDirection = SweepDirection.Clockwise
            });

        geometry.Figures?.Add(figure);
        return geometry;
    }
}