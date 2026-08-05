extends Node

# Test harness: synthesize a key press and push it through the viewport so the engine
# delivers _Input / _UnhandledInput to the C# MyNode. push_input dispatches
# synchronously, so the C# overrides run before this frame returns.

var _done := false
var _quit_frames := 0


func _process(_delta):
	# Exported mobile runs have no --quit-after CLI switch: leave after a few
	# frames so the launch harness (simctl launch --console-pty) gets a clean
	# app exit instead of a hang. Desktop/headless runs are unaffected.
	if OS.has_feature("ios") or OS.has_feature("android"):
		_quit_frames += 1
		if _quit_frames >= 30:
			get_tree().quit()
	if _done:
		return
	_done = true
	var ev := InputEventKey.new()
	ev.keycode = KEY_A
	ev.pressed = true
	get_viewport().push_input(ev)
