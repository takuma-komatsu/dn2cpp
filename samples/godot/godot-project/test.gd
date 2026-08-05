extends SceneTree

# Signal payloads from the ResourcePayload connection below. A member, not a
# lambda-captured local: the emission happens on the first frame, since children
# added to root during _initialize only enter the tree once the loop starts.
var _resource_payloads := []
var _end_frames := 0
var _managed_node = null
var _dropped_ids := []
var _handoff_id := -1

# Frame-deferred tail: _initialize cannot observe anything _Ready-driven, so the
# final checks and quit() run a few frames into the main loop. Plain `if` rather
# than assert — a failed assert would abort before quit() and hang the run.
func _end_check():
	_end_frames += 1
	if _end_frames == 1:
		# Virtual trampoline Variant arm: _post_process_key_value takes each applied key's
		# value as a Variant and its Variant return is what the engine applies. Runs here
		# rather than in _initialize because track path resolution needs the nodes inside
		# the live tree.
		var holder := Node2D.new()
		var ap = MyAnimPlayer.new()
		holder.add_child(ap)
		root.add_child(holder)
		var anim := Animation.new()
		anim.length = 1.0
		var tr := anim.add_track(Animation.TYPE_VALUE)
		anim.track_set_path(tr, ".:position")
		anim.track_insert_key(tr, 0.0, Vector2(5, 7))
		var lib := AnimationLibrary.new()
		lib.add_animation("probe", anim)
		ap.add_animation_library("gate", lib)
		ap.play("gate/probe")
		ap.advance(0.1)
		if ap.PostProcessCalls() > 0 and ap.LastKeyValue().is_equal_approx(Vector2(5, 7)) \
				and holder.position.is_equal_approx(Vector2(6, 8)):
			print("GDScript: virtual _PostProcessKeyValue Variant arm round-trips")
		else:
			printerr("FAIL: _PostProcessKeyValue Variant arm: calls=", ap.PostProcessCalls(),
				" last=", ap.LastKeyValue(), " applied=", holder.position)
		root.remove_child(holder)
		holder.free()
		return
	if _end_frames < 3:
		return
	if _end_frames == 3:
		# A signal argument carrying an engine object that is neither Node nor Node2D (a
		# registered RefCounted): the emit-side conversion must produce an Object Variant —
		# NIL means the managed shim fell through the Object detection.
		if _resource_payloads.size() == 1 and _resource_payloads[0] != null \
				and _resource_payloads[0] is MyResource and _resource_payloads[0].DoubledTag() == 14:
			print("GDScript: signal Object argument round-trips as a live object")
		else:
			printerr("FAIL: ResourcePayload signal argument did not round-trip: ", _resource_payloads)
		_resource_payloads.clear()

		# Finalizer-drain starvation: drop the managed node so nothing in the tree has
		# managed process virtuals, then queue reclamation garbage. The collect only QUEUES
		# the finalizers, so running them now depends solely on a node-independent per-frame
		# drain. The ids must all still be alive at this instant.
		root.remove_child(_managed_node)
		_managed_node.free()
		_managed_node = null
		_dropped_ids = GodotApi.MakeDroppedResourceIds(32)
		var alive := 0
		for id in _dropped_ids:
			if is_instance_id_valid(id):
				alive += 1
		if alive != _dropped_ids.size():
			printerr("FAIL: dropped resources were reclaimed before any frame elapsed")
		# The same starvation shape for a plain managed class with a real C# destructor:
		# its Finalize bodies must run via that same per-frame drain.
		GodotApi.DropDestructorProbes(32)
		if GodotApi.DestructorRunCount() != 0:
			printerr("FAIL: managed destructors ran before any frame elapsed")
		return
	if _end_frames < 8:
		return
	process_frame.disconnect(_end_check)
	# Several frames with no managed node in the tree: the queued reclamation finalizers
	# must have run. Conservative stack scanning may pin a stray survivor, so require
	# most — not all — of the batch to be gone.
	var freed := 0
	for id in _dropped_ids:
		if not is_instance_id_valid(id):
			freed += 1
	if freed >= _dropped_ids.size() / 2:
		print("GDScript: per-frame finalizer drain runs with no managed node in tree")
	else:
		printerr("FAIL: finalizer drain starved with no managed node in tree (freed=", freed, "/", _dropped_ids.size(), ")")
	_dropped_ids = []
	# The managed destructor bodies (~T()) queued alongside must have run too.
	if GodotApi.DestructorRunCount() > 0:
		print("GDScript: managed destructor ran via per-frame drain")
	else:
		printerr("FAIL: managed destructors starved with no managed node in tree")

	# The hand-off resource's anchor was released by a per-frame sweep once this script
	# dropped its own holder, so a fresh collect + drain must now reclaim it for real.
	GodotApi.ForceGcAndDrain()
	if not is_instance_id_valid(_handoff_id):
		print("GDScript: RefCounted engine hand-off reclaimed once the engine dropped it too")
	else:
		printerr("FAIL: RefCounted engine hand-off was never reclaimed after the engine dropped it")

	print("GDScript: ALL OK")
	quit()

