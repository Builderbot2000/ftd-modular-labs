using System.Collections.ObjectModel;
using FtdOptima.Core;

namespace FtdOptima.App.Presentation;

/// <summary>
/// Shell view model: receives every discovered <see cref="ICalculationModule"/> via DI and
/// exposes them for the master list. Selecting one spins up a <see cref="ModuleRunnerViewModel"/>
/// that renders its generic form and results.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public MainViewModel(IEnumerable<ICalculationModule> modules)
    {
        Modules = new ObservableCollection<ICalculationModule>(
            modules.OrderBy(m => m.Name));
        SelectedModule = Modules.FirstOrDefault();
    }

    public string Title => "FTD Optima";

    public ObservableCollection<ICalculationModule> Modules { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Runner))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ICalculationModule? selectedModule;

    [ObservableProperty]
    private ModuleRunnerViewModel? runner;

    public bool HasSelection => SelectedModule is not null;

    partial void OnSelectedModuleChanged(ICalculationModule? value)
    {
        Runner = value is null ? null : new ModuleRunnerViewModel(value);
    }
}
