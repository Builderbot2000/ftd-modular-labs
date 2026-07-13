using System.Collections.ObjectModel;
using FtdOptima.Domain.Model;
using FtdOptima.Domain.Storage;
using Uno.Extensions.Navigation;

namespace FtdOptima.App.Presentation;

/// <summary>A selectable design starting point: a null <see cref="Template"/> means a blank design.</summary>
public sealed record DesignTemplateChoice(string Display, DesignTemplate? Template);

/// <summary>
/// Home screen: lists persisted vehicle designs and creates new ones (blank or from a template),
/// then navigates into the <see cref="DesignEditorViewModel"/>.
/// </summary>
public partial class DesignsListViewModel : ObservableObject
{
    private readonly IVehicleDesignRepository _repo;
    private readonly ITemplateRepository _templates;
    private readonly INavigator _navigator;

    public DesignsListViewModel(
        IVehicleDesignRepository repo,
        ITemplateRepository templates,
        INavigator navigator)
    {
        _repo = repo;
        _templates = templates;
        _navigator = navigator;

        OpenCommand = new AsyncRelayCommand(OpenAsync, () => SelectedDesign is not null);
        CreateCommand = new AsyncRelayCommand(CreateAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedDesign is not null);
        DuplicateCommand = new AsyncRelayCommand(DuplicateAsync, () => SelectedDesign is not null);
        ManageTemplatesCommand = new AsyncRelayCommand(
            () => _navigator.NavigateViewModelAsync<TemplatesViewModel>(this));

        _ = InitializeAsync();
    }

    public string Title => "FTD Optima — Vehicle Designs";

    public ObservableCollection<VehicleDesign> Designs { get; } = new();

    [ObservableProperty]
    private VehicleDesign? selectedDesign;

    partial void OnSelectedDesignChanged(VehicleDesign? value)
    {
        (OpenCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        (DeleteCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        (DuplicateCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    public ObservableCollection<DesignTemplateChoice> DesignTemplates { get; } = new();

    [ObservableProperty]
    private DesignTemplateChoice? selectedTemplate;

    [ObservableProperty]
    private string newDesignName = "";

    [ObservableProperty]
    private string? statusMessage;

    public ICommand OpenCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand ManageTemplatesCommand { get; }

    private async Task InitializeAsync()
    {
        DesignTemplates.Clear();
        DesignTemplates.Add(new DesignTemplateChoice("Blank", null));
        foreach (var t in await _templates.GetDesignTemplatesAsync())
            DesignTemplates.Add(new DesignTemplateChoice(t.IsBuiltIn ? $"{t.Name} (built-in)" : t.Name, t));
        SelectedTemplate = DesignTemplates.FirstOrDefault();

        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        Designs.Clear();
        foreach (var d in await _repo.GetAllAsync())
            Designs.Add(d);
    }

    private async Task CreateAsync()
    {
        var template = SelectedTemplate?.Template;
        var defaultName = template?.Name is { } tn ? $"New {tn}" : "New Design";
        var designName = string.IsNullOrWhiteSpace(NewDesignName) ? defaultName : NewDesignName.Trim();

        var design = template is null
            ? DesignFactory.CreateBlank(designName, "Custom")
            : DesignFactory.CreateFromTemplate(template, designName);

        await _repo.SaveAsync(design);
        Designs.Add(design);
        NewDesignName = "";
        await _navigator.NavigateViewModelAsync<DesignEditorViewModel>(this, data: design);
    }

    private async Task OpenAsync()
    {
        if (SelectedDesign is { } design)
            await _navigator.NavigateViewModelAsync<DesignEditorViewModel>(this, data: design);
    }

    private async Task DeleteAsync()
    {
        if (SelectedDesign is not { } design)
            return;
        await _repo.DeleteAsync(design.Id);
        Designs.Remove(design);
        SelectedDesign = null;
        StatusMessage = $"Deleted '{design.Name}'.";
    }

    private async Task DuplicateAsync()
    {
        if (SelectedDesign is not { } design)
            return;
        // Reuse the factory: snapshot to an in-memory template, then instantiate a fresh copy.
        var asTemplate = DesignFactory.SnapshotAsTemplate(design, design.Name);
        var copy = DesignFactory.CreateFromTemplate(asTemplate, $"{design.Name} (copy)");
        await _repo.SaveAsync(copy);
        Designs.Add(copy);
        StatusMessage = $"Duplicated '{design.Name}'.";
    }
}
