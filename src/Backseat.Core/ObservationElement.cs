namespace Backseat.Core;

public sealed record ObservationElementFrame(double X, double Y, double Width, double Height);

public sealed record ObservationElement(
    string Role,
    string? Label,
    string? Value,
    string? ElementToken,
    ObservationElementFrame? Frame,
    IReadOnlyList<string> Actions,
    int Depth,
    bool Enabled);
