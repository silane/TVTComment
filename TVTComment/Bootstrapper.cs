using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVTComment.Views;
using Microsoft.Practices.Unity;
using Prism.Unity;
using System.Windows;

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
        }
    }
}
