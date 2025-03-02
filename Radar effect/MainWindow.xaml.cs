using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace Radar_effect
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false; // Doirani sudrab olish holati
        private Point mouseOffset; // Sichqoncha bosilgan joy

        public MainWindow()
        {
            InitializeComponent();
            CreateRadarAnimation(); // Radar animatsiyasini yaratish
            CreateTargetAnimation(); // Nishon belgisi animatsiyasini yaratish
        }

        // Radar animatsiyasini yaratish
        private void CreateRadarAnimation()
        {
            // Radar animatsiyasi
            DoubleAnimation rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(5)), // Aylanish davomiyligi
                RepeatBehavior = RepeatBehavior.Forever // Cheksiz takrorlash
            };

            // Animatsiyani radar chizig'iga bog'lash
            RotateTransform radarTransform = (RotateTransform)RadarSweep.RenderTransform;
            radarTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
        }

        // Nishon belgisi animatsiyasini yaratish
        private void CreateTargetAnimation()
        {
            // Nishon belgisi animatsiyasi
            DoubleAnimation targetAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0.5,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            // Animatsiyani nishon belgisi bilan bog'lash
            Ellipse target = (Ellipse)FindName("TargetEllipse");
            target.BeginAnimation(Ellipse.OpacityProperty, targetAnimation);
        }

        // Doirani boshlab sudrab olish
        private void TopCircle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            mouseOffset = e.GetPosition(this);
            ((Ellipse)sender).CaptureMouse();
        }

        // Doirani sudrab harakatlantirish
        private void TopCircle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            Point position = e.GetPosition(this);
            double offsetX = position.X - mouseOffset.X;
            double offsetY = position.Y - mouseOffset.Y;

            double newLeft = Canvas.GetLeft(TopCircle) + offsetX;
            double newTop = Canvas.GetTop(TopCircle) + offsetY;

            // Doiraning markazi 1-doira ichida qolishini ta'minlash
            double centerX = newLeft + TopCircle.Width / 2;
            double centerY = newTop + TopCircle.Height / 2;
            double baseCenterX = Canvas.GetLeft(BaseCircle) + BaseCircle.Width / 2;
            double baseCenterY = Canvas.GetTop(BaseCircle) + BaseCircle.Height / 2;
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

            Canvas.SetLeft(TopCircle, newLeft);
            Canvas.SetTop(TopCircle, newTop);

            // 2-doirani 1-va 3-doira o'rtasiga joylashtirish
            double midX = (centerX + baseCenterX) / 2;
            double midY = (centerY + baseCenterY) / 2;
            Canvas.SetLeft(MiddleCircle, midX - MiddleCircle.Width / 2);
            Canvas.SetTop(MiddleCircle, midY - MiddleCircle.Height / 2);

            // Chiziq koordinatalarini yangilash
            ConnectingLine.X1 = baseCenterX;
            ConnectingLine.Y1 = baseCenterY;
            ConnectingLine.X2 = centerX;
            ConnectingLine.Y2 = centerY;
        }

        // Doirani sudrab olishni to'xtatish
        private void TopCircle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ((Ellipse)sender).ReleaseMouseCapture();
        }

        // Dasturni yopish
        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // RadioButton bosilganda ishlaydigan metod
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Bu metodni kerakli funksiyalar bilan to'ldiring
        }
    }
}