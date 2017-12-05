using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.ViewModels.ShellContents
{
    class ChatCollectServiceViewModel:Prism.Mvvm.BindableBase
    {
        private IDisposable disposable;
        private string informationText;

        public Model.ChatCollectService.IChatCollectService Service { get; }
        public string InformationText
        {
            get { return informationText; }
            set { SetProperty(ref informationText, value); }
        }

        public ChatCollectServiceViewModel(Model.ChatCollectService.IChatCollectService service,IObservable<Unit> update)
        {
            this.Service = service;
            this.InformationText = service.GetInformationText();

            disposable = update.Subscribe(_ =>
            {
                InformationText = service.GetInformationText();
            });
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}
