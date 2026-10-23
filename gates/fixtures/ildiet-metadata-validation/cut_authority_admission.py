"""Raw-primary observations derived from the unchanged passive-evidence helper."""
from pathlib import Path
import re
import json
import os
import shutil
import sys
import xml.etree.ElementTree as ET
from passive_evidence_admission import create, executable_control, run, files, generated, COPIED
from copied_array_admission import digest, manifest, contract_digest, framed_hash

MODES = ("passive-async-true-carrier", "passive-async-false-prefilter")
INPUT_NAMES = ("GraphApp.dll", "GraphLib.dll", "System.ArrayFacade.dll", "System.ArrayLeaf.dll", "DnZlib.dll", "GraphTaskB.dll", "System.Private.CoreLib.dll", "Dn2Cpp.Runtime.dll")
ARMS = (("default", ()), ("no-adopt-a", ("GraphLib",)))


def cli_command(cli, original, descriptor, cut, destination, options, shared, raw=False):
    command = ['dotnet', 'exec', cli, original / INPUT_NAMES[0]]
    for name in INPUT_NAMES[1:]:
        command += ['-r', original / name]
    for assembly in options:
        command += ['--no-adopt-async', assembly]
    command += ['--link-xml', descriptor, '--shared-generics' if shared else '--no-shared-generics', '-o', destination]
    if cut is not None:
        command += ['--cut', cut]
    if raw:
        command += ['--no-ildiet']
    return list(map(str, command))


def primary(cli, env, original, descriptor, cut, area, options, shared, variant, raw=True, expected=None):
    assert not area.exists()
    before = files(original)
    destination = area / 'cpp'
    command = cli_command(cli, original, descriptor, cut, destination, options, shared, raw=raw)
    result = run(command, area, 'primary', env)
    assert result.returncode == 0, ('Original primary unsupported' if raw else 'Integrated primary failed', variant, options, shared, area)
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
    if raw:
        assert not (destination / 'ildiet').exists()
    else:
        root, published = manifest(destination / 'ildiet')
        assert root.get('fallback') == 'none' and root.get('cutsValidated') == 'true'
        assert root.find('counters').get('decode.max-per-method') == '1'
        for name in COPIED:
            assert (published / name).read_bytes() == (original / name).read_bytes()
    if expected is not None:
        assert counts == expected['admission_counts'] and notices == expected['decline_notices']
    assert files(original) == before
    return {'shared': shared, 'no_adopt': list(options), 'admission_counts': counts,
            'generated_sha256': emitted_files, 'decline_notices': notices, 'dead_carriers_emitted': False,
            'command': list(map(str, command))}


def save(path, value):
    with Path(path).open('x') as stream:
        json.dump(value, stream, indent=2, sort_keys=True)
        stream.write('\n')


def tree_files(directory):
    rows = {}
    for path in Path(directory).rglob('*'):
        assert not path.is_symlink(), path
        if path.is_file():
            rows[path.relative_to(directory).as_posix()] = {'sha256': digest(path), 'mode': path.stat().st_mode & 0o777}
    return dict(sorted(rows.items()))


def descriptors(original, area, mode):
    target = 'Decline' if mode == MODES[0] else 'PrefilterOnly'
    root = ET.parse(original / 'link.xml').getroot()
    assembly = ET.SubElement(root, 'assembly', fullname='GraphApp', preserve='nothing')
    owner = ET.SubElement(assembly, 'type', fullname='Graph.EagerDeadCross', preserve='nothing')
    ET.SubElement(owner, 'method', name=target)
    path = area / 'executable.xml'
    with path.open('xb') as stream:
        ET.ElementTree(root).write(stream, encoding='utf-8', xml_declaration=True)
    return 'Graph.EagerDeadCross::' + target, {'cut-only': original / 'link.xml', 'cut-and-descriptor': path}


