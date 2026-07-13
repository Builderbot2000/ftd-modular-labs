using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using FtdModularLabs.Domain.Model;

namespace FtdModularLabs.App.Presentation;

public sealed partial class DesignsListPage : Page
{
    public DesignsListPage()
    {
        this.InitializeComponent();
    }

    private void OnDesignDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (DataContext is not DesignsListViewModel vm)
            return;

        // The first click of the double selects the row, so SelectedDesign is set; open it.
        if ((e.OriginalSource as FrameworkElement)?.DataContext is VehicleDesign)
        {
            if (vm.OpenCommand.CanExecute(null))
                vm.OpenCommand.Execute(null);
        }
    }
}
