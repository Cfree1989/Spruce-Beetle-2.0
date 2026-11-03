#Author-Spruce Beetle
#Description-Duplicate a master setup for each scrap-numbered component and post an NC program per part.

"""Post one NC file per scrap from a single Fusion setup.

Lay Out Parts bakes a Rhino block per scrap. The block name is the scrap
number, and the block origin is the part corner. Insert that 3dm into the
fixture file. Components whose names are all digits are the parts. The
spoilboard and fence stay out of the toolpath because their names are not
scrap numbers.

Before running:
  1. Select the master setup in the browser. It needs the bore (or other
     hole toolpath), the machine, the fixture, and the work origin on the
     part corner.
  2. Set that setup's stock to a fixed-size box as big as the largest part
     if every scrap should share one stock. The script copies the master
     stock as it is.
  3. Post one program by hand first if you want the script to reuse that
     post. With no NC program in the file, the script uses the machine
     selected on the master setup.

Run:
  Utilities -> Scripts and Add-Ins -> Scripts -> + -> Add existing
  and choose this file. If Add existing is not in the menu, click the
  folder icon in that dialog and copy the PostParts folder into the
  folder it opens, then select PostParts and click Run.

Yes posts every scrap. No posts the first two so the bore can be checked
before the rest. A second run reuses setups already named with the scrap
number, regenerates them, and overwrites the NC files.
"""

import adsk.cam
import adsk.core
import adsk.fusion
import os
import traceback

# Browser names treated as the template setup when nothing is selected.
MASTER_NAMES = ('master', 'the setup')

# Scraps to post. Empty means every component whose name is a scrap number.
# Example: ('4', '73')
ONLY_SCRAPS = ()

# How many scraps the No button posts.
TEST_COUNT = 2

# Folder for the NC files. Empty asks every run.
# Example: r'C:\Users\cfree3\Desktop\nc'
OUTPUT_FOLDER = ''


def run(context):
    ui = None
    try:
        app = adsk.core.Application.get()
        ui = app.userInterface
        summary = post_parts(app, ui)
        if summary:
            ui.messageBox(summary, 'Post parts')
    except:
        text = 'Post parts failed:\n{}'.format(traceback.format_exc())
        log(text)
        if ui:
            ui.messageBox(text, 'Post parts')


def post_parts(app, ui):
    activate_manufacture(ui)
    doc = app.activeDocument
    if not doc:
        return 'Open the fixture file first.'

    design = adsk.fusion.Design.cast(doc.products.itemByProductType('DesignProductType'))
    cam = adsk.cam.CAM.cast(doc.products.itemByProductType('CAMProductType'))
    if not design or not cam:
        return 'This file has no design or manufacture data. Open the fixture file, switch to Manufacture once, and run the script again.'

    master, master_note = choose_master(cam, ui)
    if not master:
        return master_note

    if operation_count(master) == 0:
        return '"{}" has no operations. Put the bore on that setup, select it, and run the script again.'.format(master.name)

    parts, ignored = collect_parts(design)
    if ONLY_SCRAPS:
        wanted = set(ONLY_SCRAPS)
        parts = [occ for occ in parts if occ.component.name.strip() in wanted]
    if not parts:
        seen = ', '.join(ignored[:12]) or 'none'
        return (
            'No scrap components found. Bake Lay Out Parts and insert that 3dm '
            'so each part is a component named with its scrap number.\n\n'
            'Components seen: ' + seen
        )

    labels = master_last(assign_labels(parts), master)
    post, use_machine, post_note = find_post(cam)
    folder = OUTPUT_FOLDER.strip()
    if not folder:
        folder = ask_folder(ui)
        if not folder:
            return None
    os.makedirs(folder, exist_ok=True)

    limit = confirm_run(ui, master, master_note, labels, ignored, post_note, folder)
    if limit is None:
        return None
    labels = labels[:limit]

    progress = ui.createProgressDialog()
    progress.isCancelButtonShown = True
    progress.show('Post parts', '%v of %m', 0, len(labels))
    lines = [
        'Master: {}'.format(master.name),
        'Folder: {}'.format(folder),
        'Post: {}'.format(post_note),
        '',
    ]
    try:
        for index, (label, occ) in enumerate(labels):
            if progress.wasCancelled:
                lines.append('Stopped before {}.'.format(label))
                break
            progress.progressValue = index + 1
            try:
                progress.message = '{}  ({} of {})'.format(label, index + 1, len(labels))
            except Exception:
                pass
            adsk.doEvents()
            try:
                line = post_one(cam, master, label, occ, folder, post, use_machine)
            except Exception as error:
                line = '{}  FAILED  {}'.format(label, error)
                log(traceback.format_exc())
            lines.append(line)
            log(line)
            adsk.doEvents()
    finally:
        progress.hide()

    if ignored:
        lines.append('')
        lines.append('Ignored (name is not a scrap number):')
        for name in ignored[:20]:
            lines.append('  ' + name)

    log_path = os.path.join(folder, 'post-log.txt')
    with open(log_path, 'w', encoding='utf-8') as handle:
        handle.write('\n'.join(lines) + '\n')
    lines.append('')
    lines.append('Log: {}'.format(log_path))
    return '\n'.join(lines)


