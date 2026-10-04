# Godot phase membership shared by the suite and pre-merge scope selection.
CHAIN_A=(   # samples/godot/godot-project (bin/ + .godot) + iOS/Android runtime caches
    "gates/build-and-run-godot-sample.sh"
    "gates/build-and-run-godot-ios-export.sh"
    "gates/build-and-run-godot-ios-sim.sh"
    "gates/build-and-run-android-gdext.sh"
)
CHAIN_B=(   # samples/godot/godot-project-hotupdate
    "gates/build-and-run-hotupdate-godot.sh"
)
CHAIN_C=(   # samples/godot/godot-project-sdk
    "gates/build-and-run-sdk-sample.sh"
)
CHAIN_D=(   # samples/godot-dotnet/DotnetSample (nuget.config, .godot/mono)
    "gates/build-and-run-godot-dotnet-handshake.sh"
    "gates/build-and-run-godot-dotnet-sample.sh"
    # The same scripted-scene E2E as the sample gate, but the transpile passes
    # --trim-reflection: the real-engine oracle for the flag (the console side is
    # build-and-run-trim-reflection.sh). Shares the DotnetSample project write set, so it
    # stays in this chain, serial with the untrimmed sample gate above.
    "gates/build-and-run-godot-dotnet-trim.sh"
    # Needs no engine — it plays the engine's role itself, under node. It is in a
    # chain because the chains are a write-set partition, not a needs-Godot one, and
    # this gate builds the same DotnetSample project: the Phase 4/5 boundary is what
    # keeps it off the parallel-phase lib gate's toes, exactly as the note above says.
    "gates/build-and-run-godot-dotnet-wasm.sh"
)
CHAIN_E=(   # fork editor exports. The shared $FORK_GODOTSHARP/Dn2Cpp +
    # artifacts/toolchain staging no longer needs this chain for safety:
    # stage_editor_toolchain (gates/_common.sh) stages it atomically under
    # a lock held beside that tree, so it is safe even across suites. The chain
    # is kept because concurrent fork-editor launches contend for resources.
    "gates/build-and-run-godot-editor-export.sh"
    "gates/build-and-run-godot-editor-export-declang.sh"
    "gates/build-and-run-godot-editor-export-declang-android.sh"
    "gates/build-and-run-godot-editor-export-ios.sh"
    "gates/build-and-run-godot-editor-export-android.sh"
    "gates/build-and-run-godot-editor-export-web.sh"
    # The same export with the host's Emscripten SDK taken away, proving the
    # bundle's own is sufficient and stays read-only.
    "gates/build-and-run-godot-editor-export-web-hermetic.sh"
    # CRI ADX LE Android E2E: stages the external CriWare sample, exports it
    # through the fork editor (same editor-launch + toolchain-staging write set
    # as the gates above), and runs it on an attached adb device when present.
    "gates/build-and-run-cri-android.sh"
    # CRI ADX LE Web E2E: the same staged sample through the mono-module Web
    # lane against the CRI-enabled template, run in a real Chrome when present.
    "gates/build-and-run-cri-web.sh"
)
CHAIN_F=(   # samples/godot-dotnet/GDTaskSample + gates/out-gdtask*
    # Alone in its chain because the chains are a write-set partition, and this
    # gate's write set is disjoint from every other: its own project dir, its own
    # out dirs. It shares only read-only things (the pinned editor/template
    # binaries, the prebuilt CMake runtime) and ~/.nuget, whose restores are
    # already concurrency-safe.
    "gates/build-and-run-gdtask.sh"
)
GODOT_GATES=(
    "${CHAIN_A[@]}" "${CHAIN_B[@]}" "${CHAIN_C[@]}" "${CHAIN_D[@]}" "${CHAIN_E[@]}"
    "${CHAIN_F[@]}"
)
