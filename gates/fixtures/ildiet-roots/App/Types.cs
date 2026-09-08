extern alias real;
extern alias decoy;

namespace RootApp;

public sealed class RegisteredScript : real::Collision.Parent<int> { }
public sealed class OrdinaryClass : decoy::Collision.Parent<int> { }
