"""Selected attribute cuts retain physical IL without executing the cut bodies."""
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET
from copied_array_admission import INPUTS, COPIED, digest, emitted, input_hashes, manifest, contract_digest, framed_hash
from selected_layout_admission import selected_admission, selected_descriptor, selected_policy, expected_effects, EFFECTS
from selected_budget_admission import prepare_budget_fixture
from cut_authority_admission import clone_capture_driver, semantics, save, tree_files
from passive_evidence_admission import run

CASES = (
    ('ctor', 'selected-ss-known-instance', ('Graph.SelectedSpecialAttribute::.ctor',), 'ctor'),
    ('setter', 'selected-ss-known-instance', ('Graph.SelectedSpecialAttribute::set_Value',), 'setter'),
    ('both', 'selected-ss-known-instance', ('Graph.SelectedSpecialAttribute::.ctor', 'Graph.SelectedSpecialAttribute::set_Value'), 'both'),
    ('inherited-setter', 'selected-named-inherited-known-upgrade', ('Graph.SelectedBaseAttribute::set_Value',), 'setter'),
)


def command(cli, original, refs, mode, shared, cuts, destination, raw=False):
    args = ['dotnet', 'exec', cli, original / INPUTS[0]]
    for reference in refs:
        args += ['-r', reference]
    descriptor = selected_descriptor(original, mode)
    if descriptor is not None:
        args += ['--link-xml', descriptor]
    args += ['--shared-generics' if shared else '--no-shared-generics', '-o', destination]
    for cut in cuts:
        args += ['--cut', cut]
    if raw:
        args += ['--no-ildiet']
    return args


def primary(cli, original, refs, mode, shared, cuts, area, raw=True, expected=None):
    before = tree_files(original)
    result = run(command(cli, original, refs, mode, shared, cuts, area / 'cpp', raw), area, 'primary')
    assert result.returncode == 0, (mode, cuts, result.stderr.decode(errors='replace'))
    if not shared:
        assert b'dn2cpp: shared-generics:' not in result.stdout
    generated = emitted(area / 'cpp')
    assert any(n.endswith('.cpp') for n in generated) and any(n.endswith('.h') for n in generated)
    source = '\n'.join((area / 'cpp' / name).read_text(encoding='utf-8') for name in generated)

    def defined(symbol):
        hits = re.findall(r'(?m)^[A-Za-z_][A-Za-z0-9_* ]* ' + symbol + r'_m[0-9]+\([^;\n]*\)\s*\{', source)
        assert len(hits) <= 1, symbol
        return bool(hits)

    ctor_cut = any(c.endswith('::.ctor') for c in cuts)
    setter_cut = any(c.endswith('::set_Value') for c in cuts)
    assert defined('SelectedEntry_Main') and defined('SelectedEntry_Read')
    effects = expected_effects(mode)
    effects['Ctor'] &= not ctor_cut
    effects['Setter'] &= not setter_cut
    actual = {}
    for effect in EFFECTS:
        actual[effect] = defined('Selected' + effect + 'Target__ctor')
        assert defined('Selected' + effect + 'Dependency_Touch') == actual[effect]
    assert actual == effects, (mode, cuts, actual, effects)
    inherited = '-inherited' in mode
    setter = 'SelectedBaseAttribute' if inherited else 'SelectedSpecialAttribute'
    assert defined('SelectedSpecialAttribute__ctor') == (not ctor_cut)
    assert defined(setter + '_set_Value') == (not setter_cut)
    assert defined('SelectedSpecialAttribute_set_Second') == inherited
    for symbol in (setter + '_get_Value', 'SelectedSpecialAttribute_get_Second',
                   'SelectedSiblingAttribute__ctor', 'SelectedUnmatchedAttribute__ctor', 'SelectedPayload_Ordinary',
                   'SelectedPayload_Equals', 'SelectedPayload_ToString'):
        assert not defined(symbol), symbol
    assert defined('SelectedConditional_Activate') == effects['Conditional']
    factories = re.findall(r'Dn2CppObject\* attrcreate_\d+\(\)\s*\{(.*?)\n\}', source, re.S)
    assert not any('SelectedSpecialAttribute__ctor' in body for body in factories), 'cut attribute acquired an instance factory'
    observation = {'effects': actual, 'selected_factory_count': 0,
                   'provider_typeinfo_emitted': bool(re.search(r'(?m)^const Dn2CppTypeInfo ti_Graph_SelectedProvider =', source))}
    if expected is not None:
        assert observation == expected, 'cut changed original-primary admission'
    if raw:
        assert not (area / 'cpp/ildiet').exists()
    assert tree_files(original) == before
    save(area / 'observation.json', {'admission': observation, 'generated': generated, 'runtime_instance_construction_claimed': False})
    return observation


