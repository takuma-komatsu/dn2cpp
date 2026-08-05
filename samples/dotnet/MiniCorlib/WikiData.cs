namespace MiniBcl
{
    // The reflection-ctor route's LIBRARY-target extension (the Thrive GameWiki
    // shape): types DECLARED in a referenced (non-framework) library and constructed
    // only late-bound — a JSON deserializer resolves the target off a Type it was
    // handed, finds a ctor row, invokes it, then populates members via
    // PropertyInfo.SetValue. Nothing here or in the app constructs these types
    // statically; that is the point. The route opens on the NAMED user surface (a
    // generic-method type argument, an opened target's member types) — before the
    // extension the surface walk skipped non-generic library types entirely, so a
    // library type's emitted ctor table read (nullptr, 0) and construction failed
    // ("Unable to find a constructor to use for type GameWiki").
    //
    // The property initializers are deliberate: an initializer stores the BACKING
    // FIELD directly (no set-accessor call), so the accessors stay statically
    // uncalled and the app's SetValue/GetValue section exercises the
    // reflection-only accessor path — exactly GameWiki's `{ get; set; } = null!;`.
    public class WikiPage
    {
        // Newtonsoft's GetDefaultConstructor shape: a public parameterless ctor.
        public string Title { get; set; } = "untitled";

        // A library generic specialization named ONLY as a library member type:
        // the user-surface walk (ReachUserSurfaceNamedSpecializationCtors) is what
        // opens List<WikiSection>'s parameterless ctor — the List<Page> shape.
        public List<WikiSection> Sections { get; set; } = null;
    }

    public class WikiSection
    {
        public string Heading { get; set; } = "empty";
    }

    // The GameWiki.Page shape: NO parameterless ctor. A deserializer binds the
    // single public parameterized ctor (Newtonsoft's fallback when a type has no
    // default ctor) and invokes it through ConstructorInfo.Invoke.
    public class WikiEntry
    {
        public WikiEntry(string name, int level)
        {
            Name = name;
            Level = level;
        }

        public string Name { get; set; }
        public int Level { get; set; }
    }

    // A library element type named NOWHERE except inside WikiChangelog's ctor
    // parameter below — reachable only through that parameter's type argument.
    public class WikiRef
    {
        public string Target { get; set; } = "none";
    }

    // The Thrive VersionPatchNotes creator shape: a parameterized ctor whose
    // collection argument's type is named ONLY as a ctor parameter — the property
    // deliberately stores a count, not the list, so no field/property names
    // List<WikiRef>. A deserializer binding this creator deserializes the argument
    // at the PARAMETER type (Newtonsoft resolves the argument's contract off
    // ParameterInfo.ParameterType), so the reflection-ctor route must put an
    // opened target's instance-ctor parameter types on the named surface; the
    // app's section constructs the parameter type via Activator to assert exactly
    // that.
    public class WikiChangelog
    {
        public WikiChangelog(string version, List<WikiRef> refs)
        {
            Version = version;
            RefCount = refs.Count;
        }

        public string Version { get; set; }
        public int RefCount { get; set; }
    }
}
