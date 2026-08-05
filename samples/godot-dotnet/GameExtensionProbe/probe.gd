# Prints one marker and quits, so the run ends on its own in both arms of the
# staging oracle — including the missing-extension arm, which must still reach
# here since a failed GDExtension load is an engine error and not a fatal.
extends Node


func _ready() -> void:
	print("GAMEEXT_PROBE_SCENE_READY")
	get_tree().quit()
