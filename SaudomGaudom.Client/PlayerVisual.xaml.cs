using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SaudomGaudom.Client
{
    /// <summary>
    /// Interaction logic for PlayerVisual.xaml
    /// </summary>
    public partial class PlayerVisual : UserControl
    {
        public PlayerVisual()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PlayerColorProperty =
            DependencyProperty.Register("PlayerColor", typeof(Brush), typeof(PlayerVisual), new PropertyMetadata(Brushes.Red));

        public Brush PlayerColor
        {
            get { return (Brush)GetValue(PlayerColorProperty); }
            set { SetValue(PlayerColorProperty, value); }
        }

        public void UpdateColor(Brush newColor)
        {
            playerRectangle.Fill = newColor;
        }
    }
}
