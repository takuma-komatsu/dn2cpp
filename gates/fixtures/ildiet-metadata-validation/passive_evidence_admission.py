"""Passive evidence preserves original IL without executing reflection-body analysis."""
from pathlib import Path
import json
import os
import re
import subprocess
import xml.etree.ElementTree as ET
from copied_array_admission import digest, emitted, manifest, contract_digest, framed_hash

MODES = ("passive-async-true-carrier", "passive-async-false-prefilter")
INPUT_NAMES = ("GraphApp.dll", "GraphLib.dll", "System.ArrayFacade.dll", "System.ArrayLeaf.dll", "DnZlib.dll", "GraphTaskB.dll", "System.Private.CoreLib.dll", "Dn2Cpp.Runtime.dll")
COPIED = ("System.ArrayFacade.dll", "System.ArrayLeaf.dll", "DnZlib.dll", "System.Private.CoreLib.dll", "Dn2Cpp.Runtime.dll")
ARMS = (("default", ()), ("no-adopt-a", ("GraphLib",)))


def run(command, area, label, env=None):
    area.mkdir(parents=True, exist_ok=True)
    command = list(map(str, command))
    (area / (label + '.command.json')).write_text(json.dumps(command, indent=2) + '\n')
    result = subprocess.run(command, capture_output=True, env={**os.environ, **(env or {})})
    (area / (label + '.stdout.log')).write_bytes(result.stdout)
    (area / (label + '.stderr.log')).write_bytes(result.stderr)
    (area / (label + '.process.json')).write_text(json.dumps({'returncode': result.returncode}, indent=2) + '\n')
    return result


def files(path):
    return {p.name: digest(p) for p in sorted(path.iterdir()) if p.is_file()}


def generated(destination):
    return emitted(destination)


def observed(result):
    rows = [line for line in result.stdout.decode().splitlines() if line.startswith('passive-evidence-observed=')]
    assert result.returncode == 0 and len(rows) == 1, result.stderr.decode(errors='replace')
    return json.loads(rows[0].split('=', 1)[1])


def create(probe, corelib, area, mode):
    assert mode in MODES and not area.exists()
    area.mkdir(parents=True)
    original = area / 'input'
    result = run(['dotnet', 'exec', probe, '--create-copied-array-fixtures', original, corelib, mode], area, 'create')
    assert result.returncode == 0 and result.stdout.splitlines() == [('fixture-created=copied-array mode=' + mode).encode()], result.stderr
    assert set(files(original)) == {*INPUT_NAMES, 'link.xml', 'operand-audit.json'}
    checked = observed(run(['dotnet', 'exec', probe, '--check-copied-array-fixtures', original, mode, 'original'], area, 'original-check'))
    (area / 'original-check.json').write_text(json.dumps(checked, indent=2) + '\n')
    audit = json.loads((original / 'operand-audit.json').read_text())
    assert audit['variant'] == mode.removeprefix('passive-async-') and audit['primarySupported'] is None
    assert audit['rawHitNames'] == (['Decline'] if mode == MODES[0] else ['PrefilterOnly', 'Decline'])
    assert audit['carriersCalled'] is False and audit['attributeOrOverrideCarrier'] is False
    assert audit['carrierIsNongenericNonvirtualPrivate'] is True
    token = audit['witnessToken']
    for index, hit in enumerate(audit['hits']):
        il = bytes.fromhex(hit['il'])
        assert all(il[offset:offset+4] == token.to_bytes(4, 'little') for offset in hit['rawOffsets'])
        if mode == MODES[1] and index == 0:
            assert hit['actualOperandOffsets'] == [] and len(hit['rawOffsets']) == 1
            assert il[hit['rawOffsets'][0]-1] == 0x20
        else:
            assert hit['actualOperandOffsets'] == [audit['requiredOperand']]
    return original


