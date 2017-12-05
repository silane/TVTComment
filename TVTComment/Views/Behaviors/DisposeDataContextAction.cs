using System;
using System.Windows;
using System.Windows.Interactivity;

namespace TVTComment.Views.Behaviors
{
    class DisposeDataContextAction:TriggerAction<FrameworkElement>
    {
        protected override void Invoke(object parameter)
        {
            (AssociatedObject.DataContext as IDisposable)?.Dispose();
        }
    }
}
