using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	public class DrawingSurface : SKCanvasView
	{
		public static BindableProperty DrawingProperty = BindableProperty.Create(
			nameof(Drawing), typeof(SKImage), typeof(DrawingSurface),
			defaultBindingMode: BindingMode.TwoWay,
			propertyChanged: OnDrawingChanged);

		private const int StrokeWidth = 20;

		private readonly SKPaint paint;
		private readonly List<SKPath> strokes;
		private readonly Dictionary<long, SKPath> currentStrokes;

		public DrawingSurface()
		{
			paint = new SKPaint
			{
				Color = SKColors.Black,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round,
				StrokeJoin = SKStrokeJoin.Round,
				StrokeWidth = StrokeWidth,
				Style = SKPaintStyle.Stroke,
			};
			strokes = new List<SKPath>();
			currentStrokes = new Dictionary<long, SKPath>();

			EnableTouchEvents = true;

			PaintSurface += OnPaintSurface;
			Touch += OnTouch;
		}

		public SKImage Drawing
		{
			get => (SKImage)GetValue(DrawingProperty);
			set => SetValue(DrawingProperty, value);
		}

		public SKImage GenerateImage()
		{
			// determine the minimal bounds
			var hasStrokes = false;
			var bounds = new SKRect(
				float.MaxValue, float.MaxValue,
				float.MinValue, float.MinValue);
			foreach (var path in strokes.Union(currentStrokes.Values))
			{
				hasStrokes = true;
				var pathBounds = path.Bounds;
				if (bounds.Left > pathBounds.Left)
					bounds.Left = pathBounds.Left;
				if (bounds.Top > pathBounds.Top)
					bounds.Top = pathBounds.Top;
				if (bounds.Right < pathBounds.Right)
					bounds.Right = pathBounds.Right;
				if (bounds.Bottom < pathBounds.Bottom)
					bounds.Bottom = pathBounds.Bottom;
			}

			// if there are no strokes, then return null
			if (!hasStrokes)
				return null;

			// add a bit of padding and then convert to integral
			bounds.Inflate(StrokeWidth + 1f, StrokeWidth + 1f);
			var intBounds = new SKRectI(
				(int)bounds.Left, (int)bounds.Top,
				(int)bounds.Right, (int)bounds.Bottom);

			// create a new surface
			var info = new SKImageInfo(intBounds.Width, intBounds.Height);
			using (var surface = SKSurface.Create(info))
			{
				var canvas = surface.Canvas;
				canvas.Translate(-bounds.Left, -bounds.Top);

				// make sure to create a white canvas
				canvas.Clear(SKColors.White);

				// draw the strokes
				GenerateImage(canvas, 1f);

				// generate the image
				return surface.Snapshot();
			}
		}

		public void Clear()
		{
			strokes.Clear();
			currentStrokes.Clear();
			Drawing = null;

			InvalidateSurface();
		}

		private static void OnDrawingChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is DrawingSurface surface && newValue is null)
				surface.Clear();
		}

		private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;

			var currentScale = (float)(e.Info.Width / Width);

			canvas.Clear(SKColors.Transparent);

			GenerateImage(canvas, currentScale);
		}

		private void GenerateImage(SKCanvas canvas, float currentScale)
		{
			paint.StrokeWidth = StrokeWidth * currentScale;

			foreach (var path in strokes.Union(currentStrokes.Values))
			{
				canvas.DrawPath(path, paint);
			}
		}

		private void OnTouch(object sender, SKTouchEventArgs e)
		{
			// update the lines
			if (e.InContact)
			{
				if (currentStrokes.TryGetValue(e.Id, out var downPath))
				{
					downPath.LineTo(e.Location);
				}
				else
				{
					downPath = new SKPath();
					downPath.MoveTo(e.Location);
					currentStrokes[e.Id] = downPath;
				}
			}

			// handle the completion
			if (e.ActionType == SKTouchAction.Released && currentStrokes.TryGetValue(e.Id, out var upPath))
			{
				strokes.Add(upPath);
				currentStrokes.Remove(e.Id);

				// we are finished with a stroke, notify the listeners
				Drawing = GenerateImage();
			}

			// trigger a redraw
			Device.BeginInvokeOnMainThread(InvalidateSurface);

			e.Handled = true;
		}
	}
}
