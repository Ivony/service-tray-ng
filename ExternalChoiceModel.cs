namespace service_tray_ng;

public enum ExternalServiceAction
{
    Attach,
    Kill,
    StartNew,
}

/// <summary>
/// Pure decision logic for the external-process dialog: how detected instances are
/// ordered, whether closing the other instances is offered, and what the user's
/// selection resolves to. Kept free of UI so it can be unit-tested without a form.
/// </summary>
public sealed class ExternalChoiceModel
{
    private ExternalServiceAction _action;
    private ListeningProcess? _selectedProcess;
    private bool _closeOthers;

    public ExternalChoiceModel(int configuredPort, IReadOnlyList<ListeningProcess> processes)
    {
        // Instances matching the configured port first, then by port number, so the
        // default selection is the least surprising one.
        SortedProcesses = processes
            .OrderByDescending(process => process.Port == configuredPort)
            .ThenBy(process => process.Port)
            .ToArray();
        CanCloseOthers = SortedProcesses.Count > 1;
    }

    /// <summary>Detected instances, configured-port match first, then by port number.</summary>
    public IReadOnlyList<ListeningProcess> SortedProcesses { get; }

    /// <summary>Whether closing the remaining instances is offered (only with several running).</summary>
    public bool CanCloseOthers { get; }

    public ExternalServiceAction Action => _action;

    public ListeningProcess? SelectedProcess => _selectedProcess;

    public bool CloseOthers => _closeOthers;

    /// <summary>Instances to close when the user chose to attach and close the others.</summary>
    public IReadOnlyList<ListeningProcess> ProcessesToClose =>
        _action == ExternalServiceAction.Attach && _closeOthers && _selectedProcess is not null
            ? SortedProcesses.Where(process => process.ProcessId != _selectedProcess.ProcessId).ToArray()
            : [];

    public void SelectAttach(ListeningProcess process)
    {
        _selectedProcess = process;
        _action = ExternalServiceAction.Attach;
        if (!CanCloseOthers)
            _closeOthers = false;
    }

    public void SelectKill()
    {
        _action = ExternalServiceAction.Kill;
        _selectedProcess = null;
        _closeOthers = false;
    }

    public void SelectStartNew()
    {
        _action = ExternalServiceAction.StartNew;
        _selectedProcess = null;
        _closeOthers = false;
    }

    /// <summary>Enables or disables closing the other instances. Only takes effect while attaching to a specific instance.</summary>
    public void SetCloseOthers(bool value)
    {
        _closeOthers = value && CanCloseOthers && _action == ExternalServiceAction.Attach && _selectedProcess is not null;
    }
}
