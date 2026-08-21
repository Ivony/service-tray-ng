using Xunit;

namespace service_tray_ng.Tests;

public class ExternalChoiceModelTests
{
    private static readonly ListeningProcess P1 = new(111, "dsh", "127.0.0.1:4096", 4096);
    private static readonly ListeningProcess P2 = new(222, "dsh", "127.0.0.1:3080", 3080);
    private static readonly ListeningProcess P3 = new(333, "dsh", "127.0.0.1:5000", 5000);

    [Fact]
    public void Sort_ConfiguredPortMatchFirst_ThenByPort()
    {
        // Deliberately unsorted input: the configured-port instance must come first,
        // remaining instances ordered by port number.
        var model = new ExternalChoiceModel(configuredPort: 3080, [P1, P2, P3]);

        Assert.Equal([P2, P1, P3], model.SortedProcesses);
    }

    [Fact]
    public void CanCloseOthers_OnlyWithMultipleInstances()
    {
        Assert.True(new ExternalChoiceModel(3080, [P1, P2]).CanCloseOthers);
        Assert.False(new ExternalChoiceModel(3080, [P1]).CanCloseOthers);
        Assert.False(new ExternalChoiceModel(3080, []).CanCloseOthers);
    }

    [Fact]
    public void SelectAttach_PicksExactInstance()
    {
        var model = new ExternalChoiceModel(3080, [P1, P2]);

        model.SelectAttach(P1);

        Assert.Equal(ExternalServiceAction.Attach, model.Action);
        Assert.Equal(P1, model.SelectedProcess);
    }

    [Fact]
    public void SetCloseOthers_OnlyTakesEffectWhileAttaching()
    {
        var model = new ExternalChoiceModel(3080, [P1, P2]);

        model.SelectAttach(P1);
        model.SetCloseOthers(true);
        Assert.True(model.CloseOthers);

        // Selecting "close all" must reset the flag, and a later SetCloseOthers
        // while not attaching must not re-enable it.
        model.SelectKill();
        model.SetCloseOthers(true);
        Assert.False(model.CloseOthers);
    }

    [Fact]
    public void ProcessesToClose_ExcludesSelectedAndOnlyWhenCloseOthers()
    {
        var model = new ExternalChoiceModel(3080, [P1, P2, P3]);

        model.SelectAttach(P2);
        model.SetCloseOthers(true);
        Assert.Equal([P1, P3], model.ProcessesToClose);

        model.SetCloseOthers(false);
        Assert.Empty(model.ProcessesToClose);
    }

    [Fact]
    public void SelectKill_ResetsSelectionAndCloseOthers()
    {
        var model = new ExternalChoiceModel(3080, [P1, P2]);
        model.SelectAttach(P1);
        model.SetCloseOthers(true);

        model.SelectKill();

        Assert.Equal(ExternalServiceAction.Kill, model.Action);
        Assert.Null(model.SelectedProcess);
        Assert.False(model.CloseOthers);
        Assert.Empty(model.ProcessesToClose);
    }

    [Fact]
    public void SelectStartNew_ResetsSelectionAndCloseOthers()
    {
        var model = new ExternalChoiceModel(3080, [P1, P2]);
        model.SelectAttach(P1);
        model.SetCloseOthers(true);

        model.SelectStartNew();

        Assert.Equal(ExternalServiceAction.StartNew, model.Action);
        Assert.Null(model.SelectedProcess);
        Assert.False(model.CloseOthers);
        Assert.Empty(model.ProcessesToClose);
    }

    [Fact]
    public void SingleInstance_CloseOthersNeverTakesEffect()
    {
        var model = new ExternalChoiceModel(3080, [P1]);

        model.SelectAttach(P1);
        model.SetCloseOthers(true);

        Assert.False(model.CloseOthers);
        Assert.Empty(model.ProcessesToClose);
    }
}
