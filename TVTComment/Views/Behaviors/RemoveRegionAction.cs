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
    class RemoveRegionAction:TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            IRegionManager regionManager=FindRegionManager(AssociatedObject);
            if (regionManager == null) return;
            regionManager.Regions.Remove(RegionManager.GetRegionName(AssociatedObject));
        }

        private static IRegionManager FindRegionManager(DependencyObject d)
        {
            IRegionManager regionManager = RegionManager.GetRegionManager(d);
            if (regionManager != null)
                return regionManager;

            DependencyObject parent = LogicalTreeHelper.GetParent(d);
            if (parent != null)
                FindRegionManager(parent);
            return null;
        }
    }
}