# Calls C# code that was transpiled to C++ and loaded as a GDExtension.
func _initialize():
	# ClassDB registration has no fixed class limit. This sample registers 78 classes, so
	# reaching this line means the node-class loop passed 64 without aborting. Existence
	# is not enough: a registry naming a class the engine cannot build would satisfy a
	# count check and ship broken, so every one is also instantiated through ClassDB —
	# the create_instance path — and freed.
	var many_ok := 0
	var many_bad := ""
	for i in range(1, 71):
		var cname := "GateManyClass%02d" % i
		if not ClassDB.class_exists(cname):
			many_bad = cname + " (not registered)"
			break
		var inst = ClassDB.instantiate(cname)
		if inst == null or not (inst is Node) or inst.get_class() != cname:
			many_bad = cname + " (instantiate returned " + str(inst) + ")"
			if inst != null:
				inst.free()
			break
		inst.free()
		many_ok += 1
	if many_ok == 70:
		print("GDScript: registration past the old 64-class cap works (", many_ok, " classes)")
	else:
		printerr("FAIL: class registry stopped at ", many_ok, " of 70: ", many_bad)

	print("GDScript: GodotApi.Add(2, 3) = ", GodotApi.Add(2, 3))
	print("GDScript: GodotApi.Fib(10) = ", GodotApi.Fib(10))
	print("GDScript: GodotApi.IsEven(7) = ", GodotApi.IsEven(7))
	print("GDScript: ", GodotApi.Describe("Godot"))
	print("GDScript: AnimalChorus -> ", GodotApi.AnimalChorus())
	print("GDScript: VecLengthSquared(3, 4) = ", GodotApi.VecLengthSquared(3.0, 4.0))
	print("GDScript: VecLengthSquared2(Vector2(3, 4)) = ", GodotApi.VecLengthSquared2(Vector2(3.0, 4.0)))

	# Generalized engine virtuals (reverse dispatch to NON-lifecycle overrides).
	# MyControl overrides two arbitrary engine `_*` virtuals; constructing it via ClassDB
	# `.new()` and calling the public methods that invoke them proves the engine
	# dispatches into the C# override. Plain `if`/printerr, never assert, so a failure
	# fails the gate instead of hanging it.
	var mc = MyControl.new()
	var mc_min = mc.get_minimum_size()
	if mc_min.is_equal_approx(Vector2(123, 45)):
		print("GDScript: engine virtual _GetMinimumSize dispatch matches")
	else:
		printerr("FAIL: _GetMinimumSize reverse dispatch returned ", mc_min)
	var mc_tip = mc.get_tooltip(Vector2(7, 0))
	if mc_tip == "tip@7":
		print("GDScript: engine virtual _GetTooltip dispatch matches")
	else:
		printerr("FAIL: _GetTooltip reverse dispatch returned '", mc_tip, "'")
	mc.free()

	# Enum / Quaternion / Plane / AABB across the export boundary: a user-enum [Export]
	# registers as INT, the builtins as their own Variant types, the same kinds pass
	# through method arguments and returns, and an enum-argument signal delivers its
	# numeric payload.
	var xn = MyNode.new()
	var mode_ok = xn.Mode == 3
	xn.Mode = 7
	mode_ok = mode_ok and xn.Mode == 7 and xn.NextMode(0) == 3 and xn.NextMode(7) == 0
	if mode_ok:
		print("GDScript: enum export property + method round trip")
	else:
		printerr("FAIL: enum export boundary: Mode=", xn.Mode, " NextMode(0)=", xn.NextMode(0))
	var enum_vals := []
	xn.connect("EnumModeChanged", func(m): enum_vals.append(m))
	xn.RaiseEnumMode(7)
	if enum_vals == [7]:
		print("GDScript: enum signal registered + dispatches")
	else:
		printerr("FAIL: enum signal EnumModeChanged: got ", enum_vals)
	var spin_q := Quaternion(0.1, 0.2, 0.3, 0.9)
	xn.Spin = spin_q
	var clip_pl := Plane(Vector3(0.6, 0.8, 0.0), 5.0)
	xn.Clip = clip_pl
	var aabb_v := AABB(Vector3(1, 2, 3), Vector3(4, 5, 6))
	xn.Bounds = aabb_v
	if xn.Spin.is_equal_approx(spin_q) and xn.Clip.is_equal_approx(clip_pl) and xn.Bounds.is_equal_approx(aabb_v):
		print("GDScript: Quaternion/Plane/AABB export properties round trip")
	else:
		printerr("FAIL: Q/P/AABB exports: ", xn.Spin, " ", xn.Clip, " ", xn.Bounds)
	var cq = xn.ConjugateOf(Quaternion(0.5, -0.5, 0.25, 1.0))
	var gb = xn.GrowBounds(AABB(Vector3(1, 1, 1), Vector3(2, 3, 4)))
	if cq.is_equal_approx(Quaternion(-0.5, 0.5, -0.25, 1.0)) \
			and gb.is_equal_approx(AABB(Vector3(1, 1, 1), Vector3(4, 6, 8))):
		print("GDScript: Quaternion/AABB exported method args round trip")
	else:
		printerr("FAIL: Q/AABB method args: ", cq, " ", gb)
	xn.free()

	# Matrix transform operators: cross-check the hand-written shim operators against
	# NATIVE Godot, so a wrong row/column convention fails loudly.
	var t2d_col0 = Vector2(2, 0)
	var t2d_col1 = Vector2(0, 3)
	var t2d_origin = Vector2(10, 20)
	var t2d_v = Vector2(5, 7)
	var t2d_native = Transform2D(t2d_col0, t2d_col1, t2d_origin) * t2d_v
	var t2d_cs_x = GodotApi.Transform2DXformX(t2d_col0, t2d_col1, t2d_origin, t2d_v)
	var t2d_cs_y = GodotApi.Transform2DXformY(t2d_col0, t2d_col1, t2d_origin, t2d_v)
	print("GDScript: Transform2D*Vector2 C#=(", t2d_cs_x, ", ", t2d_cs_y, ") native=", t2d_native)
	assert(is_equal_approx(t2d_cs_x, t2d_native.x), "Transform2D*Vector2 X mismatch")
	assert(is_equal_approx(t2d_cs_y, t2d_native.y), "Transform2D*Vector2 Y mismatch")

	# Basis * Vector3: an asymmetric basis catches a transposed convention. The columns
	# must match GodotApi.CrossCheckBasis() verbatim.
	var b_col0 = Vector3(1, 2, 3)
	var b_col1 = Vector3(4, 5, 6)
	var b_col2 = Vector3(7, 8, 10)
	var b_v = Vector3(1, 2, 3)
	var b_native = Basis(b_col0, b_col1, b_col2) * b_v
	var b_cs_x = GodotApi.BasisXformX(b_v.x, b_v.y, b_v.z)
	var b_cs_y = GodotApi.BasisXformY(b_v.x, b_v.y, b_v.z)
	var b_cs_z = GodotApi.BasisXformZ(b_v.x, b_v.y, b_v.z)
	print("GDScript: Basis*Vector3 C#=(", b_cs_x, ", ", b_cs_y, ", ", b_cs_z, ") native=", b_native)
	assert(is_equal_approx(b_cs_x, b_native.x), "Basis*Vector3 X mismatch")
	assert(is_equal_approx(b_cs_y, b_native.y), "Basis*Vector3 Y mismatch")
	assert(is_equal_approx(b_cs_z, b_native.z), "Basis*Vector3 Z mismatch")

	# Transform3D * Vector3 = Basis * v + Origin; basis columns and origin mirror
	# GodotApi.CrossCheckTransform3D() verbatim.
	var t3d_origin = Vector3(100, 200, 300)
	var t3d_native = Transform3D(Basis(b_col0, b_col1, b_col2), t3d_origin) * b_v
	var t3d_cs_x = GodotApi.Transform3DXformX(b_v.x, b_v.y, b_v.z)
	var t3d_cs_y = GodotApi.Transform3DXformY(b_v.x, b_v.y, b_v.z)
	var t3d_cs_z = GodotApi.Transform3DXformZ(b_v.x, b_v.y, b_v.z)
	print("GDScript: Transform3D*Vector3 C#=(", t3d_cs_x, ", ", t3d_cs_y, ", ", t3d_cs_z, ") native=", t3d_native)
	assert(is_equal_approx(t3d_cs_x, t3d_native.x), "Transform3D*Vector3 X mismatch")
	assert(is_equal_approx(t3d_cs_y, t3d_native.y), "Transform3D*Vector3 Y mismatch")
	assert(is_equal_approx(t3d_cs_z, t3d_native.z), "Transform3D*Vector3 Z mismatch")

	# Matrix compositions (A * B): compose natively and probe with the basis vectors,
	# origin and a general vector so every composed element is compared.
	var t2a = Transform2D(Vector2(2, 0), Vector2(0, 3), Vector2(10, 20))
	var t2b = Transform2D(Vector2(1, 2), Vector2(3, 4), Vector2(5, 6))
	var t2c = t2a * t2b
	for pv in [Vector2(0, 0), Vector2(1, 0), Vector2(0, 1), Vector2(3, 5)]:
		var nat = t2c * pv
		assert(is_equal_approx(GodotApi.Transform2DMulXformX(pv.x, pv.y), nat.x), "Transform2D*Transform2D X mismatch")
		assert(is_equal_approx(GodotApi.Transform2DMulXformY(pv.x, pv.y), nat.y), "Transform2D*Transform2D Y mismatch")
	print("GDScript: Transform2D*Transform2D composition matches native")

	var ba = Basis(Vector3(1, 2, 3), Vector3(4, 5, 6), Vector3(7, 8, 10))
	var bb = Basis(Vector3(2, 0, 1), Vector3(1, 3, 0), Vector3(0, 1, 2))
	var bc = ba * bb
	for pv in [Vector3(1, 0, 0), Vector3(0, 1, 0), Vector3(0, 0, 1), Vector3(1, 2, 3)]:
		var nat = bc * pv
		assert(is_equal_approx(GodotApi.BasisMulXformX(pv.x, pv.y, pv.z), nat.x), "Basis*Basis X mismatch")
		assert(is_equal_approx(GodotApi.BasisMulXformY(pv.x, pv.y, pv.z), nat.y), "Basis*Basis Y mismatch")
		assert(is_equal_approx(GodotApi.BasisMulXformZ(pv.x, pv.y, pv.z), nat.z), "Basis*Basis Z mismatch")
	print("GDScript: Basis*Basis composition matches native")

	var t3a = Transform3D(ba, Vector3(100, 200, 300))
	var t3b = Transform3D(bb, Vector3(1, 2, 3))
	var t3c = t3a * t3b
	for pv in [Vector3(0, 0, 0), Vector3(1, 0, 0), Vector3(0, 1, 0), Vector3(0, 0, 1), Vector3(2, 3, 4)]:
		var nat = t3c * pv
		assert(is_equal_approx(GodotApi.Transform3DMulXformX(pv.x, pv.y, pv.z), nat.x), "Transform3D*Transform3D X mismatch")
		assert(is_equal_approx(GodotApi.Transform3DMulXformY(pv.x, pv.y, pv.z), nat.y), "Transform3D*Transform3D Y mismatch")
		assert(is_equal_approx(GodotApi.Transform3DMulXformZ(pv.x, pv.y, pv.z), nat.z), "Transform3D*Transform3D Z mismatch")
	print("GDScript: Transform3D*Transform3D composition matches native")

	# Quaternion * Quaternion (Hamilton product), compared against native Godot.
	var qa = Quaternion(0.1, 0.2, 0.3, 0.9).normalized()
	var qb = Quaternion(0.4, 0.1, 0.2, 0.8).normalized()
	var q_native = qa * qb
	var q_cs_x = GodotApi.QuatMulX(qa.x, qa.y, qa.z, qa.w, qb.x, qb.y, qb.z, qb.w)
	var q_cs_y = GodotApi.QuatMulY(qa.x, qa.y, qa.z, qa.w, qb.x, qb.y, qb.z, qb.w)
	var q_cs_z = GodotApi.QuatMulZ(qa.x, qa.y, qa.z, qa.w, qb.x, qb.y, qb.z, qb.w)
	var q_cs_w = GodotApi.QuatMulW(qa.x, qa.y, qa.z, qa.w, qb.x, qb.y, qb.z, qb.w)
	print("GDScript: Quaternion*Quaternion C#=(", q_cs_x, ", ", q_cs_y, ", ", q_cs_z, ", ", q_cs_w, ") native=", q_native)
	assert(is_equal_approx(q_cs_x, q_native.x), "Quaternion*Quaternion X mismatch")
	assert(is_equal_approx(q_cs_y, q_native.y), "Quaternion*Quaternion Y mismatch")
	assert(is_equal_approx(q_cs_z, q_native.z), "Quaternion*Quaternion Z mismatch")
	assert(is_equal_approx(q_cs_w, q_native.w), "Quaternion*Quaternion W mismatch")

	# Quaternion * Vector3 (rotate). Probe several vectors against native Godot.
	for pv in [Vector3(1, 0, 0), Vector3(0, 1, 0), Vector3(0, 0, 1), Vector3(2, 3, 4)]:
		var nat = qa * pv
		assert(is_equal_approx(GodotApi.QuatXformX(qa.x, qa.y, qa.z, qa.w, pv.x, pv.y, pv.z), nat.x), "Quaternion*Vector3 X mismatch")
		assert(is_equal_approx(GodotApi.QuatXformY(qa.x, qa.y, qa.z, qa.w, pv.x, pv.y, pv.z), nat.y), "Quaternion*Vector3 Y mismatch")
		assert(is_equal_approx(GodotApi.QuatXformZ(qa.x, qa.y, qa.z, qa.w, pv.x, pv.y, pv.z), nat.z), "Quaternion*Vector3 Z mismatch")
	print("GDScript: Quaternion*Vector3 rotation matches native")

	# Vector3 as a real export arg/return type: pass a native Vector3 in, get one back.
	var v3a = Vector3(1, 2, 3)
	var v3b = Vector3(4, 5, 6)
	var cross_cs = GodotApi.Vec3Cross(v3a, v3b)
	print("GDScript: Vec3Cross C#=", cross_cs, " native=", v3a.cross(v3b))
	assert(cross_cs.is_equal_approx(v3a.cross(v3b)), "Vec3Cross mismatch")
	var bc0 = Vector3(1, 2, 3)
	var bc1 = Vector3(4, 5, 6)
	var bc2 = Vector3(7, 8, 10)
	var bx_cs = GodotApi.BasisXform3(bc0, bc1, bc2, v3a)
	assert(bx_cs.is_equal_approx(Basis(bc0, bc1, bc2) * v3a), "BasisXform3 mismatch")
	print("GDScript: Vector3 arg+return round-trip matches native")

	# Quaternion rotation builders compared to native Godot. The axis is normalized,
	# since Godot expects a unit axis.
	var qaxis = Vector3(1, 2, 2).normalized()
	var qangle = 0.7
	var qaa_native = Quaternion(qaxis, qangle)
	print("GDScript: Quaternion(axis,angle) C#=(", GodotApi.QuatAxisAngleX(qaxis, qangle), ", ", GodotApi.QuatAxisAngleY(qaxis, qangle), ", ", GodotApi.QuatAxisAngleZ(qaxis, qangle), ", ", GodotApi.QuatAxisAngleW(qaxis, qangle), ") native=", qaa_native)
	assert(is_equal_approx(GodotApi.QuatAxisAngleX(qaxis, qangle), qaa_native.x), "Quaternion axis-angle X mismatch")
	assert(is_equal_approx(GodotApi.QuatAxisAngleY(qaxis, qangle), qaa_native.y), "Quaternion axis-angle Y mismatch")
	assert(is_equal_approx(GodotApi.QuatAxisAngleZ(qaxis, qangle), qaa_native.z), "Quaternion axis-angle Z mismatch")
	assert(is_equal_approx(GodotApi.QuatAxisAngleW(qaxis, qangle), qaa_native.w), "Quaternion axis-angle W mismatch")

	var euler = Vector3(0.3, -0.5, 0.8)
	var qe_native = Quaternion.from_euler(euler)
	assert(is_equal_approx(GodotApi.QuatFromEulerX(euler), qe_native.x), "Quaternion FromEuler X mismatch")
	assert(is_equal_approx(GodotApi.QuatFromEulerY(euler), qe_native.y), "Quaternion FromEuler Y mismatch")
	assert(is_equal_approx(GodotApi.QuatFromEulerZ(euler), qe_native.z), "Quaternion FromEuler Z mismatch")
	assert(is_equal_approx(GodotApi.QuatFromEulerW(euler), qe_native.w), "Quaternion FromEuler W mismatch")
	print("GDScript: Quaternion axis-angle + FromEuler match native")

	# Quaternion Slerp across the arc; the endpoints mirror GodotApi.SlerpA()/SlerpB().
	var sa = Quaternion(0.1, 0.2, 0.3, 0.9).normalized()
	var sb = Quaternion(0.5, -0.2, 0.1, 0.8).normalized()
	for w in [0.0, 0.25, 0.5, 0.75, 1.0]:
		var nat = sa.slerp(sb, w)
		assert(is_equal_approx(GodotApi.QuatSlerpX(w), nat.x), "Quaternion Slerp X mismatch")
		assert(is_equal_approx(GodotApi.QuatSlerpY(w), nat.y), "Quaternion Slerp Y mismatch")
		assert(is_equal_approx(GodotApi.QuatSlerpZ(w), nat.z), "Quaternion Slerp Z mismatch")
		assert(is_equal_approx(GodotApi.QuatSlerpW(w), nat.w), "Quaternion Slerp W mismatch")
	print("GDScript: Quaternion Slerp matches native")

	# Relational operators: NATIVE Godot compares X first, tie-breaking on Y then Z/W.
	# The probe pairs hit every tie-break branch, so a component-wise convention or a
	# wrong `<` vs `<=` tie-break fails loudly.
	var v2_pairs = [
		[Vector2(1, 5), Vector2(2, 0)],   # X differs (Y would disagree component-wise)
		[Vector2(1, 2), Vector2(1, 9)],   # X equal, Y decides
		[Vector2(3, 4), Vector2(3, 4)],   # fully equal (<= / >= boundary)
	]
	for p in v2_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec2Less(a, b) == (a < b), "Vec2 < mismatch")
		assert(GodotApi.Vec2LessEqual(a, b) == (a <= b), "Vec2 <= mismatch")
		assert(GodotApi.Vec2Greater(a, b) == (a > b), "Vec2 > mismatch")
		assert(GodotApi.Vec2GreaterEqual(a, b) == (a >= b), "Vec2 >= mismatch")
	print("GDScript: Vector2 relational operators match native")

	var v3_pairs = [
		[Vector3(1, 5, 9), Vector3(2, 0, 0)],   # X decides
		[Vector3(1, 2, 9), Vector3(1, 5, 0)],   # Y decides
		[Vector3(1, 2, 3), Vector3(1, 2, 7)],   # Z decides
		[Vector3(1, 2, 3), Vector3(1, 2, 3)],   # fully equal
	]
	for p in v3_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec3Less(a, b) == (a < b), "Vec3 < mismatch")
		assert(GodotApi.Vec3LessEqual(a, b) == (a <= b), "Vec3 <= mismatch")
		assert(GodotApi.Vec3Greater(a, b) == (a > b), "Vec3 > mismatch")
		assert(GodotApi.Vec3GreaterEqual(a, b) == (a >= b), "Vec3 >= mismatch")
	print("GDScript: Vector3 relational operators match native")

	var v4_pairs = [
		[Vector4(1, 5, 9, 9), Vector4(2, 0, 0, 0)],   # X decides
		[Vector4(1, 2, 9, 9), Vector4(1, 5, 0, 0)],   # Y decides
		[Vector4(1, 2, 3, 9), Vector4(1, 2, 7, 0)],   # Z decides
		[Vector4(1, 2, 3, 4), Vector4(1, 2, 3, 8)],   # W decides
		[Vector4(1, 2, 3, 4), Vector4(1, 2, 3, 4)],   # fully equal
	]
	for p in v4_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec4Less(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a < b), "Vec4 < mismatch")
		assert(GodotApi.Vec4LessEqual(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a <= b), "Vec4 <= mismatch")
		assert(GodotApi.Vec4Greater(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a > b), "Vec4 > mismatch")
		assert(GodotApi.Vec4GreaterEqual(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a >= b), "Vec4 >= mismatch")
	print("GDScript: Vector4 relational operators match native")

	# Integer-vector relational operators: the same lexicographic semantics. The
	# i-variants are not export arg types, so components are passed as ints.
	var v2i_pairs = [
		[Vector2i(1, 5), Vector2i(2, 0)],   # X decides
		[Vector2i(1, 2), Vector2i(1, 9)],   # Y decides
		[Vector2i(3, 4), Vector2i(3, 4)],   # fully equal
	]
	for p in v2i_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec2iLess(a.x, a.y, b.x, b.y) == (a < b), "Vec2i < mismatch")
		assert(GodotApi.Vec2iLessEqual(a.x, a.y, b.x, b.y) == (a <= b), "Vec2i <= mismatch")
		assert(GodotApi.Vec2iGreater(a.x, a.y, b.x, b.y) == (a > b), "Vec2i > mismatch")
		assert(GodotApi.Vec2iGreaterEqual(a.x, a.y, b.x, b.y) == (a >= b), "Vec2i >= mismatch")
	print("GDScript: Vector2i relational operators match native")

	var v3i_pairs = [
		[Vector3i(1, 5, 9), Vector3i(2, 0, 0)],   # X decides
		[Vector3i(1, 2, 9), Vector3i(1, 5, 0)],   # Y decides
		[Vector3i(1, 2, 3), Vector3i(1, 2, 7)],   # Z decides
		[Vector3i(1, 2, 3), Vector3i(1, 2, 3)],   # fully equal
	]
	for p in v3i_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec3iLess(a.x, a.y, a.z, b.x, b.y, b.z) == (a < b), "Vec3i < mismatch")
		assert(GodotApi.Vec3iLessEqual(a.x, a.y, a.z, b.x, b.y, b.z) == (a <= b), "Vec3i <= mismatch")
		assert(GodotApi.Vec3iGreater(a.x, a.y, a.z, b.x, b.y, b.z) == (a > b), "Vec3i > mismatch")
		assert(GodotApi.Vec3iGreaterEqual(a.x, a.y, a.z, b.x, b.y, b.z) == (a >= b), "Vec3i >= mismatch")
	print("GDScript: Vector3i relational operators match native")

	var v4i_pairs = [
		[Vector4i(1, 5, 9, 9), Vector4i(2, 0, 0, 0)],   # X decides
		[Vector4i(1, 2, 9, 9), Vector4i(1, 5, 0, 0)],   # Y decides
		[Vector4i(1, 2, 3, 9), Vector4i(1, 2, 7, 0)],   # Z decides
		[Vector4i(1, 2, 3, 4), Vector4i(1, 2, 3, 8)],   # W decides
		[Vector4i(1, 2, 3, 4), Vector4i(1, 2, 3, 4)],   # fully equal
	]
	for p in v4i_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec4iLess(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a < b), "Vec4i < mismatch")
		assert(GodotApi.Vec4iLessEqual(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a <= b), "Vec4i <= mismatch")
		assert(GodotApi.Vec4iGreater(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a > b), "Vec4i > mismatch")
		assert(GodotApi.Vec4iGreaterEqual(a.x, a.y, a.z, a.w, b.x, b.y, b.z, b.w) == (a >= b), "Vec4i >= mismatch")
	print("GDScript: Vector4i relational operators match native")

	# Vector2/3 Slerp, Snapped, Inverse cross-checks
	for w in [0.0, 0.25, 0.5, 0.75, 1.0]:
		var v2a = Vector2(1, 0)
		var v2b = Vector2(0, 1)
		var cs_slerp = GodotApi.Vec2Slerp(v2a, v2b, w)
		var nat_slerp = v2a.slerp(v2b, w)
		assert(cs_slerp.is_equal_approx(nat_slerp), "Vector2 Slerp standard mismatch at weight " + str(w))

		var v2zero = Vector2(0, 0)
		assert(GodotApi.Vec2Slerp(v2zero, v2b, w).is_equal_approx(v2zero.slerp(v2b, w)), "Vector2 Slerp start-zero mismatch")
		assert(GodotApi.Vec2Slerp(v2a, v2zero, w).is_equal_approx(v2a.slerp(v2zero, w)), "Vector2 Slerp end-zero mismatch")

		var v2a_len = Vector2(1, 0) * 2.0
		var v2b_len = Vector2(0, 1) * 3.0
		var cs_slerp_len = GodotApi.Vec2Slerp(v2a_len, v2b_len, w)
		var nat_slerp_len = v2a_len.slerp(v2b_len, w)
		assert(cs_slerp_len.is_equal_approx(nat_slerp_len), "Vector2 Slerp length mismatch")

		var s_v3a = Vector3(1, 0, 0)
		var s_v3b = Vector3(0, 1, 0)
		var cs_slerp3 = GodotApi.Vec3Slerp(s_v3a, s_v3b, w)
		var nat_slerp3 = s_v3a.slerp(s_v3b, w)
		assert(cs_slerp3.is_equal_approx(nat_slerp3), "Vector3 Slerp standard mismatch at weight " + str(w))

		var s_v3zero = Vector3(0, 0, 0)
		var s_v3col = Vector3(2, 0, 0)
		assert(GodotApi.Vec3Slerp(s_v3a, s_v3col, w).is_equal_approx(s_v3a.slerp(s_v3col, w)), "Vector3 Slerp colinear mismatch")
		assert(GodotApi.Vec3Slerp(s_v3zero, s_v3b, w).is_equal_approx(s_v3zero.slerp(s_v3b, w)), "Vector3 Slerp start-zero mismatch")

		var s_v3a_len = Vector3(1, 0, 0) * 2.0
		var s_v3b_len = Vector3(0, 1, 0) * 3.0
		var cs_slerp3_len = GodotApi.Vec3Slerp(s_v3a_len, s_v3b_len, w)
		var nat_slerp3_len = s_v3a_len.slerp(s_v3b_len, w)
		assert(cs_slerp3_len.is_equal_approx(nat_slerp3_len), "Vector3 Slerp length mismatch")
	print("GDScript: Vector2 and Vector3 Slerp matches native")

	# Snapped
	var snap_v2s = [
		[Vector2(1.23, 4.56), Vector2(0.5, 2.0)],
		[Vector2(-1.23, -4.56), Vector2(0.5, 2.0)],
		[Vector2(1.23, 4.56), Vector2(0.0, 1.0)],
	]
	for pair in snap_v2s:
		var v = pair[0]
		var step = pair[1]
		var cs_snapped = GodotApi.Vec2Snapped(v, step)
		var nat_snapped = v.snapped(step)
		assert(cs_snapped.is_equal_approx(nat_snapped), "Vector2 Snapped(Vector2) mismatch")

	var snap_v2s_f = [
		[Vector2(1.23, 4.56), 0.5],
		[Vector2(-1.23, -4.56), 0.5],
		[Vector2(1.23, 4.56), 0.0],
	]
	for pair in snap_v2s_f:
		var v = pair[0]
		var step = pair[1]
		var cs_snapped = GodotApi.Vec2SnappedVal(v, step)
		var nat_snapped = v.snapped(Vector2(step, step))
		assert(cs_snapped.is_equal_approx(nat_snapped), "Vector2 Snapped(float) mismatch")

	var snap_v3s = [
		[Vector3(1.23, 4.56, 7.89), Vector3(0.5, 2.0, 5.0)],
		[Vector3(-1.23, -4.56, -7.89), Vector3(0.5, 2.0, 5.0)],
		[Vector3(1.23, 4.56, 7.89), Vector3(0.0, 1.0, 0.0)],
	]
	for pair in snap_v3s:
		var v = pair[0]
		var step = pair[1]
		var cs_snapped = GodotApi.Vec3Snapped(v, step)
		var nat_snapped = v.snapped(step)
		assert(cs_snapped.is_equal_approx(nat_snapped), "Vector3 Snapped(Vector3) mismatch")

	var snap_v3s_f = [
		[Vector3(1.23, 4.56, 7.89), 0.5],
		[Vector3(-1.23, -4.56, -7.89), 0.5],
		[Vector3(1.23, 4.56, 7.89), 0.0],
	]
	for pair in snap_v3s_f:
		var v = pair[0]
		var step = pair[1]
		var cs_snapped = GodotApi.Vec3SnappedVal(v, step)
		var nat_snapped = v.snapped(Vector3(step, step, step))
		assert(cs_snapped.is_equal_approx(nat_snapped), "Vector3 Snapped(float) mismatch")
	print("GDScript: Vector2 and Vector3 Snapped matches native")

	# Inverse
	for v in [Vector2(1.5, 4.0), Vector2(-2.0, 0.5)]:
		var cs_inv = GodotApi.Vec2Inverse(v)
		var expected = Vector2(1.0 / v.x, 1.0 / v.y)
		assert(cs_inv.is_equal_approx(expected), "Vector2 Inverse mismatch")

	for v in [Vector3(1.5, 4.0, -0.2), Vector3(-2.0, 0.5, 10.0)]:
		var cs_inv = GodotApi.Vec3Inverse(v)
		var expected = Vector3(1.0 / v.x, 1.0 / v.y, 1.0 / v.z)
		assert(cs_inv.is_equal_approx(expected), "Vector3 Inverse mismatch")
	print("GDScript: Vector2 and Vector3 Inverse matches expected values")

	# ---- Vector4 / Color as native export arg+return types ----
	# Both marshal natively, with no float decomposition.
	var v4a = Vector4(1.5, -2.0, 3.25, 0.5)
	var v4b = Vector4(0.5, 4.0, -1.25, 2.0)
	assert(GodotApi.Vec4Add(v4a, v4b).is_equal_approx(v4a + v4b), "Vec4Add mismatch")
	assert(GodotApi.Vec4Mul(v4a, v4b).is_equal_approx(v4a * v4b), "Vec4Mul mismatch")
	for p in v4_pairs:
		var a = p[0]
		var b = p[1]
		assert(GodotApi.Vec4LessV(a, b) == (a < b), "Vec4LessV mismatch")
		assert(GodotApi.Vec4GreaterEqualV(a, b) == (a >= b), "Vec4GreaterEqualV mismatch")
	print("GDScript: Vector4 native export arg+return matches native")

	var c_a = Color(0.2, 0.4, 0.6, 0.8)
	var c_b = Color(0.9, 0.1, 0.5, 1.0)
	assert(GodotApi.ColorInverted(c_a).is_equal_approx(c_a.inverted()), "ColorInverted mismatch")
	for w in [0.0, 0.25, 0.5, 1.0]:
		assert(GodotApi.ColorLerp(c_a, c_b, w).is_equal_approx(c_a.lerp(c_b, w)), "ColorLerp mismatch")
	assert(GodotApi.ColorDarkened(c_a, 0.3).is_equal_approx(c_a.darkened(0.3)), "ColorDarkened mismatch")
	assert(is_equal_approx(GodotApi.ColorLuminance(c_a), c_a.get_luminance()), "ColorLuminance mismatch")
	print("GDScript: Color native export arg+return matches native")

	# ---- Large value types as native export arg+return types ----
	# Asymmetric matrices, so a flipped transpose fails. Each is checked semantically
	# (apply-to-vector for arguments, build-from-columns for returns), not round-tripped.
	var pv2 = Vector2(7, 11)
	var pv3 = Vector3(7, 11, 13)

	var t2d = Transform2D(Vector2(1, 2), Vector2(3, 4), Vector2(5, 6))
	assert(GodotApi.T2DRoundtrip(t2d).is_equal_approx(t2d), "T2DRoundtrip mismatch")
	assert(GodotApi.T2DApply(t2d, pv2).is_equal_approx(t2d * pv2), "T2DApply mismatch")

	var basis = Basis(Vector3(1, 2, 3), Vector3(4, 5, 6), Vector3(7, 8, 10))
	assert(GodotApi.BasisApply(basis, pv3).is_equal_approx(basis * pv3), "BasisApply mismatch")
	assert(GodotApi.BasisFromCols(basis.x, basis.y, basis.z).is_equal_approx(basis), "BasisFromCols mismatch")

	var t3d = Transform3D(basis, Vector3(100, 200, 300))
	assert(GodotApi.T3DApply(t3d, pv3).is_equal_approx(t3d * pv3), "T3DApply mismatch")
	assert(GodotApi.T3DFromParts(t3d.basis, t3d.origin).is_equal_approx(t3d), "T3DFromParts mismatch")

	var proj = Projection(Vector4(1, 2, 3, 4), Vector4(5, 6, 7, 8), Vector4(9, 10, 11, 12), Vector4(13, 14, 15, 16))
	var proj_rt = GodotApi.ProjRoundtrip(proj)
	var proj_fc = GodotApi.ProjFromCols(proj.x, proj.y, proj.z, proj.w)
	for q in [proj_rt, proj_fc]:
		assert(q.x.is_equal_approx(proj.x) and q.y.is_equal_approx(proj.y) \
			and q.z.is_equal_approx(proj.z) and q.w.is_equal_approx(proj.w), "Projection mismatch")
	print("GDScript: Transform2D/Basis/Transform3D/Projection native export match native")

	# ---- Basis math methods ----
	# An asymmetric, invertible basis (det = 1), compared against native Godot.
	var bm = Basis(Vector3(1, 2, 3), Vector3(0, 1, 4), Vector3(5, 6, 0))
	assert(GodotApi.BasisTransposed(bm).is_equal_approx(bm.transposed()), "BasisTransposed mismatch")
	assert(is_equal_approx(GodotApi.BasisDeterminant(bm), bm.determinant()), "BasisDeterminant mismatch")
	assert(GodotApi.BasisInverse(bm).is_equal_approx(bm.inverse()), "BasisInverse mismatch")
	print("GDScript: Basis Transposed/Determinant/Inverse match native")

	# ---- Builtin method engine bridge ----
	# Routed through the engine's own implementation, so the result is bit-identical to
	# GDScript's native call.
	var c_lin = Color(0.25, 0.5, 0.75, 1.0)
	var c_over = Color(0.8, 0.2, 0.4, 0.5)
	assert(GodotApi.ColorSrgbToLinear(c_lin).is_equal_approx(c_lin.srgb_to_linear()), "srgb_to_linear mismatch")
	assert(GodotApi.ColorLinearToSrgb(c_lin).is_equal_approx(c_lin.linear_to_srgb()), "linear_to_srgb mismatch")
	assert(GodotApi.ColorBlend(c_lin, c_over).is_equal_approx(c_lin.blend(c_over)), "blend mismatch")
	print("GDScript: Color builtin-method engine bridge matches native")

	# bool / float returns through the same bridge.
	var col = Color(0.25, 0.5, 0.75, 1.0)
	assert(GodotApi.ColorIsEqualApprox(col, col) == col.is_equal_approx(col), "Color is_equal_approx (true) mismatch")
	assert(GodotApi.ColorIsEqualApprox(col, Color(0, 0, 0, 1)) == col.is_equal_approx(Color(0, 0, 0, 1)), "Color is_equal_approx (false) mismatch")
	var va = Vector2(4, 2)
	assert(is_equal_approx(GodotApi.Vec2Aspect(va), va.aspect()), "aspect mismatch")
	assert(GodotApi.Vec2IsNormalized(va) == va.is_normalized(), "is_normalized (false) mismatch")
	assert(GodotApi.Vec2IsNormalized(Vector2(1, 0)) == Vector2(1, 0).is_normalized(), "is_normalized (true) mismatch")
	assert(GodotApi.Vec3IsEqualApprox(Vector3(1, 2, 3), Vector3(1, 2, 3)) == Vector3(1, 2, 3).is_equal_approx(Vector3(1, 2, 3)), "Vec3 is_equal_approx mismatch")
	print("GDScript: builtin-method bridge slice 2 (bool/float returns) matches native")

	# Vector4 method surface through the bridge: value-type, float and bool returns over
	# 0/1/2-arg and float-arg shapes.
	var w4a = Vector4(1.5, -2.25, 3.0, -0.5)
	var w4b = Vector4(2.0, 0.5, -1.0, 4.0)
	assert(GodotApi.Vec4Abs(w4a).is_equal_approx(w4a.abs()), "Vec4 abs mismatch")
	assert(GodotApi.Vec4Normalized(w4a).is_equal_approx(w4a.normalized()), "Vec4 normalized mismatch")
	assert(GodotApi.Vec4Inverse(w4a).is_equal_approx(w4a.inverse()), "Vec4 inverse mismatch")
	assert(GodotApi.Vec4Min(w4a, w4b).is_equal_approx(w4a.min(w4b)), "Vec4 min mismatch")
	assert(GodotApi.Vec4Max(w4a, w4b).is_equal_approx(w4a.max(w4b)), "Vec4 max mismatch")
	assert(GodotApi.Vec4DirectionTo(w4a, w4b).is_equal_approx(w4a.direction_to(w4b)), "Vec4 direction_to mismatch")
	assert(GodotApi.Vec4Snappedf(w4a, 0.25).is_equal_approx(w4a.snappedf(0.25)), "Vec4 snappedf mismatch")
	for w in [0.0, 0.25, 0.5, 1.0]:
		assert(GodotApi.Vec4Lerp(w4a, w4b, w).is_equal_approx(w4a.lerp(w4b, w)), "Vec4 lerp mismatch")
	assert(GodotApi.Vec4Clamp(w4a, Vector4(0, 0, 0, 0), Vector4(2, 2, 2, 2)).is_equal_approx(w4a.clamp(Vector4(0, 0, 0, 0), Vector4(2, 2, 2, 2))), "Vec4 clamp mismatch")
	assert(GodotApi.Vec4Clampf(w4a, -1.0, 1.0).is_equal_approx(w4a.clampf(-1.0, 1.0)), "Vec4 clampf mismatch")
	assert(is_equal_approx(GodotApi.Vec4Length(w4a), w4a.length()), "Vec4 length mismatch")
	assert(is_equal_approx(GodotApi.Vec4Dot(w4a, w4b), w4a.dot(w4b)), "Vec4 dot mismatch")
	assert(is_equal_approx(GodotApi.Vec4DistanceTo(w4a, w4b), w4a.distance_to(w4b)), "Vec4 distance_to mismatch")
	assert(GodotApi.Vec4IsNormalized(w4a) == w4a.is_normalized(), "Vec4 is_normalized (false) mismatch")
	assert(GodotApi.Vec4IsNormalized(w4a.normalized()) == w4a.normalized().is_normalized(), "Vec4 is_normalized (true) mismatch")
	assert(GodotApi.Vec4IsEqualApprox(w4a, w4a) == w4a.is_equal_approx(w4a), "Vec4 is_equal_approx (true) mismatch")
	assert(GodotApi.Vec4IsEqualApprox(w4a, w4b) == w4a.is_equal_approx(w4b), "Vec4 is_equal_approx (false) mismatch")
	assert(GodotApi.Vec4IsFinite(w4a) == w4a.is_finite(), "Vec4 is_finite mismatch")
	print("GDScript: builtin-method bridge slice 3 (Vector4 surface) matches native")

	# Quaternion builtin methods via the bridge. Quaternion is not an export type, so
	# quaternions are built from floats C#-side and results read back per component.
	var q4n = Quaternion(0.1, 0.2, 0.3, 0.9).normalized()
	var q4r = Quaternion(0.1, 0.2, 0.3, 0.9) # deliberately un-normalized
	var q4b = Quaternion(0.5, -0.2, 0.1, 0.8).normalized()
	assert(GodotApi.QuatIsNormalized(q4n.x, q4n.y, q4n.z, q4n.w) == q4n.is_normalized(), "Quat is_normalized (true) mismatch")
	assert(GodotApi.QuatIsNormalized(q4r.x, q4r.y, q4r.z, q4r.w) == q4r.is_normalized(), "Quat is_normalized (false) mismatch")
	assert(GodotApi.QuatIsFinite(q4n.x, q4n.y, q4n.z, q4n.w) == q4n.is_finite(), "Quat is_finite mismatch")
	assert(GodotApi.QuatIsEqualApprox(q4n.x, q4n.y, q4n.z, q4n.w, q4n.x, q4n.y, q4n.z, q4n.w) == q4n.is_equal_approx(q4n), "Quat is_equal_approx (true) mismatch")
	assert(GodotApi.QuatIsEqualApprox(q4n.x, q4n.y, q4n.z, q4n.w, q4b.x, q4b.y, q4b.z, q4b.w) == q4n.is_equal_approx(q4b), "Quat is_equal_approx (false) mismatch")
	assert(is_equal_approx(GodotApi.QuatAngleTo(q4n.x, q4n.y, q4n.z, q4n.w, q4b.x, q4b.y, q4b.z, q4b.w), q4n.angle_to(q4b)), "Quat angle_to mismatch")
	assert(is_equal_approx(GodotApi.QuatGetAngle(q4n.x, q4n.y, q4n.z, q4n.w), q4n.get_angle()), "Quat get_angle mismatch")
	assert(GodotApi.QuatGetAxis(q4n.x, q4n.y, q4n.z, q4n.w).is_equal_approx(q4n.get_axis()), "Quat get_axis mismatch")
	assert(GodotApi.QuatGetEuler(q4n.x, q4n.y, q4n.z, q4n.w, 2).is_equal_approx(q4n.get_euler(2)), "Quat get_euler mismatch")
	var q4_log = q4n.log()
	var q4_logc = [q4_log.x, q4_log.y, q4_log.z, q4_log.w]
	var q4_exp = q4n.exp()
	var q4_expc = [q4_exp.x, q4_exp.y, q4_exp.z, q4_exp.w]
	for c in 4:
		assert(is_equal_approx(GodotApi.QuatLogComp(q4n.x, q4n.y, q4n.z, q4n.w, c), q4_logc[c]), "Quat log comp mismatch")
		assert(is_equal_approx(GodotApi.QuatExpComp(q4n.x, q4n.y, q4n.z, q4n.w, c), q4_expc[c]), "Quat exp comp mismatch")
	# slerpni / spherical_cubic_interpolate use fixed operands mirroring GodotApi's.
	var q4_sa = Quaternion(0.1, 0.2, 0.3, 0.9).normalized()
	var q4_sb = Quaternion(0.5, -0.2, 0.1, 0.8).normalized()
	var q4_pre = Quaternion(-0.2, 0.1, 0.4, 0.85).normalized()
	var q4_post = Quaternion(0.3, 0.3, -0.1, 0.9).normalized()
	for w in [0.0, 0.25, 0.5, 0.75, 1.0]:
		var n_sl = q4_sa.slerpni(q4_sb, w)
		var n_slc = [n_sl.x, n_sl.y, n_sl.z, n_sl.w]
		var n_sc = q4_sa.spherical_cubic_interpolate(q4_sb, q4_pre, q4_post, w)
		var n_scc = [n_sc.x, n_sc.y, n_sc.z, n_sc.w]
		for c in 4:
			assert(is_equal_approx(GodotApi.QuatSlerpniComp(w, c), n_slc[c]), "Quat slerpni comp mismatch")
			assert(is_equal_approx(GodotApi.QuatSphCubicComp(w, c), n_scc[c]), "Quat spherical_cubic comp mismatch")
	print("GDScript: builtin-method bridge slice 4 (Quaternion surface) matches native")

	# Transform2D + Projection builtin methods via the bridge (large identity-layout value
	# types). Both sides invoke the engine's own impl, so a wrong large-value-type layout
	# or a spurious transpose makes the bridge differ and fails here.
	var s5t2 = Transform2D(Vector2(0.8, 0.6), Vector2(-0.6, 0.8), Vector2(3, 4)).scaled(Vector2(2, 1.5))
	var s5t2b = Transform2D(Vector2(1, 0), Vector2(0, 1), Vector2(10, -5))
	assert(GodotApi.T2DAffineInverse(s5t2).is_equal_approx(s5t2.affine_inverse()), "T2D affine_inverse mismatch")
	assert(GodotApi.T2DOrthonormalized(s5t2).is_equal_approx(s5t2.orthonormalized()), "T2D orthonormalized mismatch")
	assert(GodotApi.T2DRotated(s5t2, 0.5).is_equal_approx(s5t2.rotated(0.5)), "T2D rotated mismatch")
	assert(GodotApi.T2DScaled(s5t2, Vector2(2, 3)).is_equal_approx(s5t2.scaled(Vector2(2, 3))), "T2D scaled mismatch")
	for w in [0.0, 0.5, 1.0]:
		assert(GodotApi.T2DInterpolateWith(s5t2, s5t2b, w).is_equal_approx(s5t2.interpolate_with(s5t2b, w)), "T2D interpolate_with mismatch")
	assert(is_equal_approx(GodotApi.T2DDeterminant(s5t2), s5t2.determinant()), "T2D determinant mismatch")
	assert(is_equal_approx(GodotApi.T2DGetRotation(s5t2), s5t2.get_rotation()), "T2D get_rotation mismatch")
	assert(GodotApi.T2DGetOrigin(s5t2).is_equal_approx(s5t2.get_origin()), "T2D get_origin mismatch")
	assert(GodotApi.T2DBasisXform(s5t2, Vector2(2, 3)).is_equal_approx(s5t2.basis_xform(Vector2(2, 3))), "T2D basis_xform mismatch")
	assert(GodotApi.T2DIsConformal(s5t2) == s5t2.is_conformal(), "T2D is_conformal mismatch")
	assert(GodotApi.T2DIsEqualApprox(s5t2, s5t2) == s5t2.is_equal_approx(s5t2), "T2D is_equal_approx (true) mismatch")
	assert(GodotApi.T2DIsEqualApprox(s5t2, s5t2b) == s5t2.is_equal_approx(s5t2b), "T2D is_equal_approx (false) mismatch")
	assert(GodotApi.T2DIsFinite(s5t2) == s5t2.is_finite(), "T2D is_finite mismatch")
	var s5pr = Projection.create_perspective(75.0, 1.5, 0.1, 100.0)
	var s5pr_inv = GodotApi.ProjInverse(s5pr)
	var s5n_inv = s5pr.inverse()
	assert(s5pr_inv.x.is_equal_approx(s5n_inv.x) and s5pr_inv.y.is_equal_approx(s5n_inv.y) and s5pr_inv.z.is_equal_approx(s5n_inv.z) and s5pr_inv.w.is_equal_approx(s5n_inv.w), "Proj inverse mismatch")
	var s5pr_zn = GodotApi.ProjPerspectiveZnearAdjusted(s5pr, 0.5)
	var s5n_zn = s5pr.perspective_znear_adjusted(0.5)
	assert(s5pr_zn.x.is_equal_approx(s5n_zn.x) and s5pr_zn.z.is_equal_approx(s5n_zn.z) and s5pr_zn.w.is_equal_approx(s5n_zn.w), "Proj perspective_znear_adjusted mismatch")
	var s5pr_fy = GodotApi.ProjFlippedY(s5pr)
	var s5n_fy = s5pr.flipped_y()
	assert(s5pr_fy.x.is_equal_approx(s5n_fy.x) and s5pr_fy.y.is_equal_approx(s5n_fy.y) and s5pr_fy.z.is_equal_approx(s5n_fy.z) and s5pr_fy.w.is_equal_approx(s5n_fy.w), "Proj flipped_y mismatch")
	assert(is_equal_approx(GodotApi.ProjDeterminant(s5pr), s5pr.determinant()), "Proj determinant mismatch")
	assert(is_equal_approx(GodotApi.ProjGetAspect(s5pr), s5pr.get_aspect()), "Proj get_aspect mismatch")
	assert(is_equal_approx(GodotApi.ProjGetZFar(s5pr), s5pr.get_z_far()), "Proj get_z_far mismatch")
	assert(GodotApi.ProjIsOrthogonal(s5pr) == s5pr.is_orthogonal(), "Proj is_orthogonal mismatch")
	assert(GodotApi.ProjGetViewportHalfExtents(s5pr).is_equal_approx(s5pr.get_viewport_half_extents()), "Proj viewport_half_extents mismatch")
	print("GDScript: builtin-method bridge slice 5 (Transform2D/Projection surface) matches native")

	# Basis + Transform3D builtin methods via the bridge — the TRANSPOSE matrix types.
	# Both sides invoke the engine's impl, so a wrong column/row transpose on the
	# receiver, argument or return diverges here.
	var s6b = Basis.from_euler(Vector3(0.3, 0.5, -0.2))
	var s6b2 = Basis.from_euler(Vector3(-0.1, 0.2, 0.4))
	var s6axis = Vector3(0.3, -0.6, 0.74).normalized()
	assert(GodotApi.BasisOrthonormalized(s6b).is_equal_approx(s6b.orthonormalized()), "Basis orthonormalized mismatch")
	assert(GodotApi.BasisRotated(s6b, s6axis, 0.4).is_equal_approx(s6b.rotated(s6axis, 0.4)), "Basis rotated mismatch")
	assert(GodotApi.BasisScaled(s6b, Vector3(2, 3, 0.5)).is_equal_approx(s6b.scaled(Vector3(2, 3, 0.5))), "Basis scaled mismatch")
	assert(GodotApi.BasisGetScale(s6b).is_equal_approx(s6b.get_scale()), "Basis get_scale mismatch")
	assert(GodotApi.BasisGetEuler(s6b, 2).is_equal_approx(s6b.get_euler(2)), "Basis get_euler mismatch")
	var s6q = s6b.get_rotation_quaternion()
	var s6qc = [s6q.x, s6q.y, s6q.z, s6q.w]
	for c in 4:
		assert(is_equal_approx(GodotApi.BasisGetRotQuatComp(s6b, c), s6qc[c]), "Basis get_rotation_quaternion mismatch")
	for w in [0.0, 0.3, 0.7, 1.0]:
		assert(GodotApi.BasisSlerp(s6b, s6b2, w).is_equal_approx(s6b.slerp(s6b2, w)), "Basis slerp mismatch")
	assert(is_equal_approx(GodotApi.BasisTdotx(s6b, Vector3(1, 2, 3)), s6b.tdotx(Vector3(1, 2, 3))), "Basis tdotx mismatch")
	assert(GodotApi.BasisIsConformal(s6b) == s6b.is_conformal(), "Basis is_conformal mismatch")
	assert(GodotApi.BasisIsEqualApprox(s6b, s6b) == s6b.is_equal_approx(s6b), "Basis is_equal_approx (true) mismatch")
	assert(GodotApi.BasisIsEqualApprox(s6b, s6b2) == s6b.is_equal_approx(s6b2), "Basis is_equal_approx (false) mismatch")
	assert(GodotApi.BasisIsFinite(s6b) == s6b.is_finite(), "Basis is_finite mismatch")
	var s6t = Transform3D(s6b, Vector3(5, -3, 2))
	var s6t2 = Transform3D(s6b2, Vector3(1, 1, 1))
	assert(GodotApi.T3DInverse(s6t).is_equal_approx(s6t.inverse()), "T3D inverse mismatch")
	assert(GodotApi.T3DAffineInverse(s6t).is_equal_approx(s6t.affine_inverse()), "T3D affine_inverse mismatch")
	assert(GodotApi.T3DOrthonormalized(s6t).is_equal_approx(s6t.orthonormalized()), "T3D orthonormalized mismatch")
	assert(GodotApi.T3DRotated(s6t, s6axis, 0.4).is_equal_approx(s6t.rotated(s6axis, 0.4)), "T3D rotated mismatch")
	assert(GodotApi.T3DScaled(s6t, Vector3(2, 3, 0.5)).is_equal_approx(s6t.scaled(Vector3(2, 3, 0.5))), "T3D scaled mismatch")
	assert(GodotApi.T3DTranslated(s6t, Vector3(1, 2, 3)).is_equal_approx(s6t.translated(Vector3(1, 2, 3))), "T3D translated mismatch")
	for w in [0.0, 0.5, 1.0]:
		assert(GodotApi.T3DInterpolateWith(s6t, s6t2, w).is_equal_approx(s6t.interpolate_with(s6t2, w)), "T3D interpolate_with mismatch")
	assert(GodotApi.T3DIsEqualApprox(s6t, s6t) == s6t.is_equal_approx(s6t), "T3D is_equal_approx (true) mismatch")
	assert(GodotApi.T3DIsEqualApprox(s6t, s6t2) == s6t.is_equal_approx(s6t2), "T3D is_equal_approx (false) mismatch")
	assert(GodotApi.T3DIsFinite(s6t) == s6t.is_finite(), "T3D is_finite mismatch")
	print("GDScript: builtin-method bridge slice 6 (Basis/Transform3D transpose surface) matches native")

	# string return (Color.to_html), both branches of the with_alpha bool argument.
	for s7col in [Color(1, 0, 0, 0.5), Color(0.2, 0.4, 0.6, 1.0), Color(0, 0.8, 0.3, 0.25)]:
		assert(GodotApi.ColorToHtml(s7col, true) == s7col.to_html(true), "Color to_html(true) mismatch")
		assert(GodotApi.ColorToHtml(s7col, false) == s7col.to_html(false), "Color to_html(false) mismatch")
	print("GDScript: builtin-method bridge slice 7 (Color.to_html string return) matches native")

	# Variant return (NIL or Vector3) from Plane/AABB intersection queries.
	var s8plane = Plane(Vector3(0, 0, 1), 0)
	# Ray hit: from above the XY plane, pointing down — hits at z=0.
	var s8rf = Vector3(1, 2, 5)
	var s8rd = Vector3(0, 0, -1)
	var s8native_hit = s8plane.intersects_ray(s8rf, s8rd)
	assert(GodotApi.PlaneRayHit(s8rf, s8rd) == (s8native_hit != null), "Plane intersects_ray hit-flag mismatch")
	assert(is_equal_approx(GodotApi.PlaneRayX(s8rf, s8rd), s8native_hit.x), "Plane intersects_ray X mismatch")
	assert(is_equal_approx(GodotApi.PlaneRayY(s8rf, s8rd), s8native_hit.y), "Plane intersects_ray Y mismatch")
	assert(is_equal_approx(GodotApi.PlaneRayZ(s8rf, s8rd), s8native_hit.z), "Plane intersects_ray Z mismatch")
	# Ray miss: direction parallel to the plane → NIL.
	var s8md = Vector3(0, 1, 0)
	assert(GodotApi.PlaneRayHit(s8rf, s8md) == (s8plane.intersects_ray(s8rf, s8md) != null), "Plane intersects_ray miss-flag mismatch")

	var s8aabb = AABB(Vector3(0, 0, 0), Vector3(2, 2, 2))
	# Segment hit: passes through the box from above → enters at the +Z face.
	var s8sf = Vector3(1, 1, 5)
	var s8st = Vector3(1, 1, -5)
	var s8native_seg = s8aabb.intersects_segment(s8sf, s8st)
	assert(GodotApi.AabbSegHit(s8sf, s8st) == (s8native_seg != null), "AABB intersects_segment hit-flag mismatch")
	assert(is_equal_approx(GodotApi.AabbSegX(s8sf, s8st), s8native_seg.x), "AABB intersects_segment X mismatch")
	assert(is_equal_approx(GodotApi.AabbSegY(s8sf, s8st), s8native_seg.y), "AABB intersects_segment Y mismatch")
	assert(is_equal_approx(GodotApi.AabbSegZ(s8sf, s8st), s8native_seg.z), "AABB intersects_segment Z mismatch")
	# Segment miss: wholly outside the box → NIL.
	var s8mf = Vector3(5, 5, 5)
	var s8mt = Vector3(6, 6, 6)
	assert(GodotApi.AabbSegHit(s8mf, s8mt) == (s8aabb.intersects_segment(s8mf, s8mt) != null), "AABB intersects_segment miss-flag mismatch")
	print("GDScript: builtin-method bridge slice 8 (Variant nil/Vector3 returns) matches native")

	# ---- Packed scalar arrays <-> C# arrays ----
	# GDScript passes a Packed* literal in and reads one back; a wrong size or element
	# copy fails loudly here.
	assert(GodotApi.SumInts(PackedInt32Array([1, 2, 3, 4])) == 10, "SumInts (PackedInt32Array arg)")
	var sq = GodotApi.MakeSquares(5)
	assert(sq is PackedInt32Array and sq.size() == 5 and sq[4] == 16, "MakeSquares (PackedInt32Array return)")
	assert(GodotApi.SumLongs(PackedInt64Array([10, 20, 30])) == 60, "SumLongs (PackedInt64Array arg)")
	assert(is_equal_approx(GodotApi.SumDoubles(PackedFloat64Array([1.5, 2.25])), 3.75), "SumDoubles (PackedFloat64Array arg)")
	assert(is_equal_approx(GodotApi.SumFloats(PackedFloat32Array([1.5, 2.5, 3.0])), 7.0), "SumFloats (PackedFloat32Array arg)")
	assert(GodotApi.SumBytes(PackedByteArray([1, 2, 3, 250])) == 256, "SumBytes (PackedByteArray arg)")
	var mb = GodotApi.MakeBytes(4)
	assert(mb is PackedByteArray and mb.size() == 4 and mb[3] == 6, "MakeBytes (PackedByteArray return)")
	assert(GodotApi.JoinStrings(PackedStringArray(["a", "b", "c"])) == "a-b-c", "JoinStrings (PackedStringArray arg)")
	var words = GodotApi.MakeWords(3)
	assert(words is PackedStringArray and words.size() == 3 and words[1] == "w1", "MakeWords (PackedStringArray return)")
	print("GDScript: packed scalar-array marshalling matches native")

	# Struct-element packed arrays: Vector2/Vector3/Vector4/Color.
	var sv2 = GodotApi.SumVec2(PackedVector2Array([Vector2(1, 2), Vector2(3, 4)]))
	assert(sv2.is_equal_approx(Vector2(4, 6)), "SumVec2 (PackedVector2Array arg)")
	var rv2 = GodotApi.MakeVec2Ramp(3)
	assert(rv2 is PackedVector2Array and rv2.size() == 3 and rv2[2].is_equal_approx(Vector2(2, 4)), "MakeVec2Ramp (PackedVector2Array return)")
	var sv3 = GodotApi.SumVec3(PackedVector3Array([Vector3(1, 2, 3), Vector3(4, 5, 6)]))
	assert(sv3.is_equal_approx(Vector3(5, 7, 9)), "SumVec3 (PackedVector3Array arg)")
	var sc = GodotApi.BlendColors(PackedColorArray([Color(0.1, 0.2, 0.3, 0.4), Color(0.5, 0.1, 0.1, 0.2)]))
	assert(sc.is_equal_approx(Color(0.6, 0.3, 0.4, 0.6)), "BlendColors (PackedColorArray arg)")
	var rv4 = GodotApi.MakeVec4Ramp(2)
	assert(rv4 is PackedVector4Array and rv4.size() == 2 and rv4[1].is_equal_approx(Vector4(1, 1, 1, 1)), "MakeVec4Ramp (PackedVector4Array return)")
	print("GDScript: struct packed-array marshalling matches native")

	# Heterogeneous Array <-> Godot.Variant[]: mixed-element arrays.
	assert(GodotApi.CountVariants([1, "a", 2.0, true]) == 4, "CountVariants (Array arg)")
	assert(GodotApi.SumVariantInts([10, 20, 30]) == 60, "SumVariantInts (Array of ints)")
	var mixed = GodotApi.MakeMixedVariants()
	assert(mixed is Array and mixed.size() == 4, "MakeMixedVariants (Array return size)")
	assert(mixed[0] == 42 and is_equal_approx(mixed[1], 3.5) and mixed[2] == "hello" and mixed[3] == true, "MakeMixedVariants (values)")
	var echoed = GodotApi.EchoVariants([7, "x", 1.5, false])
	assert(echoed.size() == 4 and echoed[0] == 7 and echoed[1] == "x" and is_equal_approx(echoed[2], 1.5) and echoed[3] == false, "EchoVariants (Array round trip)")
	print("GDScript: Array<->Variant[] marshalling matches native")

	# Richer Variant payloads: Vector2/Vector3/Vector4/Color round-trip through a Variant
	# element of an Array. SumVariantVectorComponents reads each as a Vector4, with
	# missing components zero.
	assert(is_equal_approx(GodotApi.SumVariantVectorComponents(
		[Vector2(1, 2), Vector3(3, 4, 5), Vector4(6, 7, 8, 9), Color(0.1, 0.2, 0.3, 0.4)]),
		(1 + 2) + (3 + 4 + 5) + (6 + 7 + 8 + 9) + (0.1 + 0.2 + 0.3 + 0.4)),
		"SumVariantVectorComponents (Vector2/3/4/Color decode)")
	var vv = GodotApi.MakeVectorVariants()
	assert(vv.size() == 4, "MakeVectorVariants (size)")
	assert(vv[0] == Vector2(1, 2), "MakeVectorVariants (Vector2 encode)")
	assert(vv[1] == Vector3(3, 4, 5), "MakeVectorVariants (Vector3 encode)")
	assert(vv[2] == Vector4(6, 7, 8, 9), "MakeVectorVariants (Vector4 encode)")
	assert(vv[3].is_equal_approx(Color(0.1, 0.2, 0.3, 0.4)), "MakeVectorVariants (Color encode)")
	var ev = GodotApi.EchoVectorVariants([Vector2(-1, -2), Color(0.5, 0.6, 0.7, 0.8)])
	assert(ev.size() == 2, "EchoVectorVariants (size)")
	assert(ev[0] == Vector2(-1, -2), "EchoVectorVariants (Vector2 round trip)")
	assert(ev[1].is_equal_approx(Color(0.5, 0.6, 0.7, 0.8)), "EchoVectorVariants (Color round trip)")
	print("GDScript: richer Variant payloads matches native")

	# A bare Godot.Variant as an export arg / return, across nil / int / string / Vector3
	# payloads.
	assert(GodotApi.EchoVariant(42) == 42, "EchoVariant (int round trip)")
	assert(GodotApi.EchoVariant("hi") == "hi", "EchoVariant (string round trip)")
	assert(GodotApi.EchoVariant(Vector3(1, 2, 3)) == Vector3(1, 2, 3), "EchoVariant (Vector3 round trip)")
	assert(GodotApi.EchoVariant(null) == null, "EchoVariant (nil round trip)")
	assert(GodotApi.VariantAsInt(7) == 7, "VariantAsInt (Variant arg decode)")
	assert(GodotApi.MakeIntVariant(9) == 9, "MakeIntVariant (Variant return encode)")
	assert(GodotApi.MakeStringVariant("xyz") == "xyz", "MakeStringVariant (Variant return encode)")
	assert(GodotApi.NegateVariantVector(Vector3(1, -2, 3)) == Vector3(-1, 2, -3), "NegateVariantVector (Variant arg+return)")
	assert(GodotApi.VariantIsNil(null) == true, "VariantIsNil (nil arg)")
	assert(GodotApi.VariantIsNil(5) == false, "VariantIsNil (non-nil arg)")
	print("GDScript: bare Variant export boundary matches native")

	# Godot.Collections.Dictionary <-> engine Dictionary: passed in (decoded via keys() +
	# operator[]) and read back (rebuilt key by key).
	var gd_dict = {"x": 10, "y": 20}
	assert(GodotApi.DictCount(gd_dict) == 2, "DictCount (Dictionary arg)")
	assert(GodotApi.DictGet(gd_dict, "x") == 10, "DictGet (string key lookup)")
	assert(GodotApi.DictGet(gd_dict, "y") == 20, "DictGet (string key lookup)")
	assert(GodotApi.DictContains(gd_dict, "x") == true, "DictContains (present)")
	assert(GodotApi.DictContains(gd_dict, "z") == false, "DictContains (absent)")
	var made = GodotApi.MakeDict()
	assert(made is Dictionary and made.size() == 3, "MakeDict (Dictionary return size)")
	assert(made["a"] == 1 and made["b"] == 2 and is_equal_approx(made["pi"], 3.5), "MakeDict (values)")
	var echoed_d = GodotApi.EchoDict({"k": "v", "n": 7})
	assert(echoed_d.size() == 2 and echoed_d["k"] == "v" and echoed_d["n"] == 7, "EchoDict (round trip)")
	print("GDScript: Dictionary marshalling matches native")

	# Object payload in a Variant: EchoVariant round-trips identity, EchoVariants carries
	# it inside an Array element.
	var obj_node = Node.new()
	assert(GodotApi.EchoVariant(obj_node) == obj_node, "EchoVariant (Object round trip)")
	var arr_with_node = GodotApi.EchoVariants([obj_node])
	assert(arr_with_node[0] == obj_node, "EchoVariants (Object element round trip)")
	obj_node.free()
	print("GDScript: Object Variant payload matches native")

	# Wrap an engine Object carried in a Variant back into a typed shim: the cast plants
	# the handle in a fresh Node so get_name drives through it; the reverse hands the
	# same node back.
	var named_node = Node.new()
	named_node.name = "G16Node"
	assert(GodotApi.VariantNodeName(named_node) == "G16Node", "VariantNodeName ((Node)variant cast + GetName)")
	assert(GodotApi.VariantNodeRoundTrip(named_node) == named_node, "VariantNodeRoundTrip (Variant->Node->Variant identity)")
	named_node.free()
	print("GDScript: Object->typed-shim wrap matches native")

	# A RefCounted decoded from a Variant container must survive the container: the
	# temporary Array is destroyed right after the element is decoded, and dropping the
	# local releases the last engine-side reference — so the C# static's decode-time
	# reference guard is the only thing keeping the object alive.
	var held_probe := RefCounted.new()
	var held_probe_id = held_probe.get_instance_id()
	GodotApi.HoldFirstVariant([held_probe])
	held_probe = null
	assert(is_instance_id_valid(held_probe_id),
		"Variant-held RefCounted was destroyed while the managed side still held it")
	var held_back = GodotApi.TakeHeldVariant()
	assert(held_back is RefCounted and held_back.get_instance_id() == held_probe_id,
		"held Variant did not round-trip the same RefCounted instance")
	held_back = null
	print("GDScript: Variant-held RefCounted lifetime matches expected")

	# RefCounted lifecycle: a bare `new Godot.Resource()` constructs the engine object and
	# calls init_ref(), so a freshly constructed instance reports a reference count of 1.
	assert(GodotApi.NewBareResourceRefCount() == 1, "NewBareResourceRefCount (bare newobj + init_ref)")

	# A registered user RefCounted subclass constructed via C# `new` takes the
	# always-pinned ClassDB-style path.
	assert(GodotApi.NewMyResourceClassName() == "MyResource", "NewMyResourceClassName (registered newobj)")
	assert(GodotApi.NewMyResourceDoubledTag() == 14, "NewMyResourceDoubledTag (registered newobj ctor ran)")

	# Engine-method RefCounted return: the ptrcall return already carries one transferred
	# reference, so the duplicate reports 1 rather than 2 — it must not be reference()d
	# again (see GodotCallIntrinsics.cs).
	assert(GodotApi.DuplicatedResourceRefCount() == 1, "DuplicatedResourceRefCount (RefCounted return-value wrap)")

	print("GDScript: Godot.RefCounted lifecycle matches expected")

	# GDScript-driven construction via ClassDB .new(): the always-pinned
	# NodeCreateInstance path, not the C# newobj intercept.
	var mr = MyResource.new()
	assert(mr.get_class() == "MyResource", "MyResource.new() class name (ClassDB-driven construction)")
	assert(mr.get_reference_count() == 1, "MyResource.new() reference count (ClassDB init_ref)")
	assert(mr.DoubledTag() == 14, "MyResource.new() ctor ran (ClassDB-driven)")

	print("GDScript: Godot.RefCounted ClassDB construction matches expected")

	GodotApi.HelloFromCSharp()

	# Test MyNode position from GDScript
	var node = MyNode.new()
	var gd_signal_received = false
	node.connect("MyCustomSignal", func(enable):
		print("GDScript lambda received MyCustomSignal: ", enable)
		gd_signal_received = true
	)

	# Collect the ResourcePayload signal argument (checked in _end_check once the first
	# frames have run _Ready and delivered the emission).
	node.connect("ResourcePayload", func(payload):
		_resource_payloads.append(payload)
	)
	node.position = Vector2(10.5, 20.5)
	print("GDScript MyNode position: ", node.position)
	print("GDScript MyNode get_position(): ", node.get_position())

	# Add to tree to trigger _Ready() in C# which emits the signal
	root.add_child(node)

	# Verify signal was received
	print("GDScript MyNode signal received: ", gd_signal_received)

	print("GDScript MyNode MultiplyByTwo(21) = ", node.MultiplyByTwo(21))

	# A leading-underscore user method that is not an engine virtual must register as a
	# normal ClassDB method bind, callable from GDScript.
	print("GDScript MyNode _OnScoreHandler(5) = ", node._OnScoreHandler(5))

	print("GDScript MyNode initial MyExportedProperty = ", node.MyExportedProperty)
	node.MyExportedProperty = 999
	print("GDScript MyNode updated MyExportedProperty = ", node.MyExportedProperty)

	# Engine worker thread -> managed call: WorkerThreadPool runs the callable on a thread
	# the extension never created, so the entry bridge must register it with the GC
	# before any managed allocation. Without that guard this aborts in the collector or
	# returns a clobbered string.
	var worker_results: Array = []
	var worker_task = WorkerThreadPool.add_task(func():
		worker_results.append(GodotApi.DescribeFromEngineThread("worker")))
	WorkerThreadPool.wait_for_task_completion(worker_task)
	print("GDScript: engine worker thread managed call = ", worker_results[0])

	# Registered RefCounted handed back as a Variant return and held only by this local:
	# the C# side dropped its only reference when MakeHandoffResource returned. A forced
	# collect + drain must RESURRECT the shim, not detach it, so the notification below
	# still reaches the C# override instead of the engine's override-less default.
	var handoff = GodotApi.MakeHandoffResource()
	_handoff_id = handoff.get_instance_id()
	GodotApi.ForceGcAndDrain()
	if not is_instance_id_valid(_handoff_id):
		printerr("FAIL: RefCounted engine hand-off did not survive a GC while still held")
	else:
		handoff.notification(9001) # MyHandoffResource.ProbeNotification
		if handoff.WasNotified():
			print("GDScript: RefCounted engine hand-off keeps C# behavior alive")
		else:
			printerr("FAIL: RefCounted engine hand-off survived but lost C# dispatch")
	# Drop this last holder: the anchor must release once the engine's own count falls
	# back to the shim's baseline, so a later sweep + drain reclaims it for real
	# (checked in _end_check).
	handoff = null

	# Registered RefCounted built by C# `new` and dropped must be reclaimed, not leaked.
	# Deliberately the LAST section, with no explicit collect and nothing after it that
	# could drain: reclamation is the runtime's own job, via its library-deinit collect
	# and finalizer drain, so the process must still exit with no ObjectDB leak warning.
	GodotApi.MakeAndDropRegisteredResources(8)
	print("GDScript: Godot.RefCounted C#-new reclamation drained")

	# A registered RefCounted whose ctor throws: the engine object is built and bound
	# before the ctor runs, so a throwing ctor must still leave a registered finalizer,
	# or the constructed engine object is never unreferenced. Left for deinit reclamation.
	GodotApi.MakeAndDropThrowingCtor(8)
	print("GDScript: throwing-ctor registered RefCounted reclaimed")

	# Hand off to the frame-deferred tail: the remaining checks need the main loop to have
	# iterated, and it prints ALL OK and quits.
	_managed_node = node
	process_frame.connect(_end_check)
