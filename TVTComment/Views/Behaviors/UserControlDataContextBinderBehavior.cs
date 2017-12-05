using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace TVTComment.Views.Behaviors
{
    /// <summary>
    /// PrismのAutoWireViewModelがセットされたUserControlのDataContextに、UserControlが配置されたWindow側からアクセスするために使う
    /// </summary>
    class UserControlDataContextBinderBehavior:Behavior<FrameworkElement>
    {
        public object Binding
        {
            get { return (object)GetValue(BindingProperty); }
            set { SetValue(BindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Binding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BindingProperty =
            DependencyProperty.Register("Binding", typeof(object), typeof(UserControlDataContextBinderBehavior), new PropertyMetadata(null));

        private UserControl target;

        protected override void OnAttached()
        {
            target = getDescendantOrSelfUserControl(AssociatedObject);
            if (target == null) return;

            OnDataContextChanged(target.DataContext);
            target.DataContextChanged += AssociatedObject_DataContextChanged;
        }

        protected override void OnDetaching()
        {
            if (target == null) return;
            target.DataContextChanged -= AssociatedObject_DataContextChanged;
        }

        private UserControl getDescendantOrSelfUserControl(FrameworkElement frameworkElement)
        {
            var userControl = frameworkElement as UserControl;
            if (userControl != null) return userControl;

            foreach(var child in LogicalTreeHelper.GetChildren(frameworkElement))
            {
                var elem=child as FrameworkElement;
                if (elem != null)
                {
                    var descendant = getDescendantOrSelfUserControl(elem);
                    if (descendant != null) return descendant;
                }
            }
            return null;
        }

        private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnDataContextChanged(e.NewValue);
        }

        private void OnDataContextChanged(object newDataContext)
        {
            Binding = newDataContext;
        }
    }
}
