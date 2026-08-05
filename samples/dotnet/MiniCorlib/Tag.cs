using System;

namespace MiniBcl
{
    // A custom attribute DECLARED in a referenced (non-framework) library, applied to a
    // class/field/method/property DECLARED in that same library: the attribute tables and
    // the attribute-ctor reach route must not be app-module only, or an attribute out here
    // is invisible to the app's GetCustomAttributes/IsDefined. The app asserts it can read
    // Name/Level and the member attributes back across -r.
    public sealed class TagAttribute : Attribute
    {
        public TagAttribute(string name) { Name = name; }

        public string Name;   // positional-ctor-stored field
        public int Level;     // named field
    }

    [Tag("lib-class", Level = 3)]
    public class Tagged
    {
        [Tag("lib-field", Level = 1)]
        public int Field;

        [Tag("lib-prop", Level = 2)]
        public int Prop { get; set; }

        [Tag("lib-method", Level = 4)]
        public void Method() { }
    }
}