def clone_capture_driver(cli, probe, destination):
    probe = Path(probe).resolve(strict=True)
    before = tree_files(cli.parent)
    assert 'ildiet/ILDiet' not in before and 'ildiet/ILDiet.exe' not in before
    shutil.copytree(cli.parent, destination)
    assert tree_files(destination) == before
    # The managed companion route works without Unix shebang execution or modes.
    payload = {path.name: path for path in probe.parent.iterdir()
               if path.is_file() and path.suffix in ('.dll', '.json')}
    for extension in ('.dll', '.runtimeconfig.json', '.deps.json'):
        payload['ILDiet' + extension] = probe.with_suffix(extension)
    payload['cut_request_capture.py'] = Path(__file__).with_name('cut_request_capture.py').resolve(strict=True)
    expected = dict(before)
    for name, source in payload.items():
        assert source.is_file() and not source.is_symlink(), source
        expected['ildiet/' + name] = {'sha256': digest(source), 'mode': source.stat().st_mode & 0o777}
        shutil.copy2(source, destination / 'ildiet' / name)
    assert tree_files(destination) == expected
    assert tree_files(cli.parent) == before
    return destination / 'dn2cpp.dll'


def semantics(root):
    return [root.tag, {k: v for k, v in root.attrib.items() if k != 'output'},
            [[n.tag, dict(n.attrib)] for n in root]]


def capture(cli, env, original, descriptor, cut, area, options, shared):
    command = cli_command(cli, original, descriptor, cut, area / 'cpp', options, shared)
    result = run(command, area, 'capture', {**env, 'CUT_CAPTURE_DIR': str(area / 'capture'),
                 'CUT_CAPTURE_PYTHON': str(Path(sys.executable).resolve(strict=True))})
    assert result.returncode != 0 and b'ILDiet failed with exit code 86' in result.stdout + result.stderr
    record = json.loads((area / 'capture/capture.json').read_bytes())
    assert record['kind'] == 'cut-request-only' and record['companion_started'] is False and record['exit_code'] == 86
    assert record['inputs'] == [{'path': str(original / name), 'sha256': digest(original / name)} for name in INPUT_NAMES]
    path = area / 'capture/request.xml'
    root = ET.parse(path).getroot()
    assert root.tag == 'ildiet' and root.attrib == {'version': '2', 'input': str(original / INPUT_NAMES[0]),
           'output': str(area / 'cpp/ildiet'), 'copyAll': 'false'}
    assert [n.tag for n in root] == ['reference'] * 7 + ['linkXml', 'cut']
    assert [n.attrib for n in root.findall('reference')] == [{'path': str(original / name)} for name in INPUT_NAMES[1:]]
    assert [n.attrib for n in root.findall('linkXml')] == [{'path': str(descriptor)}]
    assert [n.attrib for n in root.findall('cut')] == [{'method': cut}]
    assert all(len(n) == 0 for n in root)
    return path


def publication(probe, original, output, mode, request=None, env=None):
    root, published = manifest(output)
    counters = dict(root.find('counters').attrib)
    assert root.get('fallback') == 'none' and root.get('cutsValidated') == 'true'
    assert counters['decode.max-per-method'] == '1' and int(counters['methods.evidence-body']) >= 1
    if request is not None:
        assert root.get('contract') == contract_digest(request)
    assert [Path(n.get('path')).name for n in root.findall('reference')] == list(INPUT_NAMES[1:])
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
    checked = run(['dotnet', 'exec', probe, '--check-cut-passive-fixtures', original, published, mode], output.parent,
                  'physical-check', env)
    lines = [line for line in checked.stdout.decode().splitlines() if line.startswith('cut-authority-observed=')]
    assert checked.returncode == 0 and len(lines) == 1, checked.stderr
    observation = json.loads(lines[0].split('=', 1)[1])
    closure = run(['dotnet', 'exec', probe, '--check-token-closure', *sorted(published.glob('*.dll'))], output.parent, 'closure', env)
    assert closure.returncode == 0 and b'token-closure=valid operands=' in closure.stdout
    for name in INPUT_NAMES:
        before = run(['dotnet', 'exec', probe, '--relations', original / name], output.parent, 'before-' + name, env)
        after = run(['dotnet', 'exec', probe, '--relations', published / name], output.parent, 'after-' + name, env)
        assert before.returncode == after.returncode == 0 and set(after.stdout.splitlines()) <= set(before.stdout.splitlines())
    return {'files': files(published), 'generation': root.get('generation'), 'counters': counters, 'observation': observation}


