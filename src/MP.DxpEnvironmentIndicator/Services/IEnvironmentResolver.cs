namespace MP.DxpEnvironmentIndicator.Services;

// The resolved environment to badge. Name is the identity (drives the production-silent rule and the
// live-update keying); Label is the display text shown in the pill (the admin's override, or the
// upper-cased Name by default); Color is the pill background. Null from the resolver means "show
// nothing" (production with the opt-out, or an unmatched host in a non-dev environment).
public sealed record ResolvedEnvironment(string Name, string Label, string Color);

public interface IEnvironmentResolver
{
    ResolvedEnvironment Resolve(string requestHost);
}
