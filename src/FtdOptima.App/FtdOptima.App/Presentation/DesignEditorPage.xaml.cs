using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace FtdOptima.App.Presentation;

public sealed partial class DesignEditorPage : Page
{
    public DesignEditorPage()
    {
        this.InitializeComponent();
    }

    private void OnModuleDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (DataContext is not DesignEditorViewModel vm)
            return;

        // Prefer the row actually double-clicked; fall back to the highlighted row.
        var item = (e.OriginalSource as FrameworkElement)?.DataContext as ModuleListItem
                   ?? vm.SelectedModuleItem;
        if (item is not null)
            vm.OpenModuleCommand.Execute(item);
    }
}
