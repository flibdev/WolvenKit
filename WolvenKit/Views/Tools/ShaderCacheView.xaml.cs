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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;
using Syncfusion.Windows.PropertyGrid;
using WolvenKit.App.ViewModels.Tools;
using WolvenKit.App.ViewModels.Tools.ShaderCache;

namespace WolvenKit.Views.Tools
{
    /// <summary>
    /// Interaction logic for ShaderCacheView.xaml
    /// </summary>
    public partial class ShaderCacheView : ReactiveUserControl<ShaderCacheViewModel>
    {
        public ShaderCacheViewModel Context => (ShaderCacheViewModel)DataContext;

        public ShaderCacheView()
        {
            InitializeComponent();
        }

        private void MatPropGrid_AutoGeneratingPropertyGridItem(object sender, AutoGeneratingPropertyGridItemEventArgs e)
        {
            switch (e.DisplayName)
            {
                case nameof(ReactiveObject.Changed):
                case nameof(ReactiveObject.Changing):
                case nameof(ReactiveObject.ThrownExceptions):
                case nameof(MaterialViewModel.Techniques):
                    e.Cancel = true;
                    break;
                default:
                    break;
            }
            e.ReadOnly = true;
        }
    }
}
