using Microsoft.Practices.ServiceLocation;
using Prism.Regions;
using System.Windows;
using System.Windows.Interactivity;

namespace TVTComment.Views.Behaviors
{
    class SetRegionManagerBehavior : Behavior<DependencyObject>
    {


        public bool CreateNewInstance
        {
            get { return (bool)GetValue(CreateNewInstanceProperty); }
            set { SetValue(CreateNewInstanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CreateNewInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CreateNewInstanceProperty =
            DependencyProperty.Register("CreateNewInstance", typeof(bool), typeof(SetRegionManagerBehavior), new PropertyMetadata(false));



        protected override void OnAttached()
        {
            IRegionManager regionManager = ServiceLocator.Current.GetInstance<IRegionManager>();
            if (CreateNewInstance)
                regionManager = regionManager.CreateRegionManager();
            RegionManager.SetRegionManager(AssociatedObject, regionManager);
        }
    }
}
