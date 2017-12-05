using Prism.Common;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace TVTComment.Views.Behaviors
{
    class RegionContextBinderBehavior:Behavior<DependencyObject>
    {
        public object Binding
        {
            get { return (object)GetValue(BindingProperty); }
            set { SetValue(BindingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Binding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BindingProperty =
            DependencyProperty.Register("Binding", typeof(object), typeof(RegionContextBinderBehavior), new PropertyMetadata(null,BindingPropertyChanged));

        private ObservableObject<object> regionContext;

        protected override void OnAttached()
        {
            regionContext = RegionContext.GetObservableContext(AssociatedObject);
            regionContext.PropertyChanged += RegionContext_PropertyChanged;
        }

        protected override void OnDetaching()
        {
            regionContext.PropertyChanged -= RegionContext_PropertyChanged;
            regionContext = null;
        }

        private void RegionContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(regionContext.Value!=Binding)
                Binding = regionContext.Value;
        }

        private static void BindingPropertyChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            var regionContext = ((RegionContextBinderBehavior)d).regionContext;
            if(regionContext.Value!=e.NewValue)
                regionContext.Value = e.NewValue;
        }
    }
}
