using System.Windows;
using System.Windows.Forms;

namespace TVTComment.Views.Behaviors
{
    abstract class ShowFileDialogAction : ShowCommonDialogAction
    {
        public bool AddExtension
        {
            get { return (bool)GetValue(AddExtensionProperty); }
            set { SetValue(AddExtensionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddExtension.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddExtensionProperty =
            DependencyProperty.Register("AddExtension", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(true));



        public bool AutoUpgradeEnabled
        {
            get { return (bool)GetValue(AutoUpgradeEnabledProperty); }
            set { SetValue(AutoUpgradeEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoUpgradeEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoUpgradeEnabledProperty =
            DependencyProperty.Register("AutoUpgradeEnabled", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(true));



        public virtual bool CheckFileExists
        {
            get { return (bool)GetValue(CheckFileExistsProperty); }
            set { SetValue(CheckFileExistsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CheckFileExists.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CheckFileExistsProperty =
            DependencyProperty.Register("CheckFileExists", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(false));



        public bool CheckPathExists
        {
            get { return (bool)GetValue(CheckPathExistsProperty); }
            set { SetValue(CheckPathExistsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CheckPathExists.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CheckPathExistsProperty =
            DependencyProperty.Register("CheckPathExists", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(true));



        public FileDialogCustomPlacesCollection CustomPlaces
        {
            get { return (FileDialogCustomPlacesCollection)GetValue(CustomPlacesProperty); }
            set { SetValue(CustomPlacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomPlaces.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomPlacesProperty =
            DependencyProperty.Register("CustomPlaces", typeof(FileDialogCustomPlacesCollection), typeof(ShowFileDialogAction), new PropertyMetadata(new FileDialogCustomPlacesCollection()));



        public string DefaultExt
        {
            get { return (string)GetValue(DefaultExtProperty); }
            set { SetValue(DefaultExtProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultExt.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultExtProperty =
            DependencyProperty.Register("DefaultExt", typeof(string), typeof(ShowFileDialogAction), new PropertyMetadata(""));



        public bool DereferenceLinks
        {
            get { return (bool)GetValue(DereferenceLinksProperty); }
            set { SetValue(DereferenceLinksProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DereferenceLinks.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DereferenceLinksProperty =
            DependencyProperty.Register("DereferenceLinks", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(true));



        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(ShowFileDialogAction), new PropertyMetadata(""));



        public string[] FileNames
        {
            get { return (string[])GetValue(FileNamesProperty); }
            set { SetValue(FileNamesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileNames.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNamesProperty =
            DependencyProperty.Register("FileNames", typeof(string[]), typeof(ShowFileDialogAction), new PropertyMetadata(System.Array.Empty<string>()));



        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(ShowFileDialogAction), new PropertyMetadata(""));



        public int FilterIndex
        {
            get { return (int)GetValue(FilterIndexProperty); }
            set { SetValue(FilterIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FilterIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterIndexProperty =
            DependencyProperty.Register("FilterIndex", typeof(int), typeof(ShowFileDialogAction), new PropertyMetadata(1));



        public string InitialDirectory
        {
            get { return (string)GetValue(InitialDirectoryProperty); }
            set { SetValue(InitialDirectoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InitialDirectory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InitialDirectoryProperty =
            DependencyProperty.Register("InitialDirectory", typeof(string), typeof(ShowFileDialogAction), new PropertyMetadata(""));



        public bool RestoreDirectory
        {
            get { return (bool)GetValue(RestoreDirectoryProperty); }
            set { SetValue(RestoreDirectoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RestoreDirectory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RestoreDirectoryProperty =
            DependencyProperty.Register("RestoreDirectory", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(false));



        public bool SupportMultiDottedExtensions
        {
            get { return (bool)GetValue(SupportMultiDottedExtensionsProperty); }
            set { SetValue(SupportMultiDottedExtensionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SupportMultiDottedExtensions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SupportMultiDottedExtensionsProperty =
            DependencyProperty.Register("SupportMultiDottedExtensions", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(false));



        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ShowFileDialogAction), new PropertyMetadata(""));



        public bool ValidateNames
        {
            get { return (bool)GetValue(ValidateNamesProperty); }
            set { SetValue(ValidateNamesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidateNames.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidateNamesProperty =
            DependencyProperty.Register("ValidateNames", typeof(bool), typeof(ShowFileDialogAction), new PropertyMetadata(true));

        protected override void BeforeDialogShow(CommonDialog dialog)
        {
            var fileDialog = (FileDialog)dialog;
            fileDialog.AddExtension = AddExtension;
            fileDialog.AutoUpgradeEnabled = AutoUpgradeEnabled;
            fileDialog.CheckFileExists = CheckFileExists;
            fileDialog.CheckPathExists = CheckPathExists;
            foreach (var customPlace in CustomPlaces)
                fileDialog.CustomPlaces.Add(customPlace);
            fileDialog.DefaultExt = DefaultExt;
            fileDialog.DereferenceLinks = DereferenceLinks;
            fileDialog.FileName = FileName;
            fileDialog.Filter = Filter;
            fileDialog.FilterIndex = FilterIndex;
            fileDialog.InitialDirectory = InitialDirectory;
            fileDialog.RestoreDirectory = RestoreDirectory;
            fileDialog.SupportMultiDottedExtensions = SupportMultiDottedExtensions;
            fileDialog.Title = Title;
            fileDialog.ValidateNames = ValidateNames;
        }

        protected override void AfterDialogShow(CommonDialog dialog, DialogResult dialogResult)
        {
            if (dialogResult != DialogResult.OK) return;

            var fileDialog = (FileDialog)dialog;
            FileName = fileDialog.FileName;
            FileNames = fileDialog.FileNames;
            FilterIndex = fileDialog.FilterIndex;
        }
    }
}
