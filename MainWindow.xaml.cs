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
using System.Windows.Threading;

namespace GameOfLife
{
	public static class GameSettings
	{
		public const int CELL_SIZE = 30;
		public const int CURRENT_TIMER_INTERVAL = 100;
	}

	public partial class MainWindow : Window
	{
		private DispatcherTimer timer;

		public MainWindow()
		{
			InitializeComponent();
			CanvasArea.Loaded += Init;
		}

		private void Init(object sender, RoutedEventArgs e)
		{
			//timer = new DispatcherTimer();
			//timer.Tick += TimerTick;
			//timer.Interval = TimeSpan.FromMilliseconds(GameSettings.CURRENT_TIMER_INTERVAL);
			//timer.Start();

			for (int i = 0; i < 10; i++)
			{
				Rectangle rect = new Rectangle
				{
					Width = GameSettings.CELL_SIZE,                  // Ширина
					Height = GameSettings.CELL_SIZE,                 // Высота
					Fill = Brushes.SkyBlue,       // Цвет заливки

					Stroke = Brushes.GreenYellow,
				};

				// 2. Задаем координаты X и Y относительно Canvas
				Canvas.SetLeft(rect, i * (GameSettings.CELL_SIZE + 20));     // Отступ слева (X)
				Canvas.SetTop(rect, 0);          // Отступ сверху (Y)

				// 3. Добавляем прямоугольник на холст
				CanvasArea.Children.Add(rect);
			}
		}

		private void TimerTick(object sender, EventArgs e)
		{
			
		}
	}
}
