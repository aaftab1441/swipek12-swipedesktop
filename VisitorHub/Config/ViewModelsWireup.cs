using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using SwipeDesktop.Api;
using SwipeDesktop.Modal;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Config
{
    internal class ViewModelsWireup : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder.Register(c => new StudentAlternateIdViewModel()).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.StudentAlternateId.ToString())).SingleInstance();

            builder.Register(c => new SettingsViewModel(c.Resolve<LocalStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.VisitorSettingsDialog.ToString())).SingleInstance();

            builder.Register(c => new SettingsViewModel(c.Resolve<LocalStorage>(), c.Resolve<IdCardStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.SettingsDialog.ToString())).SingleInstance();
            builder.Register(c => new StudentCardViewModel(c.Resolve<LocalStorage>(), c.Resolve<FineStorage>(), c.Resolve<IdCardStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.StudentCardDialog.ToString())).SingleInstance();
            builder.Register(c => new StaffCardViewModel(c.Resolve<LocalStorage>(), c.Resolve<FineStorage>(), c.Resolve<IdCardStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.StaffCardDialog.ToString())).SingleInstance();
            builder.Register(c => new AddPersonViewModel(c.Resolve<LocalStorage>(), c.Resolve<RemoteStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.AddPersonViewModel.ToString())).SingleInstance();

            builder.Register(c => new SelectedForPrintViewModel(c.Resolve<LocalStorage>(), c.Resolve<RemoteStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.StudentsSelectedForPrint.ToString())).SingleInstance();

            builder.Register(c => new BatchPrintViewModel(c.Resolve<LocalStorage>(), c.Resolve<RemoteStorage>())).AsImplementedInterfaces().WithMetadata<IDialogMetadata>(m => m.For(tm => tm.DialogName, DialogConstants.BatchPrint.ToString())).SingleInstance();

        }
    }
}
