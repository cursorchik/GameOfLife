using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
		public const int FIELD_ROWS = 50;
		public const int FIELD_COLS = 50;
		public const int CURRENT_TIMER_INTERVAL = 1000;
	}

	public partial class MainWindow : Window
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool FreeConsole();

		private DispatcherTimer timer;

		bool[,] field = new bool[GameSettings.FIELD_ROWS, GameSettings.FIELD_COLS];
		bool[,] nextField = new bool[GameSettings.FIELD_ROWS, GameSettings.FIELD_COLS];

		public MainWindow()
		{
			InitializeComponent();

			AllocConsole();

			CanvasArea.Loaded += Init;
		}

		private void Init(object sender, RoutedEventArgs e)
		{
			// 1. Устанавливаем область повторения плитки (X, Y, Width, Height)
			GridBrush.Viewport = new Rect(0, 0, GameSettings.CELL_SIZE, GameSettings.CELL_SIZE);

			// 2. Явно задаем размеры для контейнера внутри плитки
			GridCell.Width = GameSettings.CELL_SIZE;
			GridCell.Height = GameSettings.CELL_SIZE;

			// (Необязательно) Если внутри лежит Rectangle, можно задать размеры и ему
			GridRect.Width = GameSettings.CELL_SIZE;
			GridRect.Height = GameSettings.CELL_SIZE;

			//for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			//{
			//	for (int col = 0; col < GameSettings.FIELD_COLS; col++)
			//	{
			//		field[row, col] = false;
			//	}
			//}
		}

		private void Start()
		{
			timer = new DispatcherTimer();
			timer.Tick += TimerTick;
			timer.Interval = TimeSpan.FromMilliseconds(GameSettings.CURRENT_TIMER_INTERVAL);
			timer.Start();

			Redraw();
		}

		private void Redraw()
		{
			for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			{
				for (int col = 0; col < GameSettings.FIELD_COLS; col++)
				{
					DrawRectangleInCell(row, col, field[row, col] ? Brushes.Green : Brushes.White);
				}
			}
		}

		private void DrawRectangleInCell(int row, int column, Brush colorBrush)
		{
			double x = column * GameSettings.CELL_SIZE;
			double y = row * GameSettings.CELL_SIZE;

			Rectangle rect = new Rectangle
			{
				Width = GameSettings.CELL_SIZE - 2,
				Height = GameSettings.CELL_SIZE - 2,
				Fill = colorBrush
			};

			Canvas.SetLeft(rect, x + 1);
			Canvas.SetTop(rect, y + 1);

			CanvasArea.Children.Add(rect);
		}

		private void TimerTick(object sender, EventArgs e)
		{
			for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			{
				for (int col = 0; col < GameSettings.FIELD_COLS; col++)
				{
					int neighbors = CountAliveNeighbors(row, col);
					bool isCurrentCellAlive = field[row, col];

					if (isCurrentCellAlive)
					{
						// Правило 1 и 3: Клетка умирает от одиночества (меньше 2) или перенаселения (больше 3)
						// Правило 2: Клетка продолжает жить, если соседей 2 или 3
						nextField[row, col] = (neighbors == 2 || neighbors == 3);
					}
					else
					{
						// Правило 4: В пустой (мертвой) клетке зарождается жизнь, если соседей ровно 3
						nextField[row, col] = (neighbors == 3);
					}
				}
			}
			field = nextField;

			Redraw();
		}

		private void ButtonStartHandler(object sender, RoutedEventArgs e)
		{
			Start();
			ButtonStart.Visibility = Visibility.Collapsed;
		}

		private void MouseDownHandler(object sender, MouseButtonEventArgs e)
		{
			Point position = e.GetPosition(this);

			int col = GetCell(position.X);
			int row = GetCell(position.Y);

			if (ButtonStart.Visibility == Visibility.Visible)
			{
				field[row, col] = true;
				DrawRectangleInCell(row, col, Brushes.Red);
				//Console.WriteLine($"{row}, {col}");
			}
			//else
			//{
			//	Console.WriteLine($"Соседей у ячейки <{row}, {col}> : {CountAliveNeighbors(row, col)}");
			//}
		}

		private int GetCell(double position)
		{
			return (int)(position / GameSettings.CELL_SIZE);
		}

		private int CountAliveNeighbors(int row, int col)
		{
			int neighbors = 0;

			// Надо проверять не выходят ли индексы массива за границы поля

			/**
				x 0 0
				0 0 0
				0 0 0
			**/
			if (field[row - 1, col - 1]) neighbors++;

			/**
				0 x 0
				0 0 0
				0 0 0
			**/
			if (field[row - 1, col]) neighbors++;

			/**
				0 0 x
				0 0 0
				0 0 0
			**/
			if (field[row - 1, col + 1]) neighbors++;

			/**
				0 0 0
				0 0 x
				0 0 0
			**/
			if (field[row, col + 1]) neighbors++;

			/**
				0 0 0
				0 0 0
				0 0 x
			**/
			if (field[row + 1, col + 1]) neighbors++;

			/**
				0 0 0
				0 0 0
				0 x 0
			**/
			if (field[row + 1, col]) neighbors++;

			/**
				0 0 0
				0 0 0
				x 0 0
			**/
			if (field[row + 1, col - 1]) neighbors++;

			/**
				0 0 0
				x 0 0
				0 0 0
			**/
			if (field[row, col - 1]) neighbors++;

			return neighbors;
		}
	}
}