def capture(cli, original, refs, mode, shared, cuts, area):
    result = run(command(cli, original, refs, mode, shared, cuts, area / 'cpp'), area, 'driver',
                 {'CUT_CAPTURE_DIR': str(area / 'actual'),
                  'CUT_CAPTURE_PYTHON': str(Path(sys.executable).resolve(strict=True))})
    assert result.returncode != 0 and b'ILDiet failed with exit code 86' in result.stdout + result.stderr
    record = json.loads((area / 'actual/capture.json').read_bytes())
    assert record['companion_started'] is False and record['exit_code'] == 86
    path = area / 'actual/request.xml'
    root = ET.parse(path).getroot()
    expected_refs = [*refs, cli.parent / 'Dn2Cpp.Runtime.dll']
    assert root.tag == 'ildiet' and root.attrib == {'version': '2', 'input': str(original / INPUTS[0]),
           'output': str(area / 'cpp/ildiet'), 'copyAll': 'false'}
    descriptor = selected_descriptor(original, mode)
    assert [n.tag for n in root] == ['reference'] * len(expected_refs) + ([] if descriptor is None else ['linkXml']) + ['cut'] * len(cuts)
    assert [n.attrib for n in root.findall('reference')] == [{'path': str(p)} for p in expected_refs]
    assert [n.attrib for n in root.findall('linkXml')] == ([] if descriptor is None else [{'path': str(descriptor)}])
    assert [n.attrib for n in root.findall('cut')] == [{'method': cut} for cut in cuts]
    assert all(len(n) == 0 for n in root)
    assert record['inputs'] == [{'path': str(p), 'sha256': digest(p)} for p in [original / INPUTS[0], *expected_refs]]
    return path


def publication(probe, original, output, mode, variant, request, expected):
    root, published = manifest(output)
    assert root.get('fallback') == 'none' and root.get('cutsValidated') == 'true'
    assert root.get('contract') == contract_digest(request)
    assert root.get('constructorRegistriesRewritten') == 'false'
    counters = dict(root.find('counters').attrib)
    assert counters['decode.max-per-method'] == '1'
    source_paths = [Path(request.get('input')), *(Path(n.get('path')) for n in request.findall('reference'))]
    assert [Path(n.get('path')).name for n in root.findall('reference')] == [p.name for p in source_paths[1:]]
    assert {p.name for p in published.glob('*.dll')} == {p.name for p in source_paths}
    values = []
    for item, source in zip(root.findall('assembly'), source_paths, strict=True):
        assert item.get('name') + '.dll' == source.name and item.get('registryRewritten') == 'false'
        copied = source.name in (*COPIED, 'System.Private.CoreLib.dll', 'Dn2Cpp.Runtime.dll')
        assert item.get('mode') == ('copied' if copied else 'rewritten')
        if copied:
            assert (published / source.name).read_bytes() == source.read_bytes()
        values.extend((source.name, (published / source.name).read_bytes()))
    values.extend(('preservation.xml', (published / 'preservation.xml').read_bytes()))
    assert root.get('generation') == 'g-' + framed_hash(values)
    selected_policy(published / 'preservation.xml', mode, published=True)
    area = output.parent
    checked = run(['dotnet', 'exec', probe, '--check-selected-cut-fixtures', original, published, mode, variant], area, 'physical-check')
    assert checked.returncode == 0, checked.stderr.decode(errors='replace')
    lines = [line for line in checked.stdout.decode().splitlines() if line.startswith('selected-cut-observed=')]
    assert len(lines) == 1
    observed = json.loads(lines[0].split('=', 1)[1])
    # A known owner token conservatively retains ordinary attribute IL. These
    # physical effects confer no primary executable authority (checked by primary).
    assert not expected['effects']['Getter'] and not expected['effects']['Sibling']
    physical_effects = dict(expected['effects'], Getter=True, Sibling='-inherited' not in mode)
    assert observed['physicalEffects'] == physical_effects
    assert len(observed['physical']) == len(request.findall('cut'))
    closure = run(['dotnet', 'exec', probe, '--check-token-closure', *sorted(published.glob('*.dll'))], area, 'closure')
    assert closure.returncode == 0 and b'token-closure=valid operands=' in closure.stdout
    for source in source_paths:
        before = run(['dotnet', 'exec', probe, '--relations', source], area, 'before-' + source.name)
        after = run(['dotnet', 'exec', probe, '--relations', published / source.name], area, 'after-' + source.name)
        assert before.returncode == after.returncode == 0 and set(after.stdout.splitlines()) <= set(before.stdout.splitlines())
    return {'files': tree_files(published), 'generation': root.get('generation'), 'counters': counters, 'observed': observed}


