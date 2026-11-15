# UnrealSharp Android Minimal

This project checks the native UnrealSharp Game backend on an Android arm64
Development APK. The Game module spawns `AMinimalManagedActor`, calls its C#
`Answer` UFunction, and logs `result=42` with the process's CLR image state.
The managed module logs `managed-start=OK` when registration runs. The app stays
alive after the probe so the device gate can check process liveness.

Run from the dn2cpp repository root with an Unreal Engine 5.8.3 installation
that includes Android support, JDK 17, Android SDK Platform 36, Build Tools
36.0.0, NDK r27c, and a connected arm64 device:

```sh
dotnet build src/Dn2Cpp.Cli -c Release
UE_ROOT=/path/to/UnrealEngine \
UNREALSHARP_PLUGIN=/path/to/UnrealSharp-dn2cpp \
JAVA_HOME="$(/usr/libexec/java_home -v 17)" \
ANDROID_NDK_ROOT="$HOME/Library/Android/sdk/ndk/27.2.12479018" \
ANDROID_SERIAL=device-serial \
./gates/run-unrealsharp-android-minimal.sh
```

`ANDROID_SERIAL` can be omitted when exactly one authorized device is attached.
The gate copies the project and plugin into `artifacts/unrealsharp-android-minimal`,
generates Android Game bindings, stages the native Game library, cooks and
packages the APK, then checks its native library and UFS manifests. It installs
and launches the APK, requires both managed startup and result markers in
logcat, rejects fatal errors, and requires the same process to remain alive.
Set `UNREALSHARP_ANDROID_RESULT_DIR` to place the working project and logs
elsewhere.
