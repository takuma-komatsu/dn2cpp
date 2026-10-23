from pathlib import Path
import hashlib
import shutil
import subprocess
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from ildiet_manifest import generation, manifest as read_manifest
import xml.etree.ElementTree as ET

cli = Path(sys.argv[1]).resolve()
source = Path(sys.argv[2]).resolve()
corelib = Path(sys.argv[3]).resolve()
work = Path(sys.argv[4]).resolve()
work.mkdir(parents=True, exist_ok=True)
inputs = work / 'inputs'
inputs.mkdir(exist_ok=True)
app = inputs / source.name
shutil.copy2(source, app)
refs = [corelib] + [corelib.parent / (name + '.dll') for name in (
    'System.Runtime', 'System.Console', 'System.Collections', 'System.Linq', 'System.Threading')]
base = ['dotnet', str(cli), str(app)]
for ref in refs:
    base += ['-r', str(ref)]
out = work / 'out'


def run(extra, success=True):
    result = subprocess.run(base + extra, text=True, capture_output=True)
    if (result.returncode == 0) != success:
        raise AssertionError((result.returncode, result.stdout, result.stderr))
    return result


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot(path):
    return {p.relative_to(path).as_posix(): digest(p) for p in path.rglob('*') if p.is_file()}


original = digest(app)
run(['-o', str(out)])
assert len(ET.parse(generation(out) / 'preservation.xml').getroot()) == 0
first = snapshot(out)
(out / 'stale.pdb').write_text('stale')
run(['-o', str(out)])
assert snapshot(out) == first
for target in [inputs, app]:
    run(['-o', str(target)], False)
run(['-o', str(out), '--result', str(app)], False)
assert snapshot(out) == first and digest(app) == original

published = generation(out)
payload = snapshot(published)
case_pointer = out / 'ILDiet-Result.xml'
run(['-o', str(out), '--result', str(case_pointer)])
assert case_pointer.is_file(), 'requested case-variant manifest was not written'
result, case_generation = read_manifest(out, case_pointer)
assert result.get('version') == '2' and case_generation == published
assert digest(case_pointer) == digest(out / 'ildiet-result.xml')
assert snapshot(published) == payload and digest(app) == original
run(['-o', str(out)])
assert snapshot(published) == payload and digest(app) == original
first = snapshot(out)
print('PASS requested case-variant manifest exists and matches the published generation', flush=True)

case_app = app.with_name(app.name.swapcase())
case_inputs = inputs.with_name(inputs.name.swapcase())
case_out = out.with_name(out.name.swapcase())
if case_app.exists() and case_inputs.exists() and case_out.exists():
    run(['-o', str(out), '--result', str(case_app)], False)
    run(['-o', str(case_inputs)], False)
    assert snapshot(out) == first and digest(app) == original
    published = generation(out)
    payload = snapshot(published)
    case_result = case_out / 'case-result.xml'
    run(['-o', str(out), '--result', str(case_result)])
    result, case_generation = read_manifest(out, case_result)
    assert result.get('version') == '2' and case_generation == published
    assert digest(case_result) == digest(out / 'ildiet-result.xml')
    assert snapshot(published) == payload and digest(app) == original
    assert snapshot(out) == {**first, 'case-result.xml': digest(case_result)}
    first = snapshot(out)
    for reserved in (case_out / 'preservation.xml', case_out / published.name / 'preservation.xml'):
        run(['-o', str(out), '--result', str(reserved)], False)
        assert snapshot(out) == first and digest(app) == original
    print('PASS input aliases protected, ordinary case-result manifest validated with unchanged payload, '
          'reserved case aliases rejected', flush=True)

unowned = work / 'unowned'
unowned.mkdir(exist_ok=True)
(unowned / 'mine').write_text('keep')
run(['-o', str(unowned)], False)
assert (unowned / 'mine').read_text() == 'keep'

project = work / 'project'
(project / 'nested').mkdir(parents=True, exist_ok=True)
link = project / 'nested/link.xml'
link.write_text('<linker />')
link_hash = digest(link)
run(['-o', str(project), '--project-root', str(project)], False)
run(['-o', str(out), '--project-root', str(project), '--result', str(link)], False)
assert digest(link) == link_hash and snapshot(out) == first

request = work / 'request.xml'
request_xml = ET.Element('ildiet', version='2', input=str(app), output=str(out), copyAll='false')
for ref in refs:
    ET.SubElement(request_xml, 'reference', path=str(ref))
ET.ElementTree(request_xml).write(request, encoding='unicode')
request_hash = digest(request)
result = subprocess.run(['dotnet', str(cli), '--request', str(request), '--result', str(request)],
                        capture_output=True, text=True)
assert result.returncode != 0 and digest(request) == request_hash and snapshot(out) == first

manifest = out / 'results/result.xml'
run(['-o', str(out), '--result', str(manifest)])
assert ET.parse(manifest).getroot().tag == 'ildietResult'
run(['-o', str(out)])
assert snapshot(out) == {**first, 'results/result.xml': digest(manifest)}
first = snapshot(out)
print('PASS output determinism, owned replacement, stale PDB removal, empty preservation, '
      'source/request/descriptor protection, inside-output manifests', flush=True)

# Python creates actual OS links; MSYS ln may silently copy on Windows.
alias = work / 'descriptor-alias.xml'
output_alias = work / 'output-alias'
try:
    alias.unlink(missing_ok=True)
    alias.symlink_to(link)
    output_alias.unlink(missing_ok=True)
    output_alias.symlink_to(out, target_is_directory=True)
except OSError as error:
    print('Symlink regression prerequisite unavailable: the host must permit real file and '
          'directory symlinks (Windows Developer Mode or elevated privileges): ' + str(error),
          file=sys.stderr)
    sys.exit(77)

run(['-o', str(out), '--project-root', str(project), '--result', str(alias)], False)
assert digest(link) == link_hash and snapshot(out) == first
run(['-o', str(output_alias)], False)
run(['-o', str(out), '--result', str(output_alias / 'alias-result.xml')])
assert ET.parse(out / 'alias-result.xml').getroot().tag == 'ildietResult'
assert digest(app) == original
print('PASS descriptor/output symlink aliases cannot overwrite inputs or escape staging')