def primary(cli, env, original, area, options, shared, variant, integrated=False, expected=None):
    assert not area.exists()
    before = files(original)
    destination = area / 'cpp'
    command = ['dotnet', 'exec', cli, original / INPUT_NAMES[0]]
    for name in INPUT_NAMES[1:]:
        command += ['-r', original / name]
    for assembly in options:
        command += ['--no-adopt-async', assembly]
    command += ['--link-xml', original / 'link.xml', '--shared-generics' if shared else '--no-shared-generics',
                '-o', destination]
    if not integrated:
        command += ['--no-ildiet']
    result = run(command, area, 'primary', env)
    assert result.returncode == 0, ('Original primary is unsupported; no companion claim', variant, options, shared, area)
    emitted_files = generated(destination)
    source = '\n'.join((destination / n).read_text(encoding='utf-8') for n in emitted_files)

    def definitions(symbol):
        return re.findall(r'(?m)^[A-Za-z_][A-Za-z0-9_* ]* ' + symbol + r'_m[0-9]+\([^;\n]*\)\s*\{', source)

    assert all(definitions('EagerEntry_' + name) for name in ('Main', 'Carry', 'Arm'))
    counts = {'conditional': len(definitions('EagerConditional_Activate')),
              'array': len(definitions('EagerCell_1__ctor')), 'array_b': len(definitions('EagerBCell_1__ctor')),
              'policy': len(definitions('EagerPolicy_1_Activate'))}
    assert {k: bool(v) for k, v in counts.items()} == {
        'conditional': bool(options), 'array': True, 'array_b': not options, 'policy': False}, counts
    for key, dependency in (('conditional', 'EagerConditionalDependency'), ('array', 'EagerArrayDependency'),
                            ('array_b', 'EagerBArrayDependency'), ('policy', 'EagerPolicyDependency')):
        assert bool(counts[key]) == bool(definitions(dependency + '_Touch'))
    for symbol in ('EagerDeadCross_Decline', 'EagerDeadCross_PrefilterOnly', 'EagerBuilder_1_get_Task',
                   'EagerTask_1_OutsideContract', 'EagerDeclinedTask_1_OutsideContract',
                   'EagerLazyFieldCell_1__ctor', 'EagerLazyMethodCell_1__ctor', 'EagerLazyConditional_Activate',
                   'EagerAnchor__ctor', 'EagerAnchor__cctor', 'EagerAnchor_Ordinary'):
        assert not definitions(symbol), ('Dead/passive body was emitted', symbol)
    for name in ('EagerLazyFieldDependency', 'EagerLazyMethodDependency', 'EagerLazyConditionalDependency',
                 'EagerAnchorCtorDependency', 'EagerAnchorCctorDependency', 'EagerAnchorOrdinaryDependency'):
        assert not definitions(name + '_Touch'), name
    assert definitions('EagerDataAttribute__ctor')
    notices = [line for line in result.stderr.decode().splitlines() if line.startswith('dn2cpp: adoption declined:')]
    wanted = [] if options else [
        "dn2cpp: adoption declined: Graph.EagerBuilder::get_Task is outside the mapped contract; transpiling " + n + "'s real IL"
        for n in ('GraphLib', 'GraphTaskB')]
    assert notices == wanted, (notices, wanted)
    if integrated:
        root, published = manifest(destination / 'ildiet')
        assert root.get('fallback') == 'none' and root.find('counters').get('decode.max-per-method') == '1'
        for name in COPIED:
            assert (published / name).read_bytes() == (original / name).read_bytes()
    else:
        assert not (destination / 'ildiet').exists()
    if expected is not None:
        assert counts == expected['admission_counts'] and notices == expected['decline_notices']
    assert files(original) == before
    return {'shared': shared, 'no_adopt': list(options), 'admission_counts': counts,
            'generated_sha256': emitted_files, 'decline_notices': notices, 'dead_carriers_emitted': False,
            'command': list(map(str, command))}



def request(original, area, descriptor=None):
    area.mkdir(parents=True, exist_ok=False)
    root = ET.Element('ildiet', version='2', input=str(original / INPUT_NAMES[0]), output=str(area / 'publication'))
    for name in INPUT_NAMES[1:]:
        ET.SubElement(root, 'reference', path=str(original / name))
    ET.SubElement(root, 'linkXml', path=str(descriptor or original / 'link.xml'))
    path = area / 'request.xml'
    ET.ElementTree(root).write(path, encoding='utf-8', xml_declaration=True)
    return root, path


def failure(companion, original, area, mode, env=None, descriptor=None):
    before = files(original)
    _, path = request(original, area, descriptor)
    result = run(['dotnet', 'exec', companion, '--request', path], area, 'companion', env)
    audit = json.loads((original / 'operand-audit.json').read_text())
    diagnostic = 'cannot decode reflection body GraphApp::' + audit['failingCarrier'] + ': Unsupported IL opcode: Refanytype (0xFE1D)'
    assert result.returncode != 0 and diagnostic in (result.stderr + result.stdout).decode(errors='replace'), (mode, area, result.returncode, diagnostic)
    assert not (area / 'publication/ildiet-result.xml').exists()
    assert files(original) == before
    return {'mode': mode, 'returncode': result.returncode, 'exact_diagnostic': diagnostic,
            'unsupported_control': descriptor is not None, 'supported_original_pass': False}


