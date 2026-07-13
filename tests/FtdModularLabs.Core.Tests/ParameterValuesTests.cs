using FtdModularLabs.Core;

namespace FtdModularLabs.Core.Tests;

public class ParameterValuesTests
{
    private static ModuleSchema Schema() => new(new[]
    {
        new ParameterDescriptor("mass", "Mass", ParameterKind.Number, Default: 10.0, Min: 0.0),
        new ParameterDescriptor("count", "Count", ParameterKind.Integer, Min: 1, Max: 5),
        new ParameterDescriptor("mat", "Material", ParameterKind.Enum,
            Options: new[] { "Steel", "Lead" }),
        new ParameterDescriptor("armed", "Armed", ParameterKind.Boolean, Default: false),
        new ParameterDescriptor("label", "Label", ParameterKind.Text, Default: ""),
    });

    [Fact]
    public void GetDouble_coerces_from_string_int_and_double()
    {
        var v = new ParameterValues()
            .Set("a", "3.5").Set("b", 4).Set("c", 2.5);

        Assert.Equal(3.5, v.GetDouble("a"));
        Assert.Equal(4.0, v.GetDouble("b"));
        Assert.Equal(2.5, v.GetDouble("c"));
    }

    [Fact]
    public void GetInt_rounds_double_and_parses_string()
    {
        var v = new ParameterValues().Set("a", 4.0).Set("b", "7");
        Assert.Equal(4, v.GetInt("a"));
        Assert.Equal(7, v.GetInt("b"));
    }

    [Fact]
    public void GetBool_and_GetString_work()
    {
        var v = new ParameterValues().Set("flag", true).Set("name", "hi");
        Assert.True(v.GetBool("flag"));
        Assert.Equal("hi", v.GetString("name"));
    }

    [Fact]
    public void GetEnum_parses_string_option()
    {
        var v = new ParameterValues().Set("k", "Tungsten");
        Assert.Equal(Metal.Tungsten, v.GetEnum<Metal>("k"));
    }

    [Fact]
    public void Require_throws_when_missing()
    {
        var v = new ParameterValues();
        Assert.Throws<KeyNotFoundException>(() => v.GetDouble("nope"));
    }

    [Fact]
    public void Validate_passes_for_valid_values()
    {
        var v = new ParameterValues()
            .Set("mass", 12.0).Set("count", 3).Set("mat", "Steel").Set("armed", true).Set("label", "x");

        Assert.Empty(v.Validate(Schema()));
    }

    [Fact]
    public void Validate_reports_missing_required_but_allows_defaulted()
    {
        // Only "count" and "mat" are required (no defaults); omit both.
        var v = new ParameterValues();
        var errors = v.Validate(Schema());

        Assert.Contains(errors, e => e.Contains("Count"));
        Assert.Contains(errors, e => e.Contains("Material"));
        Assert.DoesNotContain(errors, e => e.Contains("Mass"));   // has a default
    }

    [Fact]
    public void Validate_enforces_numeric_range_and_integer_kind()
    {
        var v = new ParameterValues()
            .Set("mass", -1.0).Set("count", 9).Set("mat", "Steel");
        var errors = v.Validate(Schema());

        Assert.Contains(errors, e => e.Contains("Mass") && e.Contains(">="));
        Assert.Contains(errors, e => e.Contains("Count") && e.Contains("<="));
    }

    [Fact]
    public void Validate_rejects_non_integer_for_integer_kind()
    {
        var v = new ParameterValues().Set("count", 2.5).Set("mat", "Steel");
        var errors = v.Validate(Schema());
        Assert.Contains(errors, e => e.Contains("Count") && e.Contains("whole number"));
    }

    [Fact]
    public void Validate_rejects_unknown_enum_option()
    {
        var v = new ParameterValues().Set("count", 2).Set("mat", "Plastic");
        var errors = v.Validate(Schema());
        Assert.Contains(errors, e => e.Contains("Material") && e.Contains("one of"));
    }

    [Fact]
    public void WithDefaults_fills_missing_values()
    {
        var merged = new ParameterValues().Set("count", 2).Set("mat", "Steel")
            .WithDefaults(Schema());

        Assert.Equal(10.0, merged.GetDouble("mass"));
        Assert.False(merged.GetBool("armed"));
    }

    private enum Metal { Steel, Lead, Tungsten }
}
