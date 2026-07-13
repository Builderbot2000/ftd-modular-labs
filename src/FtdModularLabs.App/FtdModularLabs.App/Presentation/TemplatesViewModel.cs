using System.Collections.ObjectModel;
using FtdModularLabs.Domain.Model;
using FtdModularLabs.Domain.Storage;
using Uno.Extensions.Navigation;

namespace FtdModularLabs.App.Presentation;

/// <summary>
/// Template library manager: browse built-in and user templates for designs and modules, and delete
/// user-created ones (built-ins are read-only).
/// </summary>
public partial class TemplatesViewModel : ObservableObject
{
    private readonly ITemplateRepository _templates;
    private readonly INavigator _navigator;

    public TemplatesViewModel(ITemplateRepository templates, INavigator navigator)
    {
        _templates = templates;
        _navigator = navigator;

        DeleteDesignTemplateCommand = new AsyncRelayCommand(DeleteDesignTemplateAsync,
            () => SelectedDesignTemplate is { IsBuiltIn: false });
        DeleteModuleTemplateCommand = new AsyncRelayCommand(DeleteModuleTemplateAsync,
            () => SelectedModuleTemplate is { IsBuiltIn: false });
        BackCommand = new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

        _ = RefreshAsync();
    }

    public string Title => "Template Library";

    public ObservableCollection<DesignTemplate> DesignTemplates { get; } = new();
    public ObservableCollection<ModuleTemplate> ModuleTemplates { get; } = new();

    [ObservableProperty]
    private DesignTemplate? selectedDesignTemplate;

    partial void OnSelectedDesignTemplateChanged(DesignTemplate? value) =>
        (DeleteDesignTemplateCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

    [ObservableProperty]
    private ModuleTemplate? selectedModuleTemplate;

    partial void OnSelectedModuleTemplateChanged(ModuleTemplate? value) =>
        (DeleteModuleTemplateCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();

    [ObservableProperty]
    private string? statusMessage;

    public ICommand DeleteDesignTemplateCommand { get; }
    public ICommand DeleteModuleTemplateCommand { get; }
    public ICommand BackCommand { get; }

    private async Task RefreshAsync()
    {
        DesignTemplates.Clear();
        foreach (var t in await _templates.GetDesignTemplatesAsync())
            DesignTemplates.Add(t);

        ModuleTemplates.Clear();
        foreach (var t in await _templates.GetModuleTemplatesAsync())
            ModuleTemplates.Add(t);
    }

    private async Task DeleteDesignTemplateAsync()
    {
        if (SelectedDesignTemplate is not { IsBuiltIn: false } t)
            return;
        await _templates.DeleteDesignTemplateAsync(t.TemplateId);
        DesignTemplates.Remove(t);
        StatusMessage = $"Deleted design template '{t.Name}'.";
    }

    private async Task DeleteModuleTemplateAsync()
    {
        if (SelectedModuleTemplate is not { IsBuiltIn: false } t)
            return;
        await _templates.DeleteModuleTemplateAsync(t.TemplateId);
        ModuleTemplates.Remove(t);
        StatusMessage = $"Deleted module template '{t.Name}'.";
    }
}