def publish(companion, probe, original, captured, area, mode, env=None):
    assert not area.exists()
    area.mkdir(parents=True)
    tree = ET.parse(captured)
    expected = semantics(tree.getroot())
    tree.getroot().set('output', str(area / 'publication'))
    assert semantics(tree.getroot()) == expected
    path = area / 'request.xml'
    with path.open('xb') as stream:
        tree.write(stream, encoding='utf-8', xml_declaration=True)
    save(area / 'request-provenance.json', {'captured': str(captured), 'sha256': digest(captured),
         'only_semantic_change': 'output', 'semantics': expected})
    result = run(['dotnet', 'exec', companion, '--request', path], area, 'companion', env)
    assert result.returncode == 0, (mode, result.stderr.decode(errors='replace'))
    record = publication(probe, original, area / 'publication', mode, tree.getroot(), env)
    save(area / 'receipt.json', record)
    return record


def backend_request(original, corelib, area):
    root = ET.Element('ildiet', version='2', input=str(original / 'PolicyApp.dll'), output=str(area / 'publication'), copyAll='false')
    ET.SubElement(root, 'reference', path=str(original / 'PolicyLibrary.dll'))
    ET.SubElement(root, 'reference', path=str(corelib))
    ET.SubElement(root, 'rewriteAssembly', name='PolicyLibrary')
    ET.SubElement(root, 'conditionalMembers', assembly='PolicyApp', baseType='Policy.Root')
    ET.SubElement(root, 'typeRoot', assembly='PolicyApp', type='PolicyFixture.ExplicitScript')
    ET.SubElement(root, 'methodRoot', assembly='PolicyLibrary', type='Policy.Registry', method='.cctor')
    ET.SubElement(root, 'registrationAttribute', assembly='PolicyLibrary', type='Policy.RegistrationsAttribute')
    ET.SubElement(root, 'constructorRegistry', assembly='PolicyLibrary', type='Policy.Registry', method='.cctor')
    for name in ('FactorySignatureWrapper', 'FactoryLive'):
        ET.SubElement(root, 'cut', method='Policy.Registry::' + name)
    return root


def backend(companion, probe, corelib, directory, env=None):
    assert not directory.exists()
    original = directory / 'input'
    created = run(['dotnet', 'exec', probe, '--create-backend-policy-fixture', original], directory, 'create', env)
    assert created.returncode == 0 and created.stdout.splitlines() == [b'fixture-created=backend-policy']
    before = tree_files(original)
    records = []
    for repetition in ('first', 'repeat'):
        area = directory / repetition
        area.mkdir()
        request = backend_request(original, corelib, area)
        path = area / 'request.xml'
        with path.open('xb') as stream:
            ET.ElementTree(request).write(stream, encoding='utf-8', xml_declaration=True)
        result = run(['dotnet', 'exec', companion, '--request', path], area, 'companion', env)
        assert result.returncode == 0, result.stderr
        root, published = manifest(area / 'publication')
        assert root.get('fallback') == 'none' and root.get('cutsValidated') == 'true'
        assert root.get('constructorRegistriesRewritten') == 'true' and root.get('contract') == contract_digest(request)
        assert root.find('counters').get('decode.max-per-method') == '1'
        assert [Path(n.get('path')).name for n in root.findall('reference')] == ['PolicyLibrary.dll', corelib.name]
        assert {p.name for p in published.glob('*.dll')} == {'PolicyApp.dll', 'PolicyLibrary.dll', corelib.name}
        assert (published / corelib.name).read_bytes() == corelib.read_bytes()
        values = []
        for item in root.findall('assembly'):
            name = item.get('name') + '.dll'
            values.extend((name, (published / name).read_bytes()))
            assert item.get('mode') == ('copied' if name == corelib.name else 'rewritten')
            if item.get('mode') == 'copied':
                assert (published / name).read_bytes() == corelib.read_bytes() and name == corelib.name
        values.extend(('preservation.xml', (published / 'preservation.xml').read_bytes()))
        assert root.get('generation') == 'g-' + framed_hash(values)
        checked = run(['dotnet', 'exec', probe, '--check-cut-backend-fixture', original, published], area, 'retained-check', env)
        assert checked.returncode == 0 and checked.stdout.splitlines() == [
            b'cut-backend-contract=physical-factories,original-pointer,ancestor-redirect,ctor-stub,callback-runtime-negative'], checked.stderr
        closure = run(['dotnet', 'exec', probe, '--check-token-closure', *sorted(published.glob('*.dll'))], area, 'closure', env)
        assert closure.returncode == 0 and b'token-closure=valid operands=' in closure.stdout
        for name in ('PolicyApp.dll', 'PolicyLibrary.dll'):
            earlier = run(['dotnet', 'exec', probe, '--relations', original / name], area, 'before-' + name, env)
            later = run(['dotnet', 'exec', probe, '--relations', published / name], area, 'after-' + name, env)
            assert earlier.returncode == later.returncode == 0
            assert set(later.stdout.splitlines()) <= set(earlier.stdout.splitlines())
        records.append({'files': files(published), 'generation': root.get('generation'), 'counters': dict(root.find('counters').attrib)})
    assert records[0] == records[1] and tree_files(original) == before
    save(directory / 'receipt.json', {'kind': 'standalone-backend-cut-contract', 'primary_equivalence_claimed': False, 'record': records[0]})
    print('graph-cut-backend=standalone-contract,physical-factories,ancestor-redirect,no-constructor-runtime-promotion', flush=True)
    return records[0]


