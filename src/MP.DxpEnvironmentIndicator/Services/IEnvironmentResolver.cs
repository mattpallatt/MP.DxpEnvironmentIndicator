namespace MP.DxpEnvironmentIndicator.Services;

// The resolved environment to badge: a display name and a CSS colour. Null from the resolver means
// "show nothing" (production with the opt-out, or an unmatched host in a non-dev environment).
public sealed record ResolvedEnvironment(string Name, string Color);

public interface IEnvironmentResolver
{
    ResolvedEnvironment Resolve(string requestHost);
}
