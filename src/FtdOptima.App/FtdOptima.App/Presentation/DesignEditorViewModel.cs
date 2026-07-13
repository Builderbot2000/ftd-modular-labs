using System.Collections.ObjectModel;
using FtdOptima.Domain.Catalog;
using FtdOptima.Domain.Model;
using FtdOptima.Domain.Storage;
using Uno.Extensions.Navigation;

namespace FtdOptima.App.Presentation;

/// <summary>An observable row for a module in the design's module list; edits its name live.</summary>
public partial class ModuleListItem : ObservableObject
{
    public ModuleListItem(DesignModule model, ISubsystemTypeRegistry registry)
    {
        Model = model;
        var type = registry.FindType(model.SubsystemTypeId);
        TypeName = type?.Name ?? model.SubsystemTypeId;
        HasCalculator = type?.HasCalculator ?? false;
        name = model.Name;
    }

    public DesignModule Model { get; }
    public string TypeName { get; }
    public bool HasCalculator { get; }

    [ObservableProperty]
    private string name;

    partial void OnNameChanged(string value) => Model.Name = value;
}

/// <summary>A selectable subsystem type in the "add module" picker, shown grouped by category.</summary>
public sealed record TypeChoice(SubsystemType Type, string Display);

/// <summary>A selectable module preset in the "add module" picker; a null <see cref="Template"/> means blank.</summary>
public sealed record TemplateChoice(string Display, ModuleTemplate? Template);

/// <summary>
/// Master/detail editor for one <see cref="VehicleDesign"/>: edit its name/class, add/remove/reorder
/// modules, edit the selected module's parameters (via <see cref="ModuleEditorViewModel"/>), and
/// persist the whole design. Keeping it a single page means saving is one repository call.
/// </summary>
public partial class DesignEditorViewModel : ObservableObject
{
    private readonly VehicleDesign _design;
    private readonly IVehicleDesignRepository _repo;
    private readonly ITemplateRepository _templates;
    private readonly ISubsystemTypeRegistry _registry;
    private readonly INavigator _navigator;

    public DesignEditorViewModel(
        VehicleDesign design,
        IVehicleDesignRepository repo,
        ITemplateRepository templates,
        ISubsystemTypeRegistry registry,
        INavigator navigator)
    {
        _design = design;
        _repo = repo;
        _templates = templates;
        _registry = registry;
        _navigator = navigator;

        name = design.Name;
        vehicleClass = design.VehicleClass;

        Modules = new ObservableCollection<ModuleListItem>(
            design.Modules.Select(m => new ModuleListItem(m, registry)));

        Types = registry.All
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .Select(t => new TypeChoice(t, $"{t.Category}: {t.Name}"))
            .ToList();
        selectedType = Types.FirstOrDefault();

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        SaveAsTemplateCommand = new AsyncRelayCommand(SaveAsTemplateAsync);
        AddModuleCommand = new AsyncRelayCommand(AddModuleAsync);
        BackCommand = new AsyncRelayCommand(BackAsync);

        // Modules start collapsed: nothing is opened for editing until the user enters one.
        _ = LoadModuleTemplatesAsync();
    }

    // ---- design fields ----

    [ObservableProperty]
    private string name;

    partial void OnNameChanged(string value) => _design.Name = value;

    [ObservableProperty]
    private string vehicleClass;

    partial void OnVehicleClassChanged(string value) => _design.VehicleClass = value;

    // ---- module list + detail ----

    public ObservableCollection<ModuleListItem> Modules { get; }

    /// <summary>The row highlighted in the list (single click). Highlighting does not open the editor.</summary>
    [ObservableProperty]
    private ModuleListItem? selectedModuleItem;

    /// <summary>The module opened for editing in the detail pane (entered via double click).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEnteredModule))]
    private ModuleListItem? enteredModule;

    [ObservableProperty]
    private ModuleEditorViewModel? moduleEditor;

    public bool HasEnteredModule => EnteredModule is not null;

    partial void OnEnteredModuleChanged(ModuleListItem? oldValue, ModuleListItem? newValue)
    {
        // Persist edits from the previously-open module before switching.
        ModuleEditor?.CommitToModule();
        ModuleEditor = newValue is null ? null : new ModuleEditorViewModel(newValue.Model, _registry);
    }