def post_one(cam, master, label, occ, folder, post, use_machine):
    setup = setup_for_part(cam, master, label, occ)
    problems = generate(cam, setup)
    if problems:
        return '{}  FAILED  {}'.format(label, '; '.join(problems))

    delete_programs_for(cam, label)
    program = add_program(cam, setup, label, folder, post, use_machine)
    options = adsk.cam.NCProgramPostProcessOptions.create()
    program.postProcess(options)
    adsk.doEvents()

    written = newest_output(folder, label)
    warnings = warning_text(setup)
    if not written:
        return '{}  FAILED  post wrote no file{}'.format(label, warnings)
    return '{}  posted  {}{}'.format(label, written, warnings)


def setup_for_part(cam, master, label, occ):
    """Copy the master once per scrap. A second run reuses the named setup.

    The master is left unchanged until every other copy exists, and only
    when the master itself is already named with that scrap number.
    """
    existing = find_named(cam.setups, label)
    if existing is not None and not same_setup(existing, master):
        target = existing
    elif master.name == label:
        target = master
    else:
        target = duplicate_setup(cam, master)
        if target is None:
            raise RuntimeError('Could not duplicate "{}".'.format(master.name))
        copied = operation_count(target)
        source = operation_count(master)
        if source and copied == 0:
            raise RuntimeError('The copy of "{}" has no operations.'.format(master.name))
        rename_operation(target, label)

    saved_fixtures = fixture_snapshot(target)
    try:
        set_models(target, occ)
    finally:
        restore_fixtures(target, saved_fixtures)
    assert_model(target, occ, label)
    set_param(target.parameters, 'job_programName', label)
    set_param(target.parameters, 'job_programComment', 'Scrap {}'.format(label))
    try:
        target.notes = 'Scrap {}'.format(label)
    except Exception:
        pass
    adsk.doEvents()
    return target


def duplicate_setup(cam, master):
    """duplicate() reports success or failure. The new setup is the one that was not there before."""
    before = set(setup.name for setup in iter_collection(cam.setups))
    result = master.duplicate()
    created = [setup for setup in iter_collection(cam.setups) if setup.name not in before]
    if len(created) == 1:
        return created[0]
    if result not in (None, True, False):
        setup = adsk.cam.Setup.cast(result)
        if setup:
            return setup
    return None


def set_models(setup, occurrence):
    models = adsk.core.ObjectCollection.create()
    models.add(occurrence)
    try:
        setup.models = models
        return
    except Exception:
        pass
    param = setup.parameters.itemByName('job_model')
    if not param:
        raise RuntimeError('Could not set the setup model.')
    value = adsk.cam.CadObjectParameterValue.cast(param.value)
    if not value:
        raise RuntimeError('Could not set the setup model.')
    value.value = [occurrence]


def assert_model(setup, occurrence, label):
    want_occ = token_of(occurrence)
    want_comp = token_of(occurrence.component)
    if not want_occ and not want_comp:
        if setup.models.count < 1:
            raise RuntimeError('Setup "{}" has no model.'.format(label))
        return
    for index in range(setup.models.count):
        model = setup.models.item(index)
        got = token_of(model)
        if want_occ and got == want_occ:
            return
        component = getattr(model, 'component', None)
        if component is not None and want_comp and token_of(component) == want_comp:
            return
        parent = getattr(model, 'parentComponent', None)
        if parent is not None and want_comp and token_of(parent) == want_comp:
            return
        native = getattr(model, 'nativeObject', None)
        if native is not None:
            parent = getattr(native, 'parentComponent', None)
            if parent is not None and want_comp and token_of(parent) == want_comp:
                return
    kinds = []
    for index in range(setup.models.count):
        kinds.append(setup.models.item(index).objectType)
    raise RuntimeError('Model did not switch to {}. Models now: {}'.format(label, ', '.join(kinds) or 'none'))


