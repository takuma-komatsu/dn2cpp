#!/usr/bin/env bash
# Delegate callable routes and native-only constructor refusals reuse the
# primitive aggregate image. Only descriptor-root isolation needs another image.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_delegate-callable-bodies.sh gates/fixtures/ldftn-local/DelegateCallableFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj gates/expected/delegate-callable-refusals.txt"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/check-delegate-callable-bodies.py samples/dotnet/SelfHostPrimSubset/keep-delegate-callable-bodies.xml"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|delegate-callable:reflection,call,calli,callvirt,ldvirtftn|retained-only:descriptor|refusals:frozen-native|cli:$(_gate_cli_hash)"

delegate_callable_prepare_app() {
    local project="$1" out="$2"
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    mkdir -p "$out/app" "$out/retained-app" || return 1
    cp -R "$(dirname "$_CG_APP")/." "$out/app/" || return 1
    cp "$_CG_APP" "$out/retained-app/$project.dll" || return 1
    _CG_APP="$out/app/$project.dll"
    gate_capture_ok "$out/fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --delegate-callable-bodies "$_CG_APP" "$_CG_CORELIB" || return 1
    gate_output byte-witness "$out/fixture.stdout" \
        'CallDelegateEquals=call:System.Boolean System.Delegate::Equals(System.Object)' \
        'CalliFactory=ldftn+calli:System.MulticastDelegate System.MulticastDelegate::NewMulticastDelegate(System.Object[],System.Int32)' \
        'CalliCtorCollectibleVirtualDispatch=ldftn+calli:System.Void System.MulticastDelegate::CtorCollectibleVirtualDispatch(System.Object,System.IntPtr,System.IntPtr,System.IntPtr)' \
        'CallvirtDelegateEquals=callvirt:System.Boolean System.Delegate::Equals(System.Object)' \
        'VirtCalliDelegateEquals=ldvirtftn+calli:System.Boolean System.Delegate::Equals(System.Object)' \
        'delegate virtual null and argument-order fixtures verified' \
        'VirtualBoundEquals=ldvirtftn+delegate:System.Boolean System.Delegate::Equals(System.Object)' \
        'VirtualBoundEqualsLocal=ldvirtftn+local+delegate:System.Boolean System.Delegate::Equals(System.Object)' \
        --count 1 --policy crlf
}

delegate_callable_asserts() {
    local out="$1" route side output
    for route in reflection call calli callvirt ldvirtftn; do
        gate_capture_ok "$out/delegate-$route-native" run_bounded "$out/SelfHostPrimSubset$EXE_EXT" delegate-callable "$route"
        gate_capture_ok "$out/delegate-$route-oracle" run_bounded dotnet "$_CG_APP" delegate-callable "$route"
        gate_output compare "$out/delegate-$route-native.stdout" "$out/delegate-$route-oracle.stdout"
        gate_output compare "$out/delegate-$route-native.stderr" "$out/delegate-$route-oracle.stderr"
        for side in native oracle; do
            output="$out/delegate-$route-$side.stdout"
            gate_output byte-witness "$output" \
                'Retained delegate value members end' 'Retained multicast accessors end' \
                'Retained multicast construction end' 'Retained multicast operations end' \
                'Named delegate construction public paths end' 'named-rebind-Delegate:True' \
                'named-rebind-MulticastDelegate:True' 'CoreCLR delegate construction boundary end' \
                'Delegate callable bodies end' 'Delegate declared-owner value contracts end' \
                'delegate-virtual-binding-values:True' 'delegate-virtual-binding-method:True' \
                'delegate-virtual-binding-identity:True' 'Delegate virtual binding identity end' \
                'delegate-closed-null-Delegate:True' 'delegate-closed-null-MulticastDelegate:True' \
                'delegate-callvirt-null-order:True' 'delegate-callvirt-value-order:True' \
                'delegate-ldvirtftn-base-null:NullReferenceException' 'delegate-ldvirtftn-final-null:address' \
                'delegate-base-null-False:True' 'delegate-base-null-True:True' \
                'Delegate virtual null boundaries end' --count 1 --policy crlf
            if grep -Fq ':False' "$output"; then
                echo "FAIL: delegate callable contract returned false on $route/$side" >&2
                return 1
            fi
        done
    done
    for side in native oracle; do
        if [ "$side" = native ]; then
            gate_capture_ok "$out/delegate-prefix-$side" run_bounded "$out/SelfHostPrimSubset$EXE_EXT" delegate-callable before-virtual-delegates
        else
            gate_capture_ok "$out/delegate-prefix-$side" run_bounded dotnet "$_CG_APP" delegate-callable before-virtual-delegates
        fi
        gate_output prefix "$out/delegate-reflection-$side.stdout" "$out/delegate-prefix-$side.stdout" \
            --before '== Delegate declared-owner value contracts ==' --policy crlf
    done

    # CoreCLR owns these unsupported constructors and prefix shapes; a CLR
    # execution is not their oracle. Reuse the image against the frozen contract.
    for route in reflection call calli callvirt ldvirtftn; do
        gate_capture_ok "$out/delegate-refusals-$route" run_bounded "$out/SelfHostPrimSubset$EXE_EXT" delegate-refusals "$route"
        gate_output compare "$out/delegate-refusals-$route.stdout" gates/expected/delegate-callable-refusals.txt --policy crlf
    done
}

delegate_callable_retained_only() {
    local out="$1"; shift
    local input="$out/retained-app/SelfHostPrimSubset.dll"
    gate_capture_ok "$out/retained-fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --delegate-retained-only "$input" "$_CG_CORELIB"
    gate_output byte-witness "$out/retained-fixture.stdout" \
        'delegate retained-only roots=CurrentCulture,CurrentUICulture' --count 1 --policy crlf
    invoke_cli "$input" -r "$_CG_CORELIB" "$@" \
        --link-xml samples/dotnet/SelfHostPrimSubset/keep-delegate-callable-bodies.xml -o "$out/retained-only"
    gate_python gates/check-delegate-callable-bodies.py "$out/retained-only"
    compile_console "$out/retained-only" DelegateRetainedBodies
    gate_capture_ok "$out/retained-only/native" run_bounded "$out/retained-only/DelegateRetainedBodies$EXE_EXT"
    : > "$out/retained-only/expected.stdout"
    gate_output compare "$out/retained-only/native.stdout" "$out/retained-only/expected.stdout"
    gate_output compare "$out/retained-only/native.stderr" "$out/retained-only/expected.stdout"
}
