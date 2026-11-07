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
		public const int CELL_SIZE = 20;
		public const int CURRENT_TIMER_INTERVAL = 100;

		public static int FieldRows;
		public static int FieldCols;

		public static readonly SolidColorBrush lifeColor = Brushes.Black;
		public static readonly SolidColorBrush deadColor = Brushes.White;

		public static GameRule CurrentRule = GameRule.Parse("B345/S4567");
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

		bool[] field;
		bool[] nextField;
		Rectangle[,] cells;

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
			// 1. Размеры поля под доступную область экрана
			ComputeFieldSize();

			int size = GameSettings.FieldRows * GameSettings.FieldCols;
			field = new bool[size];
			nextField = new bool[size];

			cells = new Rectangle[GameSettings.FieldRows, GameSettings.FieldCols];

			// 2. Дальше — как раньше: плитка, прямоугольники, отрисовка
			GridBrush.Viewport = new Rect(0, 0, GameSettings.CELL_SIZE, GameSettings.CELL_SIZE);
			GridCell.Width = GameSettings.CELL_SIZE;
			GridCell.Height = GameSettings.CELL_SIZE;
			GridRect.Width = GameSettings.CELL_SIZE;
			GridRect.Height = GameSettings.CELL_SIZE;

			CreateCells();
			Redraw();
		}

		private void ComputeFieldSize()
		{
			// Рабочая область экрана (без панели задач), в DIP
			Rect workArea = SystemParameters.WorkArea;

			// Оставляем запас под кнопку/слайдер сверху и возможные отступы
			const double topReserve = 40;   // под UI-панели
			const double margin = 20;

			double availableWidth = workArea.Width - margin;
			double availableHeight = workArea.Height - topReserve;

			GameSettings.FieldCols = (int)(availableWidth / GameSettings.CELL_SIZE);
			GameSettings.FieldRows = (int)(availableHeight / GameSettings.CELL_SIZE);

			// На всякий случай — минимум 10×10
			if (GameSettings.FieldCols < 10) GameSettings.FieldCols = 10;
			if (GameSettings.FieldRows < 10) GameSettings.FieldRows = 10;
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
			int row = lastMouseRow >= 0 ? lastMouseRow : GameSettings.FieldRows / 2;
			int col = lastMouseCol >= 0 ? lastMouseCol : GameSettings.FieldCols / 2;

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
			for (int row = 0; row < GameSettings.FieldRows; row++)
			{
				for (int col = 0; col < GameSettings.FieldCols; col++)
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

			for (int row = 0; row < GameSettings.FieldRows; row++)
			{
				for (int col = 0; col < GameSettings.FieldCols; col++)
				{
					DrawCell(row, col);
				}
			}
		}

		private void DrawCell(int row, int col)
		{
			cells[row, col].Fill = field[Idx(row, col)] ? GameSettings.lifeColor : GameSettings.deadColor;
		}

		private void TimerTick(object sender, EventArgs e)
		{
			for (int row = 0; row < GameSettings.FieldRows; row++)
			{
				for (int col = 0; col < GameSettings.FieldCols; col++)
				{
					int i = Idx(row, col);
					int neighbors = CountAliveNeighbors(row, col);
					bool isCurrentCellAlive = field[i];

					var rule = GameSettings.CurrentRule;
					bool nextAlive = isCurrentCellAlive ? rule.Survive[neighbors] : rule.Birth[neighbors];

					nextField[i] = nextAlive;
				}
			}

			for (int row = 0; row < GameSettings.FieldRows; row++)
			{
				for (int col = 0; col < GameSettings.FieldCols; col++)
				{
					int i = Idx(row, col);
					if (nextField[i] != field[i])
					{
						field[i] = nextField[i];
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
			int r1 = Math.Min(GameSettings.FieldRows - 1, row + 1);
			int c0 = Math.Max(0, col - 1);
			int c1 = Math.Min(GameSettings.FieldCols - 1, col + 1);

			int neighbors = 0;
			for (int r = r0; r <= r1; r++)
			{
				int rowOffset = r * GameSettings.FieldCols;
				for (int c = c0; c <= c1; c++)
				{
					if (r == row && c == col) continue;
					if (field[rowOffset + c]) neighbors++;
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

					if (r < 0 || r >= GameSettings.FieldRows) continue;
					if (c < 0 || c >= GameSettings.FieldCols) continue;

					field[Idx(r, c)] = true;
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

			if (row < 0 || row >= GameSettings.FieldRows) return;
			if (col < 0 || col >= GameSettings.FieldCols) return;

			// Ctrl зажат → стираем, иначе → рисуем
			drawValue = (Keyboard.Modifiers & ModifierKeys.Control) == 0;

			isDrawing = true;

			field[Idx(row, col)] = drawValue;
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

			if (row < 0 || row >= GameSettings.FieldRows) return;
			if (col < 0 || col >= GameSettings.FieldCols) return;

			int i = Idx(row, col);
			if (field[i] != drawValue)
			{
				field[i] = drawValue;
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

		private static int Idx(int row, int col)
		{
			return row * GameSettings.FieldCols + col;
		}
	}

	public sealed class GameRule
	{
		public bool[] Birth = new bool[9];    // Birth[n] = true, если рождается при n соседях
		public bool[] Survive = new bool[9];  // Survive[n] = true, если выживает при n соседях

		public static GameRule Parse(string rule)
		{
			if (string.IsNullOrWhiteSpace(rule))
				throw new FormatException("Правило пустое");

			rule = rule.Replace(" ", "").ToUpperInvariant();

			string[] parts = rule.Split('/');
			if (parts.Length != 2)
				throw new FormatException("Ожидается формат B.../S..., например B3/S23");

			string birthPart = parts[0];
			string survivePart = parts[1];

			if (!birthPart.StartsWith("B"))
				throw new FormatException("Левая часть должна начинаться с B");
			if (!survivePart.StartsWith("S"))
				throw new FormatException("Правая часть должна начинаться с S");

			birthPart = birthPart.Substring(1);
			survivePart = survivePart.Substring(1);

			var result = new GameRule();

			foreach (char c in birthPart)
			{
				if (c < '0' || c > '8')
					throw new FormatException($"Недопустимая цифра в B-части: {c}");
				result.Birth[c - '0'] = true;
			}

			foreach (char c in survivePart)
			{
				if (c < '0' || c > '8')
					throw new FormatException($"Недопустимая цифра в S-части: {c}");
				result.Survive[c - '0'] = true;
			}

			return result;
		}
	}
}
