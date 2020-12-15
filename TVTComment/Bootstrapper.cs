using Microsoft.Practices.Unity;
using Prism.Unity;
using System.Windows;
using TVTComment.Views;

namespace TVTComment
{
    class Bootstrapper:UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return this.Container.Resolve<Shell>();
        }

        protected override void InitializeShell()
        {
            ((Window)this.Shell).Show();
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            this.Container.RegisterInstance(typeof(Model.TVTComment), new Model.TVTComment());
            this.Container.RegisterTypeForNavigation<Views.ChatCollectServiceCreationOptionControl.NichanChatCollectServiceCreationOptionControl>
                 (nameof(Views.ChatCollectServiceCreationOptionControl.NichanChatCollectServiceCreationOptionControl));
            this.Container.RegisterTypeForNavigation<Views.ChatCollectServiceCreationOptionControl.FileChatCollectServiceCreationOptionControl>
                (nameof(Views.ChatCollectServiceCreationOptionControl.FileChatCollectServiceCreationOptionControl));
            this.Container.RegisterTypeForNavigation<Views.ChatCollectServiceCreationOptionControl.NiconicoLiveChatCollectServiceCreationOptionControl>
                (nameof(Views.ChatCollectServiceCreationOptionControl.NiconicoLiveChatCollectServiceCreationOptionControl));
        }
    }
}