def fixture_snapshot(setup):
    try:
        if not setup.fixtureEnabled:
            return None
        fixtures = setup.fixtures
    except Exception:
        return None
    if fixtures is None or fixtures.count == 0:
        return None
    saved = adsk.core.ObjectCollection.create()
    for index in range(fixtures.count):
        saved.add(fixtures.item(index))
    return saved


def restore_fixtures(setup, saved):
    if saved is None or saved.count == 0:
        return
    try:
        setup.fixtureEnabled = True
        setup.fixtures = saved
    except Exception as error:
        log('Fixture restore failed: {}'.format(error))


def generate(cam, setup):
    operations = each_operation(setup)
    if not operations:
        return ['no operations']
    try:
        return generate_list(cam, operations)
    except Exception:
        drilled = [op for op in operations if op.objectType == adsk.cam.Operation.classType()]
        if not drilled or len(drilled) == len(operations):
            raise
        return generate_list(cam, drilled)


def generate_list(cam, operations):
    collection = adsk.core.ObjectCollection.create()
    for operation in operations:
        collection.add(operation)
    future = cam.generateToolpath(collection)
    while not future.isGenerationCompleted:
        adsk.doEvents()
    adsk.doEvents()
    problems = []
    for operation in operations:
        if operation.hasError:
            problems.append('{}: {}'.format(operation.name, operation.error or 'generation error'))
    return problems


def warning_text(setup):
    notes = []
    for operation in each_operation(setup):
        if operation.hasWarning:
            notes.append('{}: {}'.format(operation.name, operation.warning or 'warning'))
    if not notes:
        return ''
    return '  (warning: {})'.format('; '.join(notes))


def add_program(cam, setup, label, folder, post, use_machine):
    stem = file_stem(label)
    last_error = None
    for browser_name in (label, 'NC {}'.format(label)):
        try:
            program_input = cam.ncPrograms.createInput()
            program_input.displayName = browser_name
            program_input.operations = [setup]
            params = program_input.parameters
            set_param(params, 'nc_program_filename', stem)
            set_param(params, 'nc_program_comment', 'Scrap {}'.format(label))
            set_param(params, 'nc_program_openInEditor', False)
            set_param(params, 'nc_program_useMachineConfig', bool(use_machine))
            set_param(params, 'nc_program_output_folder', folder.replace('\\', '/'))
            set_param(params, 'nc_program_postToFusionTeam', False)
            program = cam.ncPrograms.add(program_input)
            if post is not None and not use_machine:
                program.postConfiguration = post
            elif post is not None and program.postConfiguration is None:
                program.postConfiguration = post
            return program
        except Exception as error:
            last_error = error
    raise last_error


def delete_programs_for(cam, label):
    stem = file_stem(label)
    names = set((label, 'NC {}'.format(label)))
    doomed = []
    for program in iter_collection(cam.ncPrograms):
        if program.name in names:
            doomed.append(program)
            continue
        filename = ''
        try:
            filename = program.parameters.itemByName('nc_program_filename').value.value
        except Exception:
            filename = ''
        if filename == stem:
            doomed.append(program)
    for program in doomed:
        program.deleteMe()


def find_post(cam):
    """Use the post from the last NC program already in the file."""
    if cam.ncPrograms.count == 0:
        return None, True, 'machine on the master setup (no NC program in the file yet)'
    template = cam.ncPrograms.item(cam.ncPrograms.count - 1)
    post = None
    use_machine = True
    try:
        post = template.postConfiguration
    except Exception:
        post = None
    try:
        use_machine = bool(template.parameters.itemByName('nc_program_useMachineConfig').value.value)
    except Exception:
        use_machine = post is None
    if post is None and not use_machine:
        note = '"{}" has no post. Post one program by hand, then run this again.'.format(template.name)
    elif use_machine:
        note = 'machine config from "{}"'.format(template.name)
    else:
        description = ''
        try:
            description = post.description or ''
        except Exception:
            description = ''
        note = 'post from "{}" {}'.format(template.name, description).strip()
    return post, use_machine, note


