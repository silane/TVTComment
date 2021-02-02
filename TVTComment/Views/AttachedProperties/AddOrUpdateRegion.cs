using Prism.Regions;
using System.Windows;

namespace TVTComment.Views.AttachedProperties
{
    /// <summary>
    /// Regionはターゲットのコントロールが消えても登録解除されないので場合によってはすでに同じ名前のRegionが登録されているといってエラーになる
    /// そこでこのクラスを使うと既に登録されている場合はエラーにせず上書きで登録する
    /// </summary>
    static class AddOrUpdateRegion
    {
        public static string GetRegionName(DependencyObject obj)
        {
            return (string)obj.GetValue(RegionNameProperty);
        }

        public static void SetRegionName(DependencyObject obj, string value)
        {
            obj.SetValue(RegionNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for RegionName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RegionNameProperty =
            DependencyProperty.RegisterAttached("RegionName", typeof(string), typeof(AddOrUpdateRegion), new PropertyMetadata(null, OnRegionNamePropertyChanged));

        private static void OnRegionNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var regionName = (string)e.NewValue;
            IRegionManager regionManager = FindRegionManager(d);
            if (regionManager != null && regionManager.Regions.ContainsRegionWithName(regionName))
                regionManager.Regions.Remove(regionName);
            RegionManager.SetRegionName(d, regionName);
        }

        private static IRegionManager FindRegionManager(DependencyObject d)
        {
            IRegionManager regionManager = RegionManager.GetRegionManager(d);
            if (regionManager != null)
                return regionManager;

            DependencyObject parent = LogicalTreeHelper.GetParent(d);
            if (parent != null)
                return FindRegionManager(parent);
            return null;
        }
    }
}
