extends SceneTree

# Drives the SdkSample C# node (standard Godot C# compiled against the GodotSharp
# shim, transpiled to C++ and loaded as a GDExtension). Adding the node to the
# tree triggers its overridden _Ready(), which calls GD.Print and emits a signal.
func _initialize():
	var node = MyNode.new()
	node.connect("Pinged", func(): print("GDScript: got Pinged signal"))
	root.add_child(node)
	print("GDScript: Health = ", node.Health)
	node.Health = 75
	print("GDScript: Health after set = ", node.Health)
	print("GDScript: SDK sample OK")
	quit()