def choose_master(cam, ui):
    if cam.setups.count == 0:
        return None, 'This file has no setups. Make the master setup first.'

    selected = [setup for setup in iter_collection(cam.setups) if safe_selected(setup)]
    if len(selected) == 1:
        return selected[0], 'selected in the browser'
    if len(selected) > 1:
        named = [setup for setup in selected if is_master_name(setup.name)]
        if len(named) == 1:
            return named[0], 'selected in the browser'
        return selected[0], 'first of {} selected setups'.format(len(selected))

    for setup in iter_collection(cam.setups):
        if is_master_name(setup.name):
            return setup, 'name matches MASTER'

    names = [setup.name for setup in iter_collection(cam.setups)]
    preview = '\n'.join(names[:30])
    default = names[0]
    entered, cancelled = ui.inputBox(
        'Click the master setup in the browser and run again, or type its name.\n\n' + preview,
        'Master setup',
        default,
    )
    if cancelled:
        return None, None
    match = find_named(cam.setups, entered.strip())
    if match is None:
        return None, 'No setup named "{}".'.format(entered.strip())
    return match, 'typed name'


def confirm_run(ui, master, master_note, labels, ignored, post_note, folder):
    names = [label for label, _occ in labels]
    preview = ', '.join(names[:8])
    if len(names) > 8:
        preview += ', ...'
    lines = [
        'Master: {} ({})'.format(master.name, master_note),
        'Operations: {}'.format(operation_count(master)),
        'Scraps: {} ({})'.format(len(names), preview),
        'Stock: {}'.format(stock_note(master)),
        'Post: {}'.format(post_note),
        'Folder: {}'.format(folder),
    ]
    origin = param_expression(master, 'wcs_origin_mode')
    box_point = param_expression(master, 'wcs_origin_boxPoint')
    if origin or box_point:
        lines.append('Origin: {} {}'.format(origin or '', box_point or '').strip())
    if parts_are_spread([occ for _label, occ in labels]):
        lines.append(
            'Parts are spread out in the file. The posted origin is the master '
            'origin. A model box point or model origin stays on each part corner. '
            'A point picked on the spoilboard stays on the spoilboard.'
        )
    if ignored:
        lines.append('Ignored {} component(s) whose names are not scrap numbers.'.format(len(ignored)))
    lines.append('')
    lines.append('Yes posts all {}. No posts the first {} ({}). Cancel does nothing.'.format(
        len(names),
        min(TEST_COUNT, len(names)),
        ', '.join(names[:TEST_COUNT]),
    ))
    button = ui.messageBox(
        '\n'.join(lines),
        'Post parts',
        adsk.core.MessageBoxButtonTypes.YesNoCancelButtonType,
    )
    if button == adsk.core.DialogResults.DialogYes:
        return len(labels)
    if button == adsk.core.DialogResults.DialogNo:
        return min(TEST_COUNT, len(labels))
    return None


def collect_parts(design):
    seen = set()
    parts = []
    ignored = []
    for occ in iter_collection(design.rootComponent.allOccurrences):
        key = occurrence_key(occ)
        if key in seen:
            continue
        seen.add(key)
        name = occ.component.name.strip()
        if not name.isdigit():
            if name:
                ignored.append(name)
            continue
        if not has_solid(occ.component):
            ignored.append('{} (no solid)'.format(name))
            continue
        parts.append(occ)
    parts.sort(key=lambda occ: int(occ.component.name.strip()))
    unique_ignored = []
    for name in ignored:
        if name not in unique_ignored:
            unique_ignored.append(name)
    return parts, unique_ignored


def master_last(labels, master):
    """Edit a master named with a scrap number only after the other copies exist."""
    front = []
    back = []
    for label, occ in labels:
        if label == master.name:
            back.append((label, occ))
        else:
            front.append((label, occ))
    return front + back


def same_setup(a, b):
    token_a = token_of(a)
    token_b = token_of(b)
    if token_a and token_b:
        return token_a == token_b
    try:
        return a.operationId == b.operationId
    except Exception:
        return False


def assign_labels(occurrences):
    """One program name per instance. A second instance of the same number gets a _2 suffix."""
    used = {}
    labeled = []
    for occ in occurrences:
        base = occ.component.name.strip()
        used[base] = used.get(base, 0) + 1
        label = base if used[base] == 1 else '{}_{}'.format(base, used[base])
        labeled.append((label, occ))
    return labeled


