namespace MP.DxpEnvironmentIndicator.Services;

// The resolved badge: the display label and the pill colour. Null means "show nothing" (disabled).
public sealed record ResolvedEnvironment(string Label, string Color);

public interface IEnvironmentResolver
{
    ResolvedEnvironment Resolve();
}
