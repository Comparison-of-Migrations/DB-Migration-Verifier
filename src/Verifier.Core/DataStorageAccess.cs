using System.Runtime.CompilerServices;

// The data layer reuses the same private-file and atomic-write implementation
// for explicit local settings and temporary observation baselines; it does not
// get a second weaker persistence path.
[assembly: InternalsVisibleTo("Verifier.Data")]
