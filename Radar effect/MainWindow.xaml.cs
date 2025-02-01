using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Radar_effect
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CreateRadarAnimation();
        }

        private void CreateRadarAnimation()
        {
            // Radar animation
            DoubleAnimation rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(5)), // Aylanish davomiyligi
                RepeatBehavior = RepeatBehavior.Forever // Cheksiz takrorlash
            };

            // Animatsiyani aylanish effekti bilan bog'lash
            RotateTransform radarTransform = (RotateTransform)RadarSweep.RenderTransform;
            radarTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
        }

        private void MenuQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