def publish(companion, probe, original, area, mode, env=None):
    before = files(original)
    tree, path = request(original, area)
    result = run(['dotnet', 'exec', companion, '--request', path], area, 'companion', env)
    assert result.returncode == 0, (mode, result.stderr.decode(errors='replace'))
    root, published = manifest(area / 'publication')
    counters = dict(root.find('counters').attrib)
    assert root.get('fallback') == 'none' and counters['decode.max-per-method'] == '1'
    assert int(counters['methods.evidence-body']) >= 1
    assert root.get('contract') == contract_digest(tree)
    assert [Path(r.get('path')).name for r in root.findall('reference')] == list(INPUT_NAMES[1:])
    assert {p.name for p in published.glob('*.dll')} == set(INPUT_NAMES)
    values = []
    for item in root.findall('assembly'):
        name = item.get('name') + '.dll'
        assert item.get('mode') == ('copied' if name in COPIED else 'rewritten')
        assert item.get('registryRewritten') == 'false'
        values.extend((name, (published / name).read_bytes()))
    values.extend(('preservation.xml', (published / 'preservation.xml').read_bytes()))
    assert root.get('generation') == 'g-' + framed_hash(values)
    for name in COPIED:
        assert (published / name).read_bytes() == (original / name).read_bytes()
    checked = observed(run(['dotnet', 'exec', probe, '--check-copied-array-fixtures', published, mode, 'published'], area, 'retained-check', env))
    earlier = json.loads((original.parent / 'original-check.json').read_text())
    for key in ('physical', 'contracts', 'order'):
        assert checked[key] == earlier[key], (mode, key, 'original physical/predicate evidence changed')
    assert checked['falseCarrierBody'] is False and checked['falseCarrierPresent'] is False and checked['carriersCalled'] is False
    closure = run(['dotnet', 'exec', probe, '--check-token-closure', *sorted(published.glob('*.dll'))], area, 'closure', env)
    assert closure.returncode == 0 and b'token-closure=valid operands=' in closure.stdout
    for name in INPUT_NAMES:
        before_rel = run(['dotnet', 'exec', probe, '--relations', original / name], area, 'before-' + name, env)
        after_rel = run(['dotnet', 'exec', probe, '--relations', published / name], area, 'after-' + name, env)
        assert before_rel.returncode == after_rel.returncode == 0
        assert set(after_rel.stdout.splitlines()) <= set(before_rel.stdout.splitlines()), (mode, name, 'new relation')
    assert files(original) == before
    receipt = {'observation': checked, 'files': files(published), 'generation': root.get('generation'), 'counters': counters}
    (area / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    return receipt


def executable_control(drivers, companions, original, area, mode):
    assert not area.exists()
    area.mkdir(parents=True)
    before = files(original)
    target = 'Decline' if mode == MODES[0] else 'PrefilterOnly'
    descriptor = ET.parse(original / 'link.xml').getroot()
    assembly = ET.SubElement(descriptor, 'assembly', fullname='GraphApp', preserve='nothing')
    owner = ET.SubElement(assembly, 'type', fullname='Graph.EagerDeadCross', preserve='nothing')
    ET.SubElement(owner, 'method', name=target)
    path = area / 'executable.xml'
    ET.ElementTree(descriptor).write(path, encoding='utf-8', xml_declaration=True)
    records = []
    for label, cli, env in drivers:
        for shared in (True, False):
            destination = area / label / ('sharing-on' if shared else 'sharing-off')
            command = ['dotnet', 'exec', cli, original / INPUT_NAMES[0]]
            for name in INPUT_NAMES[1:]:
                command += ['-r', original / name]
            command += ['--link-xml', path, '--no-ildiet', '--shared-generics' if shared else '--no-shared-generics', '-o', destination / 'cpp']
            result = run(command, destination, 'unsupported-original', env)
            diagnostic = 'Unsupported IL opcode: Refanytype (0xFE1D)'
            assert result.returncode != 0 and diagnostic in (result.stderr + result.stdout).decode(errors='replace'), (label, mode, result.stderr)
            records.append({'driver': label, 'shared': shared, 'returncode': result.returncode, 'expected_rejection': diagnostic})
    for label, companion, env in companions:
        records.append({'companion': label, **failure(companion, original, area / label / 'companion-control', mode, env, path)})
    assert files(original) == before
    receipt = {'mode': mode, 'selected_body': target, 'descriptor_sha256': digest(path), 'expected_unsupported': True,
               'supported_original_pass': False, 'records': records}
    (area / 'unsupported-control.json').write_text(json.dumps(receipt, indent=2) + '\n')
    return receipt


def validate_passive_evidence(companion, probe, cli, corelib, directory):
    directory = Path(directory)
    assert not directory.exists()
    directory.mkdir(parents=True)
    prepared = []
    for mode in MODES:
        area = directory / mode
        original = create(probe, corelib, area, mode)
        raw = {}
        for arm, options in ARMS:
            for shared in (True, False):
                raw[(arm, shared)] = primary(cli, {}, original, area / ('original-' + arm + '-' + str(shared)), options, shared, mode)
        prepared.append((mode, area, original, raw))
    for mode, area, original, raw in prepared:
        first = publish(companion, probe, original, area / 'standalone-first', mode)
        repeat = publish(companion, probe, original, area / 'standalone-repeat', mode)
        assert first == repeat
        for arm, options in ARMS:
            for shared in (True, False):
                primary(cli, {}, original, area / ('integrated-' + arm + '-' + str(shared)), options, shared, mode,
                        integrated=True, expected=raw[(arm, shared)])
        executable_control((('current', cli, {}),), (('current', companion, {}),), original, area / 'executable-control', mode)
        print('graph-passive-evidence=' + mode + ':original-first,physical-IL,normal-body-rejects,both-sharing-and-options', flush=True)
    print('graph-validation=passive-evidence-body-phase,true-memberref,scalar-prefilter-negative,executable-unsupported-control', flush=True)