    /// <summary>Enters a module (opens its editor). Triggered by double-clicking a list row.</summary>
    [RelayCommand]
    private void OpenModule(ModuleListItem? item)
    {
        if (item is not null)
            EnteredModule = item;
    }

    // ---- add-module picker ----

    public IReadOnlyList<TypeChoice> Types { get; }

    [ObservableProperty]
    private TypeChoice? selectedType;

    partial void OnSelectedTypeChanged(TypeChoice? value) => _ = LoadModuleTemplatesAsync();

    public ObservableCollection<TemplateChoice> ModuleTemplates { get; } = new();

    [ObservableProperty]
    private TemplateChoice? selectedModuleTemplate;

    [ObservableProperty]
    private string newModuleName = "";

    [ObservableProperty]
    private string? statusMessage;

    public ICommand SaveCommand { get; }
    public ICommand SaveAsTemplateCommand { get; }
    public ICommand AddModuleCommand { get; }
    public ICommand BackCommand { get; }

    [RelayCommand]
    private void RemoveModule(ModuleListItem? item)
    {
        if (item is null)
            return;
        _design.Modules.Remove(item.Model);
        Modules.Remove(item);
        if (ReferenceEquals(SelectedModuleItem, item))
            SelectedModuleItem = null;
        if (ReferenceEquals(EnteredModule, item))
            EnteredModule = null;
    }

    [RelayCommand]
    private void MoveModuleUp(ModuleListItem? item) => Move(item, -1);

    [RelayCommand]
    private void MoveModuleDown(ModuleListItem? item) => Move(item, +1);

    private void Move(ModuleListItem? item, int delta)
    {
        if (item is null)
            return;
        int i = Modules.IndexOf(item);
        int j = i + delta;
        if (i < 0 || j < 0 || j >= Modules.Count)
            return;
        Modules.Move(i, j);
        _design.Modules.RemoveAt(i);
        _design.Modules.Insert(j, item.Model);
    }

    private async Task LoadModuleTemplatesAsync()
    {
        ModuleTemplates.Clear();
        ModuleTemplates.Add(new TemplateChoice("Blank (no preset)", null));
        if (SelectedType is { } choice)
        {
            var presets = await _templates.GetModuleTemplatesAsync(choice.Type.Id);
            foreach (var t in presets)
                ModuleTemplates.Add(new TemplateChoice(t.IsBuiltIn ? $"{t.Name} (built-in)" : t.Name, t));
        }
        SelectedModuleTemplate = ModuleTemplates.FirstOrDefault();
    }

    private Task AddModuleAsync()
    {
        if (SelectedType is not { } typeChoice)
            return Task.CompletedTask;

        var template = SelectedModuleTemplate?.Template;
        var defaultName = template?.Name ?? typeChoice.Type.Name;
        var moduleName = string.IsNullOrWhiteSpace(NewModuleName) ? defaultName : NewModuleName.Trim();

        var module = template is null
            ? DesignFactory.CreateBlankModule(typeChoice.Type, moduleName)
            : DesignFactory.CreateModuleFromTemplate(template, moduleName);

        _design.Modules.Add(module);
        var item = new ModuleListItem(module, _registry);
        Modules.Add(item);
        // Adding is a deliberate action, so highlight and open the new module for editing.
        SelectedModuleItem = item;
        EnteredModule = item;
        NewModuleName = "";
        StatusMessage = $"Added '{moduleName}'.";
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        ModuleEditor?.CommitToModule();
        await _repo.SaveAsync(_design);
        StatusMessage = "Saved.";
    }

    private async Task SaveAsTemplateAsync()
    {
        ModuleEditor?.CommitToModule();
        var template = DesignFactory.SnapshotAsTemplate(_design, _design.Name);
        await _templates.SaveDesignTemplateAsync(template);
        StatusMessage = $"Saved '{_design.Name}' as a template.";
    }

    private async Task BackAsync()
    {
        await SaveAsync();
        await _navigator.NavigateBackAsync(this);
    }
}