def ask_folder(ui):
    dialog = ui.createFolderDialog()
    dialog.title = 'Folder for NC programs'
    if dialog.showDialog() != adsk.core.DialogResults.DialogOK:
        return None
    return dialog.folder


def newest_output(folder, label):
    stem = file_stem(label)
    matches = []
    try:
        names = os.listdir(folder)
    except OSError:
        return None
    for name in names:
        file_stem_on_disk, _ext = os.path.splitext(name)
        if file_stem_on_disk == stem:
            matches.append(os.path.join(folder, name))
    if not matches:
        return None
    matches.sort(key=os.path.getmtime, reverse=True)
    return matches[0]


def each_operation(setup):
    operations = []
    all_ops = setup.allOperations
    for index in range(all_ops.count):
        operation = all_ops.item(index)
        try:
            if operation.isSuppressed:
                continue
        except Exception:
            pass
        operations.append(operation)
    return operations


def operation_count(setup):
    try:
        return setup.allOperations.count
    except Exception:
        return len(each_operation(setup))


def has_solid(component):
    bodies = component.bRepBodies
    for index in range(bodies.count):
        if bodies.item(index).isSolid:
            return True
    return False


def parts_are_spread(occurrences):
    points = []
    for occ in occurrences:
        try:
            points.append(occ.boundingBox.minPoint)
        except Exception:
            return False
    if len(points) < 2:
        return False
    origin = points[0]
    for point in points[1:]:
        if abs(point.x - origin.x) > 0.1 or abs(point.y - origin.y) > 0.1 or abs(point.z - origin.z) > 0.1:
            return True
    return False


def stock_note(setup):
    try:
        mode = setup.stockMode
    except Exception:
        return 'copied from the master'
    modes = adsk.cam.SetupStockModes
    if mode == getattr(modes, 'RelativeBoxStock', None):
        return 'relative-size box. It resizes to each scrap. Set the master to a fixed-size box first if you want one stock for the whole set.'
    if mode == getattr(modes, 'FixedBoxStock', None):
        return 'fixed-size box, copied from the master'
    if mode == getattr(modes, 'SolidStock', None):
        return 'solid body, copied from the master'
    return 'copied from the master'


def activate_manufacture(ui):
    workspace = ui.workspaces.itemById('CAMEnvironment')
    if workspace and not workspace.isActive:
        workspace.activate()


def find_named(collection, name):
    for item in iter_collection(collection):
        if item.name == name:
            return item
    return None


def iter_collection(collection):
    items = []
    for index in range(collection.count):
        items.append(collection.item(index))
    return items


def rename_operation(operation, name):
    if operation.name == name:
        return name
    try:
        operation.name = name
        return operation.name
    except Exception:
        operation.name = 'Scrap {}'.format(name)
        return operation.name


def set_param(parameters, name, value):
    param = parameters.itemByName(name)
    if not param:
        return False
    try:
        param.value.value = value
        return True
    except Exception:
        pass
    try:
        if isinstance(value, bool):
            param.expression = 'true' if value else 'false'
        elif isinstance(value, str):
            param.expression = "'{}'".format(value.replace("'", ''))
        else:
            param.expression = str(value)
        return True
    except Exception:
        return False


def param_expression(owner, name):
    try:
        param = owner.parameters.itemByName(name)
    except Exception:
        return ''
    if not param:
        return ''
    try:
        return param.expression or ''
    except Exception:
        return ''


def file_stem(label):
    cleaned = []
    for char in str(label):
        if char.isalnum() or char in ('-', '_'):
            cleaned.append(char)
        else:
            cleaned.append('_')
    stem = ''.join(cleaned).strip('_')
    return stem or 'part'


def token_of(obj):
    try:
        token = obj.entityToken
    except Exception:
        return None
    return token or None


def occurrence_key(occ):
    try:
        return occ.fullPathName
    except Exception:
        return token_of(occ) or str(id(occ))


def safe_selected(setup):
    try:
        return bool(setup.isSelected)
    except Exception:
        return False


def is_master_name(name):
    return name.strip().casefold() in MASTER_NAMES


def log(message):
    try:
        adsk.core.Application.get().log(str(message))
    except Exception:
        pass
