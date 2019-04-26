using System;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	public class SlideInBehavior : Behavior
	{
		private VisualElement element;

		public static BindableProperty IsVisibleProperty = BindableProperty.Create(
			nameof(IsVisible), typeof(bool), typeof(SlideInBehavior), propertyChanged: OnIsVisibleChanged);

		public bool IsVisible
		{
			get => (bool)GetValue(IsVisibleProperty);
			set => SetValue(IsVisibleProperty, value);
		}

		protected override void OnAttachedTo(BindableObject bindable)
		{
			base.OnAttachedTo(bindable);

			element = bindable as VisualElement;
			if (element != null)
			{
				BindingContext = element.BindingContext;
				element.BindingContextChanged += OnElementBindingContextChanged;
			}
		}

		protected override void OnDetachingFrom(BindableObject bindable)
		{
			if (element != null)
			{
				element.BindingContextChanged -= OnElementBindingContextChanged;
				element = null;
			}
			BindingContext = null;

			base.OnDetachingFrom(bindable);
		}

		private void OnElementBindingContextChanged(object sender, EventArgs e) =>
			BindingContext = element?.BindingContext;

		private static void OnIsVisibleChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is SlideInBehavior behavior && newValue is bool isVisible)
			{
				var offset = isVisible ? 0 : behavior.element.Height;
				behavior.element.TranslateTo(0, offset);
			}
		}
	}
}