def publish(companion, probe, original, captured, area, mode, variant, expected):
    area.mkdir(parents=True, exist_ok=False)
    tree = ET.parse(captured)
    before = semantics(tree.getroot())
    tree.getroot().set('output', str(area / 'publication'))
    assert semantics(tree.getroot()) == before
    request = area / 'request.xml'
    tree.write(request, encoding='utf-8', xml_declaration=True)
    save(area / 'request-provenance.json', {'captured': str(captured), 'sha256': digest(captured), 'only_semantic_change': 'output'})
    result = run(['dotnet', 'exec', companion, '--request', request], area, 'companion')
    assert result.returncode == 0, result.stderr.decode(errors='replace')
    return publication(probe, original, area / 'publication', mode, variant, tree.getroot(), expected)


def validate_selected_cuts(companion, probe, cli, corelib, directory):
    directory = Path(directory).resolve()
    directory.mkdir(parents=True, exist_ok=False)
    inputs, controls, prepared = {}, [], []
    for mode in dict.fromkeys(case[1] for case in CASES):
        area = directory / mode
        original, refs, hashes = prepare_budget_fixture(probe, corelib, area, mode)
        baseline = selected_admission(cli, original, corelib, area / 'uncut-original', mode, ('original',), refs, None, None)
        controls.append((original, refs, mode, area, {str(r['shared']): r['selected_admission'] for r in baseline}))
        inputs[mode] = (original, refs, hashes)
    for label, mode, cuts, variant in CASES:
        original, refs, hashes = inputs[mode]
        for shared in (True, False):
            area = directory / label / ('sharing-on' if shared else 'sharing-off')
            expected = primary(cli, original, refs, mode, shared, cuts, area / 'raw')
            prepared.append((mode, variant, original, refs, shared, cuts, area, expected))
    capture_cli = clone_capture_driver(cli, probe, directory / 'capture-cli')
    capture_pin = tree_files(capture_cli.parent)
    assert (capture_cli.parent / 'Dn2Cpp.Runtime.dll').read_bytes() == (cli.parent / 'Dn2Cpp.Runtime.dll').read_bytes()
    captures = []
    for mode, variant, original, refs, shared, cuts, area, expected in prepared:
        captured = capture(capture_cli, original, refs, mode, shared, cuts, area / 'capture')
        captures.append((mode, variant, original, refs, shared, cuts, area, expected, captured))
    for original, refs, mode, area, baseline in controls:
        selected_admission(cli, original, corelib, area / 'uncut-integrated', mode, ('integrated',), refs, None, baseline)
    receipts = []
    for mode, variant, original, refs, shared, cuts, area, expected, captured in captures:
        standalone = [publish(companion, probe, original, captured, area / ('standalone-' + str(i)), mode, variant, expected) for i in (1, 2)]
        assert standalone[0] == standalone[1]
        integrated = area / 'integrated'
        primary(cli, original, refs, mode, shared, cuts, integrated, raw=False, expected=expected)
        published = publication(probe, original, integrated / 'cpp/ildiet', mode, variant, ET.parse(captured).getroot(), expected)
        assert published == standalone[0], 'standalone and integrated selected-cut publication changed'
        receipts.append({'case': area.relative_to(directory).as_posix(), 'admission': expected, 'publication': published})
        print('graph-selected-cut=' + mode + ':' + variant + ':sharing-' + str(shared) + ':original-first,actual-xml,physical-il,nonexecution,standalone-integrated', flush=True)
    for original, refs, hashes in inputs.values():
        assert input_hashes(original, corelib) == hashes
    assert tree_files(capture_cli.parent) == capture_pin
    save(directory / 'receipt.json', receipts)
    print('graph-validation=selected-attribute-constructor-and-own-inherited-setter-cuts,unchanged-inputs,physical-token-closure,bounded-factory,uncut-positive-controls', flush=True)
    return receipts
