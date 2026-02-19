using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ReactiveUI;

using WolvenKit.App.ViewModels.Tools.ShaderCache;

namespace WolvenKit.Views.Dialogs.Windows
{
    /// <summary>
    /// Interaction logic for ExportShaderTechniquesDialogView.xaml
    /// </summary>
    public partial class ExportShaderTechniquesDialogView : IViewFor<ExportShaderTechniquesDialogViewModel>
    {
        /// <summary>
        /// GUID to track folder open dialog history separately from main app
        /// </summary>
        private readonly Guid _dialogGuid = new("bfbd5734-0e25-44dd-b5c3-b3b678aec610");

        public ExportShaderTechniquesDialogViewModel ViewModel { get; set; }
        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (ExportShaderTechniquesDialogViewModel)value;
        }

        public ExportShaderTechniquesDialogView()
        {
            InitializeComponent();

            ViewModel = new ExportShaderTechniquesDialogViewModel();
            DataContext = ViewModel;
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel is not ExportShaderTechniquesDialogViewModel vm)
            {
                return;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                ClientGuid = _dialogGuid
            };

            if (dialog.ShowDialog() == true)
            {
                vm.Folder = dialog.FolderName;
            }
        }
    }
}
