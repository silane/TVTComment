using System.Windows.Controls;

namespace TVTComment.Model.Utils
{
    public class IMETextBox : TextBox
    {
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            GetBindingExpression(TextProperty).UpdateSource();
            base.OnTextChanged(e);
        }
    }
}
