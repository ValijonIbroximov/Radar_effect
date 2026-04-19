using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace Radar_effect
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point mouseOffset;

        // Radar parametrlari
        private double baseRadarCenterX;
        private double baseRadarCenterY;
        private double radarRadius = 400;

        // Nishonlar ro'yxati
        private List<Canvas> targets = new List<Canvas>();
        private DispatcherTimer targetTimer;
        private Random random = new Random();

        // Kamera (Joystik) taymeri va vektorlar
        private DispatcherTimer cameraTimer;
        private double joystickVectorX = 0;
        private double joystickVectorY = 0;

        // Professional ranglar palitrasi
        private SolidColorBrush pitchBlack = new SolidColorBrush(Colors.Black);
        private SolidColorBrush darkPhosphorGreen = new SolidColorBrush(Color.FromRgb(2, 20, 2));
        private SolidColorBrush gridGreen = new SolidColorBrush(Color.FromRgb(20, 70, 20));
        private SolidColorBrush brightPhosphorGreen = new SolidColorBrush(Color.FromRgb(57, 255, 20));
        private SolidColorBrush alertRed = new SolidColorBrush(Color.FromRgb(255, 60, 60));
        private FontFamily techFont = new FontFamily("Consolas");

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Oq fon chiqib qolmasligi uchun Canvasni to'liq qora qilamiz
            MainCanvas.Background = pitchBlack;

            DrawHighEndRadarSystem();
            StartTargetSimulation();
            StartCameraMovementTimer();
        }

        private void DrawHighEndRadarSystem()
        {
            baseRadarCenterX = MainCanvas.ActualWidth / 2;
            baseRadarCenterY = MainCanvas.ActualHeight / 2;

            // 1. Asosiy radar orqa foni (Qop-qora va to'q yashil)
            Ellipse radarBackground = new Ellipse
            {
                Width = radarRadius * 2,
                Height = radarRadius * 2,
                Fill = darkPhosphorGreen,
                Stroke = brightPhosphorGreen,
                StrokeThickness = 2
            };
            Canvas.SetLeft(radarBackground, baseRadarCenterX - radarRadius);
            Canvas.SetTop(radarBackground, baseRadarCenterY - radarRadius);
            RadarContainer.Children.Add(radarBackground);

            // 2. Mayda o'lchov panjarasi (Polar Grid)
            for (int i = 1; i <= 8; i++) // 8 ta masofa halqasi
            {
                double currentRadius = radarRadius * (i / 8.0);
                Ellipse ring = new Ellipse
                {
                    Width = currentRadius * 2,
                    Height = currentRadius * 2,
                    Stroke = gridGreen,
                    StrokeThickness = (i % 2 == 0) ? 1.5 : 0.5, // Har ikkinchi halqa qalinroq
                    StrokeDashArray = (i % 2 == 0) ? null : new DoubleCollection { 4, 4 }
                };
                Canvas.SetLeft(ring, baseRadarCenterX - currentRadius);
                Canvas.SetTop(ring, baseRadarCenterY - currentRadius);
                RadarContainer.Children.Add(ring);
            }

            // 3. Azimut chiziqlari (Har 10 va 30 gradusda)
            for (int i = 0; i < 360; i += 10)
            {
                bool isMajor = (i % 30 == 0);
                double rad = i * Math.PI / 180;

                // Kichik chiziqchalar (Tick marks) chegarada
                double innerTick = isMajor ? radarRadius - 15 : radarRadius - 5;
                Point p1 = new Point(baseRadarCenterX + innerTick * Math.Cos(rad), baseRadarCenterY + innerTick * Math.Sin(rad));
                Point p2 = new Point(baseRadarCenterX + radarRadius * Math.Cos(rad), baseRadarCenterY + radarRadius * Math.Sin(rad));

                Line tickLine = new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = brightPhosphorGreen, StrokeThickness = isMajor ? 2 : 1 };
                RadarContainer.Children.Add(tickLine);

                // Asosiy kesib o'tuvchi chiziqlar va matnlar
                if (isMajor)
                {
                    Line azLine = new Line
                    {
                        X1 = baseRadarCenterX,
                        Y1 = baseRadarCenterY,
                        X2 = baseRadarCenterX + radarRadius * Math.Cos(rad),
                        Y2 = baseRadarCenterY + radarRadius * Math.Sin(rad),
                        Stroke = gridGreen,
                        StrokeThickness = 1
                    };
                    RadarContainer.Children.Add(azLine);

                    TextBlock degreeText = new TextBlock
                    {
                        Text = i.ToString("000"),
                        Foreground = brightPhosphorGreen,
                        FontFamily = techFont,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold
                    };
                    double textX = baseRadarCenterX + (radarRadius + 20) * Math.Cos(rad) - 10;
                    double textY = baseRadarCenterY + (radarRadius + 20) * Math.Sin(rad) - 8;
                    Canvas.SetLeft(degreeText, textX);
                    Canvas.SetTop(degreeText, textY);
                    RadarContainer.Children.Add(degreeText);
                }
            }

            // 4. Markaziy Reticle (O'qotar/Kuzatuv mo'ljali)
            Path centralCross = new Path { Stroke = brightPhosphorGreen, StrokeThickness = 2 };
            PathGeometry crossGeom = new PathGeometry();
            crossGeom.Figures.Add(new PathFigure(new Point(baseRadarCenterX - 20, baseRadarCenterY), new[] { new LineSegment(new Point(baseRadarCenterX + 20, baseRadarCenterY), true) }, false));
            crossGeom.Figures.Add(new PathFigure(new Point(baseRadarCenterX, baseRadarCenterY - 20), new[] { new LineSegment(new Point(baseRadarCenterX, baseRadarCenterY + 20), true) }, false));
            centralCross.Data = crossGeom;
            RadarContainer.Children.Add(centralCross);

            // 5. HAQIQIY FOSFOR SWEEP (120 ta uzluksiz kesma yordamida o'ta silliq gradient)
            Canvas sweepContainer = new Canvas();

            // So'nuvchi dum (120 gradus)
            for (int i = 0; i < 120; i++)
            {
                double startAngle = -i;
                double endAngle = -(i + 1.5); // Oraliqda oq chiziq qolmasligi uchun biroz ustma-ust tushadi

                Path wedge = CreateWedge(baseRadarCenterX, baseRadarCenterY, radarRadius, startAngle, endAngle);
                wedge.Fill = brightPhosphorGreen;
                // Haqiqiy radarlardek eksponensial so'nish qonuniyati
                wedge.Opacity = Math.Pow((120.0 - i) / 120.0, 3) * 0.7;

                sweepContainer.Children.Add(wedge);
            }

            // Nurning o'tkir kesuvchi old qismi (Leading edge)
            Line leadingEdge = new Line
            {
                X1 = baseRadarCenterX,
                Y1 = baseRadarCenterY,
                X2 = baseRadarCenterX,
                Y2 = baseRadarCenterY - radarRadius, // Tepaga qarab (0 gradus)
                Stroke = new SolidColorBrush(Colors.White), // Yadrosi oq
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = Colors.LimeGreen, BlurRadius = 15, ShadowDepth = 0 } // Kuchli yashil porlash
            };
            sweepContainer.Children.Add(leadingEdge);

            // Aylanuvchi konteynerni sozlash
            RotateTransform radarRotation = new RotateTransform { CenterX = baseRadarCenterX, CenterY = baseRadarCenterY };
            sweepContainer.RenderTransform = radarRotation;
            RadarContainer.Children.Add(sweepContainer);

            // 3 soniyada 360 gradus aylanadi
            DoubleAnimation rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(3),
                RepeatBehavior = RepeatBehavior.Forever
            };
            radarRotation.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
        }

        // Sector (Wedge) chizish uchun yordamchi matematik funksiya
        private Path CreateWedge(double cx, double cy, double radius, double startAngle, double endAngle)
        {
            // Burchakni radianlarga o'tkazish (-90 gradus tepadan boshlanishi uchun)
            double startRad = (startAngle - 90) * Math.PI / 180.0;
            double endRad = (endAngle - 90) * Math.PI / 180.0;

            Point p1 = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad));
            Point p2 = new Point(cx + radius * Math.Cos(endRad), cy + radius * Math.Sin(endRad));

            PathGeometry geom = new PathGeometry();
            PathFigure fig = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
            fig.Segments.Add(new LineSegment(p1, false));
            fig.Segments.Add(new ArcSegment(p2, new Size(radius, radius), 0, false, SweepDirection.Counterclockwise, false));
            geom.Figures.Add(fig);

            return new Path { Data = geom };
        }

        // --- NISHONLAR IMITATSIYASI ---
        private void StartTargetSimulation()
        {
            for (int i = 0; i < 6; i++) GenerateProfessionalTarget();

            targetTimer = new DispatcherTimer();
            targetTimer.Interval = TimeSpan.FromSeconds(1.5);
            targetTimer.Tick += (s, e) =>
            {
                if (random.NextDouble() > 0.7 && targets.Count < 15) GenerateProfessionalTarget();

                foreach (var target in targets)
                {
                    double currentLeft = Canvas.GetLeft(target);
                    double currentTop = Canvas.GetTop(target);
                    Canvas.SetLeft(target, currentLeft + (random.NextDouble() - 0.5) * 8);
                    Canvas.SetTop(target, currentTop + (random.NextDouble() - 0.5) * 8);
                }
            };
            targetTimer.Start();
        }

        private void GenerateProfessionalTarget()
        {
            double angle = random.NextDouble() * 2 * Math.PI;
            double distance = random.NextDouble() * (radarRadius - 60);
            double x = baseRadarCenterX + distance * Math.Cos(angle);
            double y = baseRadarCenterY + distance * Math.Sin(angle);

            Canvas targetCanvas = new Canvas();

            // Nishon vizuali - Harbiy ko'rinishdagi markaziy xoch va kvadrat
            Rectangle blip = new Rectangle
            {
                Width = 10,
                Height = 10,
                Stroke = alertRed,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0)),
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 15, ShadowDepth = 0 }
            };
            Canvas.SetLeft(blip, -5); Canvas.SetTop(blip, -5);

            // Data yorlig'i (Texnik ma'lumotlar)
            string targetId = "TRG-" + random.Next(1000, 9999);
            string alt = "FL" + random.Next(150, 450); // Flight Level
            string spd = "SPD " + random.Next(300, 950);

            TextBlock dataTag = new TextBlock
            {
                Text = $"{targetId}\n{alt} {spd}",
                Foreground = alertRed,
                FontFamily = techFont,
                FontSize = 11,
                LineHeight = 14,
                Opacity = 0.9
            };

            Line tagConnector = new Line { X1 = 5, Y1 = -5, X2 = 25, Y2 = -25, Stroke = alertRed, StrokeThickness = 1, Opacity = 0.6 };
            Canvas.SetLeft(dataTag, 30); Canvas.SetTop(dataTag, -35);

            targetCanvas.Children.Add(tagConnector);
            targetCanvas.Children.Add(blip);
            targetCanvas.Children.Add(dataTag);

            Canvas.SetLeft(targetCanvas, x); Canvas.SetTop(targetCanvas, y);

            // Pulsatsiyalovchi animatsiya
            DoubleAnimation blink = new DoubleAnimation { From = 1.0, To = 0.5, Duration = TimeSpan.FromSeconds(1), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            targetCanvas.BeginAnimation(Canvas.OpacityProperty, blink);

            targets.Add(targetCanvas);
            RadarContainer.Children.Add(targetCanvas);
        }

        // ==========================================
        // JOSTIK VA KAMERA HARAKATI MANTIQI
        // ==========================================

        private void StartCameraMovementTimer()
        {
            cameraTimer = new DispatcherTimer();
            cameraTimer.Interval = TimeSpan.FromMilliseconds(16);
            cameraTimer.Tick += (s, e) =>
            {
                if (joystickVectorX == 0 && joystickVectorY == 0) return;

                double maxSpeed = 15.0;
                double nextX = RadarTranslate.X - (joystickVectorX * maxSpeed);
                double nextY = RadarTranslate.Y - (joystickVectorY * maxSpeed);

                double maxPanX = MainCanvas.ActualWidth / 1.5;
                double maxPanY = MainCanvas.ActualHeight / 1.5;

                if (nextX > maxPanX) nextX = maxPanX;
                if (nextX < -maxPanX) nextX = -maxPanX;
                if (nextY > maxPanY) nextY = maxPanY;
                if (nextY < -maxPanY) nextY = -maxPanY;

                if (RadarTranslate != null)
                {
                    RadarTranslate.X = nextX;
                    RadarTranslate.Y = nextY;
                }
            };
            cameraTimer.Start();
        }

        private void TopCircle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            mouseOffset = e.GetPosition(this);
            ((Ellipse)sender).CaptureMouse();
        }

        private void TopCircle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            Point position = e.GetPosition(this);
            double offsetX = position.X - mouseOffset.X;
            double offsetY = position.Y - mouseOffset.Y;

            double newLeft = Canvas.GetLeft(TopCircle) + offsetX;
            double newTop = Canvas.GetTop(TopCircle) + offsetY;

            double centerX = newLeft + TopCircle.Width / 2;
            double centerY = newTop + TopCircle.Height / 2;

            double baseCenterX = 50;
            double baseCenterY = 50;
            double radius = BaseCircle.Width / 2 - TopCircle.Width / 2;

            double distance = Math.Sqrt(Math.Pow(centerX - baseCenterX, 2) + Math.Pow(centerY - baseCenterY, 2));

            if (distance > radius)
            {
                double angle = Math.Atan2(centerY - baseCenterY, centerX - baseCenterX);
                centerX = baseCenterX + radius * Math.Cos(angle);
                centerY = baseCenterY + radius * Math.Sin(angle);
                newLeft = centerX - TopCircle.Width / 2;
                newTop = centerY - TopCircle.Height / 2;
            }

            Canvas.SetLeft(TopCircle, newLeft); Canvas.SetTop(TopCircle, newTop);
            Canvas.SetLeft(MiddleCircle, (centerX + baseCenterX) / 2 - MiddleCircle.Width / 2);
            Canvas.SetTop(MiddleCircle, (centerY + baseCenterY) / 2 - MiddleCircle.Height / 2);

            ConnectingLine.X1 = baseCenterX; ConnectingLine.Y1 = baseCenterY;
            ConnectingLine.X2 = centerX; ConnectingLine.Y2 = centerY;

            joystickVectorX = (centerX - baseCenterX) / radius;
            joystickVectorY = (centerY - baseCenterY) / radius;
        }

        private void TopCircle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ((Ellipse)sender).ReleaseMouseCapture();

            Canvas.SetLeft(TopCircle, 35); Canvas.SetTop(TopCircle, 35);
            Canvas.SetLeft(MiddleCircle, 35); Canvas.SetTop(MiddleCircle, 35);

            ConnectingLine.X1 = 50; ConnectingLine.Y1 = 50;
            ConnectingLine.X2 = 50; ConnectingLine.Y2 = 50;

            joystickVectorX = 0;
            joystickVectorY = 0;
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void RadioButton_Checked(object sender, RoutedEventArgs e) { }
    }
}