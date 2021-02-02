using System.Windows;
using System.Windows.Forms;

namespace TVTComment.Views.Behaviors
{
    class ShowOpenFileDialogAction : ShowFileDialogAction
    {
        public override bool CheckFileExists
        {
            get { return (bool)GetValue(CheckFileExistsProperty); }
            set { SetValue(CheckFileExistsProperty, value); }
        }

        public static readonly new DependencyProperty CheckFileExistsProperty =
            DependencyProperty.Register("CheckFileExists", typeof(bool), typeof(ShowOpenFileDialogAction), new PropertyMetadata(true));



        public bool Multiselect
        {
            get { return (bool)GetValue(MultiselectProperty); }
            set { SetValue(MultiselectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Multiselect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MultiselectProperty =
            DependencyProperty.Register("Multiselect", typeof(bool), typeof(ShowOpenFileDialogAction), new PropertyMetadata(false));



        public bool ReadOnlyChecked
        {
            get { return (bool)GetValue(ReadOnlyCheckedProperty); }
            set { SetValue(ReadOnlyCheckedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReadOnlyChecked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadOnlyCheckedProperty =
            DependencyProperty.Register("ReadOnlyChecked", typeof(bool), typeof(ShowOpenFileDialogAction), new PropertyMetadata(false));



        public bool ShowReadOnly
        {
            get { return (bool)GetValue(ShowReadOnlyProperty); }
            set { SetValue(ShowReadOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowReadOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowReadOnlyProperty =
            DependencyProperty.Register("ShowReadOnly", typeof(bool), typeof(ShowOpenFileDialogAction), new PropertyMetadata(false));

        protected override CommonDialog GetDialog()
        {
            return new OpenFileDialog();
        }

        protected override void BeforeDialogShow(CommonDialog dialog)
        {
            var openFileDialog = (OpenFileDialog)dialog;
            openFileDialog.Multiselect = Multiselect;
            openFileDialog.ReadOnlyChecked = ReadOnlyChecked;
            openFileDialog.ShowReadOnly = ShowReadOnly;

            base.BeforeDialogShow(dialog);
        }

        protected override void AfterDialogShow(CommonDialog dialog, DialogResult dialogResult)
        {
            base.AfterDialogShow(dialog, dialogResult);

            if (dialogResult != DialogResult.OK) return;

            var openFileDialog = (OpenFileDialog)dialog;
            ReadOnlyChecked = openFileDialog.ReadOnlyChecked;
        }
    }
}
