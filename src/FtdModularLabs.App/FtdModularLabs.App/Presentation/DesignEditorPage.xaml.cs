using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace FtdModularLabs.App.Presentation;

public sealed partial class DesignEditorPage : Page
{
    public DesignEditorPage()
    {
        this.InitializeComponent();
    }

    // Click-to-toggle: click a module to open its editor; click the currently-open module again to
    // close it and return to the vehicle overview. Clicking a different module switches editors.
    private void OnModuleTapped(object sender, TappedRoutedEventArgs e)
    {
        if (DataContext is not DesignEditorViewModel vm)
            return;

        var item = (e.OriginalSource as FrameworkElement)?.DataContext as ModuleListItem;
        if (item is null)
            return;

        if (ReferenceEquals(item, vm.EnteredModule))
        {
            vm.EnteredModule = null;
            vm.SelectedModuleItem = null;
        }
        else
        {
            vm.EnteredModule = item;
        }
        e.Handled = true;
    }
}
