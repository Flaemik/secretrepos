using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace WpfApp3
{
    public class PuzzleCaptcha
    {
        private static readonly Random Random = new Random();
        private readonly Canvas canvas;
        private readonly List<PuzzlePiece> pieces = new List<PuzzlePiece>();
        private readonly List<Rectangle> slots = new List<Rectangle>();
        private readonly int gridSize = 2;
        private readonly int pieceCount = 4;
        private readonly double pieceSize = 80;
        private bool isCompleted;
        private Point startPoint;
        private PuzzlePiece draggingPiece;
        public event EventHandler<bool> CaptchaCompleted;
        public PuzzleCaptcha(Canvas captchaCanvas)
        {
            canvas = captchaCanvas;
            InitializePuzzle();
        }
        public bool IsCaptchaCompleted()
        {
            return isCompleted;
        }
        public void Reset()
        {
            canvas.Children.Clear();
            pieces.Clear();
            slots.Clear();
            isCompleted = false;
            InitializePuzzle();
        }
        private void InitializePuzzle()
        {
            AddSlots();
            AddPieces();
        }
        private BitmapImage LoadPieceImage(int imageNumber)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri($"pack://application:,,,/CaptchaImages/{imageNumber}.png");
            image.DecodePixelWidth = (int)pieceSize;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        private void AddSlots()
        {
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    Rectangle slot = new Rectangle
                    {
                        Width = pieceSize,
                        Height = pieceSize,
                        Fill = Brushes.WhiteSmoke,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(slot, 10 + col * (pieceSize + 5));
                    Canvas.SetTop(slot, 10 + row * (pieceSize + 5));
                    canvas.Children.Add(slot);
                    slots.Add(slot);
                }
            }
        }
        private void AddPieces()
        {
            List<PuzzlePiece> createdPieces = new List<PuzzlePiece>();
            for (int number = 1; number <= pieceCount; number++)
            {
                int row = (number - 1) / gridSize;
                int col = (number - 1) % gridSize;
                PuzzlePiece piece = new PuzzlePiece
                {
                    Width = pieceSize,
                    Height = pieceSize,
                    CorrectRow = row,
                    CorrectCol = col,
                    CurrentRow = -1,
                    CurrentCol = -1,
                    Content = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.White,
                        Child = new Image
                        {
                            Source = LoadPieceImage(number),
                            Stretch = Stretch.Fill
                        }
                    }
                };
                piece.MouseLeftButtonDown += Piece_MouseLeftButtonDown;
                piece.MouseMove += Piece_MouseMove;
                piece.MouseLeftButtonUp += Piece_MouseLeftButtonUp;
                createdPieces.Add(piece);
            }
            foreach (PuzzlePiece piece in createdPieces.OrderBy(x => Random.Next()))
            {
                int index = pieces.Count;
                piece.HomeLeft = 10 + index * (pieceSize + 8);
                piece.HomeTop = 190;
                Canvas.SetLeft(piece, piece.HomeLeft);
                Canvas.SetTop(piece, piece.HomeTop);
                canvas.Children.Add(piece);
                pieces.Add(piece);
            }
        }
        private void Piece_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            draggingPiece = sender as PuzzlePiece;
            if (draggingPiece == null || isCompleted)
                return;
            draggingPiece.IsPlaced = false;
            draggingPiece.CurrentRow = -1;
            draggingPiece.CurrentCol = -1;
            startPoint = e.GetPosition(canvas);
            draggingPiece.CaptureMouse();
            Canvas.SetZIndex(draggingPiece, 10);
        }
        private void Piece_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingPiece == null || !draggingPiece.IsMouseCaptured)
                return;
            Point currentPoint = e.GetPosition(canvas);
            Canvas.SetLeft(draggingPiece, Canvas.GetLeft(draggingPiece) + currentPoint.X - startPoint.X);
            Canvas.SetTop(draggingPiece, Canvas.GetTop(draggingPiece) + currentPoint.Y - startPoint.Y);
            startPoint = currentPoint;
        }
        private void Piece_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggingPiece == null)
                return;
            draggingPiece.ReleaseMouseCapture();
            TryPlacePiece(draggingPiece);
            Canvas.SetZIndex(draggingPiece, 1);
            draggingPiece = null;
        }
        private void TryPlacePiece(PuzzlePiece piece)
        {
            Rectangle nearestSlot = FindNearestSlot(piece);
            if (nearestSlot == null)
            {
                ReturnHome(piece);
                return;
            }
            int slotIndex = slots.IndexOf(nearestSlot);
            int row = slotIndex / gridSize;
            int col = slotIndex % gridSize;
            if (pieces.Any(p => p != piece && p.IsPlaced && p.CurrentRow == row && p.CurrentCol == col))
            {
                ReturnHome(piece);
                return;
            }
            Canvas.SetLeft(piece, Canvas.GetLeft(nearestSlot));
            Canvas.SetTop(piece, Canvas.GetTop(nearestSlot));
            piece.CurrentRow = row;
            piece.CurrentCol = col;
            piece.IsPlaced = true;
            CheckPuzzleCompletion();
        }
        private Rectangle FindNearestSlot(PuzzlePiece piece)
        {
            double pieceLeft = Canvas.GetLeft(piece);
            double pieceTop = Canvas.GetTop(piece);
            return slots
            .Select(slot => new
            {
                Slot = slot,
                Distance = Math.Abs(pieceLeft - Canvas.GetLeft(slot)) + Math.Abs(pieceTop - Canvas.GetTop(slot))
            })
            .Where(x => x.Distance < 70)
            .OrderBy(x => x.Distance)
            .Select(x => x.Slot)
            .FirstOrDefault();
        }
        private void ReturnHome(PuzzlePiece piece)
        {
            Canvas.SetLeft(piece, piece.HomeLeft);
            Canvas.SetTop(piece, piece.HomeTop);
            piece.IsPlaced = false;
            piece.CurrentRow = -1;
            piece.CurrentCol = -1;
        }
        private void CheckPuzzleCompletion()
        {
            bool allCorrect = pieces.All(piece =>
            piece.IsPlaced &&
            piece.CurrentRow == piece.CorrectRow &&
            piece.CurrentCol == piece.CorrectCol);
            if (!allCorrect)
                return;
            isCompleted = true;
            CaptchaCompleted?.Invoke(this, true);
            MessageBox.Show("Капча успешно пройдена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    public class PuzzlePiece : ContentControl
    {
        public int CorrectRow { get; set; }
        public int CorrectCol { get; set; }
        public int CurrentRow { get; set; }
        public int CurrentCol { get; set; }
        public bool IsPlaced { get; set; }
        public double HomeLeft { get; set; }
        public double HomeTop { get; set; }
    }
}