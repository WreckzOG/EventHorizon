from pathlib import Path
import json,zipfile,shutil
from PIL import Image
root=Path(__file__).resolve().parent
manifest=json.loads((root/'manifest.json').read_text())
assert len(manifest['description'])<=250
assert Image.open(root/'icon.png').size==(256,256)
release=root/'package';release.mkdir(exist_ok=True)
path=release/('Event_Horizon-'+manifest['version_number']+'.zip')
with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
    for name in ['manifest.json','README.md','CHANGELOG.md','icon.png']:archive.write(root/name,name)
    archive.write(root/'src/EventHorizon.dll','plugins/EventHorizon/EventHorizon.dll')
with zipfile.ZipFile(path) as archive:
    assert archive.testzip() is None
    assert len(archive.namelist())==5
print(path, path.stat().st_size,'bytes; validated archive, manifest, icon and DLL.')
