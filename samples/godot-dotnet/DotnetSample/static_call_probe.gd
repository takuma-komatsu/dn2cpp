extends Node

# Run after the child exit markers so the existing output remains a prefix.
func _exit_tree():
	var script_type = load("res://GenericConcrete.cs")
	var own = script_type.call("StaticFormat", 23, "alpha")
	var inherited = script_type.call("StaticBase", 19, 28)
	var repeated = script_type.call("StaticFormat", 91, "beta")
	print("DN2CPP_DM_STATIC own=%s base=%s repeat=%s" % [own, inherited, repeated])
