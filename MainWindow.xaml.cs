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
		public const int CELL_SIZE = 10;
		public const int FIELD_ROWS = 100;
		public const int FIELD_COLS = 100;
		public const int CURRENT_TIMER_INTERVAL = 100;

		public static readonly SolidColorBrush lifeColor = Brushes.Black;
		public static readonly SolidColorBrush deadColor = Brushes.White;
	}

	public static class Patterns
	{
		public static readonly string[] Glider =
		{
			" X ",
			"  X",
			"XXX"
		};

		public static readonly string[] LWSS =
		{
			" X  X",
			"X    ",
			"X   X",
			"XXXX "
		};

		public static readonly string[] Blinker =
		{
			"XXX"
		};

		public static readonly string[] Block =
		{
			"XX",
			"XX"
		};
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

		Rectangle[,] cells = new Rectangle[GameSettings.FIELD_ROWS, GameSettings.FIELD_COLS];

		private int lastMouseRow = -1;
		private int lastMouseCol = -1;

		private bool isPaused = true;
		private bool isDrawing = false;
		private bool drawValue;


		public MainWindow()
		{
			InitializeComponent();

			//AllocConsole();

			timer = new DispatcherTimer();
			timer.Tick += TimerTick;
			timer.Interval = TimeSpan.FromMilliseconds(GameSettings.CURRENT_TIMER_INTERVAL);

			CanvasArea.Loaded += Init;
			this.PreviewKeyDown += Window_PreviewKeyDown;

			SpeedSlider.Value = GameSettings.CURRENT_TIMER_INTERVAL;
		}

		private void Init(object sender, RoutedEventArgs e)
		{
			// 1. Устанавливаем область повторения плитки (X, Y, Width, Height)
			GridBrush.Viewport = new Rect(0, 0, GameSettings.CELL_SIZE, GameSettings.CELL_SIZE);

			// 2. Явно задаем размеры для контейнера внутри плитки
			GridCell.Width = GameSettings.CELL_SIZE;
			GridCell.Height = GameSettings.CELL_SIZE;

			// Если внутри лежит Rectangle, можно задать размеры и ему
			GridRect.Width = GameSettings.CELL_SIZE;
			GridRect.Height = GameSettings.CELL_SIZE;

			// 3. Создаём прямоугольники для всех клеток один раз
			CreateCells();

			// 4. Рисуем начальное (пустое) состояние
			Redraw();
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space)
			{
				TogglePause();
				e.Handled = true;
				return;
			}

			if (e.Key == Key.D1 || e.Key == Key.NumPad1)
			{
				InsertAtMouseOrCenter(Patterns.Glider);
				e.Handled = true;
			}
			else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
			{
				InsertAtMouseOrCenter(Patterns.LWSS);
				e.Handled = true;
			}
			else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
			{
				InsertAtMouseOrCenter(Patterns.Blinker);
				e.Handled = true;
			}
			else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
			{
				InsertAtMouseOrCenter(Patterns.Block);
				e.Handled = true;
			}
		}

		private void InsertAtMouseOrCenter(string[] pattern)
		{
			int row = lastMouseRow >= 0 ? lastMouseRow : GameSettings.FIELD_ROWS / 2;
			int col = lastMouseCol >= 0 ? lastMouseCol : GameSettings.FIELD_COLS / 2;

			InsertPattern(pattern, row, col);
		}

		private void TogglePause()
		{
			isPaused = !isPaused;

			if (isPaused)
			{
				timer.Stop();
				PauseLabel.Visibility = Visibility.Visible;
			}
			else
			{
				timer.Start();
				PauseLabel.Visibility = Visibility.Collapsed;
			}
		}

		private void CreateCells()
		{
			for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			{
				for (int col = 0; col < GameSettings.FIELD_COLS; col++)
				{
					double x = col * GameSettings.CELL_SIZE;
					double y = row * GameSettings.CELL_SIZE;

					Rectangle rect = new Rectangle
					{
						Width = GameSettings.CELL_SIZE - 2,
						Height = GameSettings.CELL_SIZE - 2,
						Fill = Brushes.White
					};

					Canvas.SetLeft(rect, x + 1);
					Canvas.SetTop(rect, y + 1);

					CanvasArea.Children.Add(rect);
					cells[row, col] = rect;
				}
			}
		}

		private void Redraw()
		{

			for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			{
				for (int col = 0; col < GameSettings.FIELD_COLS; col++)
				{
					DrawCell(row, col);
				}
			}
		}

		private void DrawCell(int row, int col)
		{
			cells[row, col].Fill = field[row, col] ? GameSettings.lifeColor : GameSettings.deadColor;
		}

		private void TimerTick(object sender, EventArgs e)
		{
			for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			{
				for (int col = 0; col < GameSettings.FIELD_COLS; col++)
				{
					int neighbors = CountAliveNeighbors(row, col);
					bool isCurrentCellAlive = field[row, col];

					bool nextAlive = isCurrentCellAlive
						? (neighbors == 2 || neighbors == 3)
						: (neighbors == 3);

					nextField[row, col] = nextAlive;
				}
			}

			// Копируем и перекрашиваем только изменившиеся
			for (int row = 0; row < GameSettings.FIELD_ROWS; row++)
			{
				for (int col = 0; col < GameSettings.FIELD_COLS; col++)
				{
					if (nextField[row, col] != field[row, col])
					{
						field[row, col] = nextField[row, col];
						DrawCell(row, col);
					}
				}
			}
		}

		private void ButtonRestartHandler(object sender, RoutedEventArgs e)
		{
			isPaused = true;
			timer.Stop();
			PauseLabel.Visibility = Visibility.Visible;

			Array.Clear(field, 0, field.Length);
			Array.Clear(nextField, 0, nextField.Length);

			Redraw();
		}

		private int GetCell(double position)
		{
			return (int)(position / GameSettings.CELL_SIZE);
		}

		private int CountAliveNeighbors(int row, int col)
		{
			int r0 = Math.Max(0, row - 1);
			int r1 = Math.Min(GameSettings.FIELD_ROWS - 1, row + 1);
			int c0 = Math.Max(0, col - 1);
			int c1 = Math.Min(GameSettings.FIELD_COLS - 1, col + 1);

			int neighbors = 0;
			for (int r = r0; r <= r1; r++)
			{
				for (int c = c0; c <= c1; c++)
				{
					if (r == row && c == col) continue;
					if (field[r, c]) neighbors++;
				}
			}
			return neighbors;
		}


		private void InsertPattern(string[] pattern, int startRow, int startCol)
		{
			for (int dr = 0; dr < pattern.Length; dr++)
			{
				string line = pattern[dr];

				for (int dc = 0; dc < line.Length; dc++)
				{
					if (line[dc] != 'X') continue;

					int r = startRow + dr;
					int c = startCol + dc;

					if (r < 0 || r >= GameSettings.FIELD_ROWS) continue;
					if (c < 0 || c >= GameSettings.FIELD_COLS) continue;

					field[r, c] = true;
					DrawCell(r, c);
				}
			}
		}


		private void MouseDownHandler(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) return;

			Point position = e.GetPosition(CanvasArea);

			int col = GetCell(position.X);
			int row = GetCell(position.Y);

			if (row < 0 || row >= GameSettings.FIELD_ROWS) return;
			if (col < 0 || col >= GameSettings.FIELD_COLS) return;

			// Ctrl зажат → стираем, иначе → рисуем
			drawValue = (Keyboard.Modifiers & ModifierKeys.Control) == 0;

			isDrawing = true;

			field[row, col] = drawValue;
			DrawCell(row, col);
		}

		private void MouseMoveHandler(object sender, MouseEventArgs e)
		{
			Point position = e.GetPosition(CanvasArea);
			lastMouseCol = GetCell(position.X);
			lastMouseRow = GetCell(position.Y);

			if (!isDrawing) return;
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				EndDrawing();
				return;
			}

			// Если пользователь зажал/отпустил Ctrl по ходу движения — меняем режим
			drawValue = (Keyboard.Modifiers & ModifierKeys.Control) == 0;

			int col = GetCell(position.X);
			int row = GetCell(position.Y);

			if (row < 0 || row >= GameSettings.FIELD_ROWS) return;
			if (col < 0 || col >= GameSettings.FIELD_COLS) return;

			if (field[row, col] != drawValue)
			{
				field[row, col] = drawValue;
				DrawCell(row, col);
			}
		}

		private void MouseUpHandler(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				EndDrawing();
		}

		private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (timer == null) return;

			double ms = SpeedSlider.Value;
			timer.Interval = TimeSpan.FromMilliseconds(ms);

			if (SpeedLabel != null)
				SpeedLabel.Text = $"{ms:0} мс";
		}

		private void EndDrawing()
		{
			if (!isDrawing) return;

			isDrawing = false;
		}
	}
}
