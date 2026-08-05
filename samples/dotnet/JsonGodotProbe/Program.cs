using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

// GodotApi model-shape probe: mirrors the real GodotApi.cs extension_api.json model
// (same [JsonPropertyName] graph: classes/methods/properties + utility_functions
// + builtin_classes/members/constructors/constants/operators) so the source-gen
// converter surface it reaches equals what dn2cpp's own engine-binding load path
// (Dn2Cpp.Godot.GodotApi.Load) reaches — but as a standalone exe with a clean,
// JSON-focused reachability root (not the whole transpiler). Deserializes a
// representative fragment exercising every nested shape. dotnet <dll> output is
// the diff oracle.

namespace JsonGodotProbe;

public sealed class ClassApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("inherits")] public string Inherits { get; set; } = "";
    [JsonPropertyName("methods")] public List<MethodApi> Methods { get; set; }
    [JsonPropertyName("properties")] public List<PropertyApi> Properties { get; set; }
}

public sealed class MethodApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("hash")] public long Hash { get; set; }
    [JsonPropertyName("is_static")] public bool IsStatic { get; set; }
    [JsonPropertyName("is_virtual")] public bool IsVirtual { get; set; }
    [JsonPropertyName("return_value")] public ReturnValueApi ReturnValue { get; set; }
    [JsonPropertyName("return_type")] public string ReturnType { get; set; }
    [JsonPropertyName("arguments")] public List<ArgumentApi> Arguments { get; set; }
}

public sealed class ReturnValueApi
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("meta")] public string Meta { get; set; }
}

public sealed class ArgumentApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("meta")] public string Meta { get; set; }
}

public sealed class PropertyApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("getter")] public string Getter { get; set; } = "";
    [JsonPropertyName("setter")] public string Setter { get; set; } = "";
}

public sealed class UtilityFunctionApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("is_vararg")] public bool IsVararg { get; set; }
    [JsonPropertyName("return_type")] public string ReturnType { get; set; }
    [JsonPropertyName("arguments")] public List<ArgumentApi> Arguments { get; set; }
}

public sealed class BuiltinClassApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("members")] public List<MemberApi> Members { get; set; }
    [JsonPropertyName("constructors")] public List<ConstructorApi> Constructors { get; set; }
    [JsonPropertyName("constants")] public List<ConstantApi> Constants { get; set; }
    [JsonPropertyName("operators")] public List<OperatorApi> Operators { get; set; }
    [JsonPropertyName("methods")] public List<MethodApi> Methods { get; set; }
    [JsonPropertyName("has_destructor")] public bool HasDestructor { get; set; }
}

public sealed class OperatorApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("right_type")] public string RightType { get; set; }
    [JsonPropertyName("return_type")] public string ReturnType { get; set; } = "";
}

public sealed class MemberApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
}

public sealed class ConstructorApi
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("arguments")] public List<ArgumentApi> Arguments { get; set; }
}

public sealed class ConstantApi
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
}

public sealed class ExtensionApi
{
    [JsonPropertyName("classes")] public List<ClassApi> Classes { get; set; }
    [JsonPropertyName("utility_functions")] public List<UtilityFunctionApi> UtilityFunctions { get; set; }
    [JsonPropertyName("builtin_classes")] public List<BuiltinClassApi> BuiltinClasses { get; set; }
}

[JsonSerializable(typeof(ExtensionApi))]
internal partial class GodotApiProbeCtx : JsonSerializerContext
{
}

public static class Program
{
    // Representative fragment: one Object-derived class with a method (return_value
    // object + arguments) and a property, one utility function, and one builtin
    // class with members/constructor/constant/operator/method — every nested shape.
    private const string Json = """
    {
      "classes": [
        { "name": "Node", "inherits": "Object",
          "methods": [ { "name": "add_child", "hash": 1234567890, "is_static": false,
                         "is_virtual": false,
                         "return_value": { "type": "void", "meta": null },
                         "arguments": [ { "name": "node", "type": "Node", "meta": null } ] } ],
          "properties": [ { "name": "name", "type": "StringName",
                            "getter": "get_name", "setter": "set_name" } ] }
      ],
      "utility_functions": [
        { "name": "abs", "category": "math", "is_vararg": false,
          "return_type": "float",
          "arguments": [ { "name": "x", "type": "float", "meta": "double" } ] }
      ],
      "builtin_classes": [
        { "name": "Vector2", "has_destructor": false,
          "members": [ { "name": "x", "type": "float" }, { "name": "y", "type": "float" } ],
          "constructors": [ { "index": 0, "arguments": [] } ],
          "constants": [ { "name": "ZERO", "type": "Vector2", "value": "Vector2(0, 0)" } ],
          "operators": [ { "name": "+", "right_type": "Vector2", "return_type": "Vector2" } ],
          "methods": [ { "name": "length", "hash": 999, "is_static": false,
                         "is_virtual": false, "return_type": "float", "arguments": [] } ] }
      ]
    }
    """;

    public static int Main()
    {
        ExtensionApi api = JsonSerializer.Deserialize(Json, GodotApiProbeCtx.Default.ExtensionApi);
        ClassApi node = api.Classes[0];
        UtilityFunctionApi abs = api.UtilityFunctions[0];
        BuiltinClassApi vec = api.BuiltinClasses[0];
        System.Console.WriteLine(node.Name + " <- " + node.Inherits);
        System.Console.WriteLine(node.Methods[0].Name + " #" + node.Methods[0].Hash
            + " -> " + node.Methods[0].ReturnValue.Type
            + " (" + node.Methods[0].Arguments[0].Type + ")");
        System.Console.WriteLine(node.Properties[0].Name + ": " + node.Properties[0].Type);
        System.Console.WriteLine(abs.Name + "/" + abs.Category + " -> " + abs.ReturnType);
        System.Console.WriteLine(vec.Name + " members=" + vec.Members.Count
            + " op0=" + vec.Operators[0].Name + " const0=" + vec.Constants[0].Name
            + " m0=" + vec.Methods[0].Name);
        return 0;
    }
}