def validate_cut_authority(companion, probe, cli, corelib, directory):
    directory = Path(directory).resolve()
    assert not directory.exists()
    directory.mkdir(parents=True)
    prepared = []
    for mode in MODES:
        area = directory / mode
        original = create(probe, corelib, area, mode)
        cut, selectors = descriptors(original, area, mode)
        raw = {}
        for selector, descriptor in selectors.items():
            for arm, options in ARMS:
                for shared in (True, False):
                    raw[(selector, arm, shared)] = primary(cli, {}, original, descriptor, cut,
                        area / selector / arm / str(shared) / 'raw', options, shared, mode)
        prepared.append((mode, area, original, cut, selectors, raw))
    capture_cli = clone_capture_driver(cli, probe, directory / 'capture-cli')
    capture_pin = tree_files(capture_cli.parent)
    captures = {}
    for mode, area, original, cut, selectors, raw in prepared:
        for selector, descriptor in selectors.items():
            previous = None
            for arm, options in ARMS:
                for shared in (True, False):
                    path = capture(capture_cli, {}, original, descriptor, cut,
                                   area / selector / arm / str(shared) / 'capture', options, shared)
                    value = semantics(ET.parse(path).getroot())
                    assert previous is None or previous == value
                    previous = value
                    captures[(mode, selector)] = path
    for mode, area, original, cut, selectors, raw in prepared:
        before = tree_files(original)
        for selector, descriptor in selectors.items():
            records = [publish(companion, probe, original, captures[(mode, selector)],
                       area / selector / ('standalone-' + str(i)), mode) for i in (1, 2)]
            assert records[0] == records[1]
            for arm, options in ARMS:
                for shared in (True, False):
                    runs = []
                    for repetition in (1, 2):
                        dest = area / selector / arm / str(shared) / ('integrated-' + str(repetition))
                        runs.append(primary(cli, {}, original, descriptor, cut, dest, options, shared, mode,
                                            raw=False, expected=raw[(selector, arm, shared)]))
                        checked = publication(probe, original, dest / 'cpp/ildiet', mode)
                        assert checked['files'] == records[0]['files'] and checked['observation'] == records[0]['observation']
                    assert runs[0]['generated_sha256'] == runs[1]['generated_sha256']
        assert tree_files(original) == before
        print('graph-cut-authority=' + mode + ':cut-only,and-descriptor,both-sharing-and-options,physical-only', flush=True)
    assert tree_files(capture_cli.parent) == capture_pin
    backend(companion, probe, corelib, directory / 'backend-registry')
    print('graph-validation=cut-authority,originals-first,cli-xml,physical-il,scalar-token-negative,backend-factory-contract', flush=True)
