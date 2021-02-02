using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace TVTComment.Views.Behaviors
{
    class ShowColorDialogAction : ShowCommonDialogAction
    {
        public bool AllowFullOpen
        {
            get { return (bool)GetValue(AllowFullOpenProperty); }
            set { SetValue(AllowFullOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowFullOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowFullOpenProperty =
            DependencyProperty.Register("AllowFullOpen", typeof(bool), typeof(ShowColorDialogAction), new PropertyMetadata(true));


        public bool AnyColor
        {
            get { return (bool)GetValue(AnyColorProperty); }
            set { SetValue(AnyColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnyColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnyColorProperty =
            DependencyProperty.Register("AnyColor", typeof(bool), typeof(ShowColorDialogAction), new PropertyMetadata(false));


        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(ShowColorDialogAction), new PropertyMetadata(Color.Black));


        public Color[] CustomColors
        {
            get { return (Color[])GetValue(CustomColorsProperty); }
            set { SetValue(CustomColorsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomColors.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomColorsProperty =
            DependencyProperty.Register("CustomColors", typeof(Color[]), typeof(ShowColorDialogAction), new PropertyMetadata(null));


        public bool FullOpen
        {
            get { return (bool)GetValue(FullOpenProperty); }
            set { SetValue(FullOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FullOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FullOpenProperty =
            DependencyProperty.Register("FullOpen", typeof(bool), typeof(ShowColorDialogAction), new PropertyMetadata(false));


        public bool SolidColorOnly
        {
            get { return (bool)GetValue(SolidColorOnlyProperty); }
            set { SetValue(SolidColorOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SolidColorOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SolidColorOnlyProperty =
            DependencyProperty.Register("SolidColorOnly", typeof(bool), typeof(ShowColorDialogAction), new PropertyMetadata(false));

        protected override CommonDialog GetDialog()
        {
            return new ColorDialog();
        }

        protected override void BeforeDialogShow(CommonDialog dialog)
        {
            var colorDialog = (ColorDialog)dialog;
            colorDialog.AllowFullOpen = AllowFullOpen;
            colorDialog.AnyColor = AnyColor;
            colorDialog.Color = Color;
            colorDialog.CustomColors = CustomColors?.Select(x => x.ToArgb()).ToArray();
            colorDialog.FullOpen = FullOpen;
            colorDialog.SolidColorOnly = SolidColorOnly;
        }

        protected override void AfterDialogShow(CommonDialog dialog, DialogResult dialogResult)
        {
            if (dialogResult != DialogResult.OK) return;

            var colorDialog = (ColorDialog)dialog;
            Color = colorDialog.Color;
            CustomColors = colorDialog.CustomColors?.Select(x => Color.FromArgb(x)).ToArray();
        }
    }
}
