using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace TVTComment.Views.Behaviors
{
    abstract class ShowCommonDialogAction:TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            var dialog = GetDialog();
            BeforeDialogShow(dialog);

            DialogResult dialogResult;
            var window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                var win32Window = new NativeWindow();
                win32Window.AssignHandle(new WindowInteropHelper(window).Handle);
                dialogResult = dialog.ShowDialog(win32Window);
            }
            else
                dialogResult = dialog.ShowDialog();

            AfterDialogShow(dialog, dialogResult);
        }

        protected abstract CommonDialog GetDialog();

        protected abstract void BeforeDialogShow(CommonDialog dialog);

        protected abstract void AfterDialogShow(CommonDialog dialog, DialogResult dialogResult);
    }
}
