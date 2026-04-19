import json
import subprocess
import zipfile
from pathlib import Path


def find_build_outputs(project_root: Path) -> tuple[Path, Path]:
    debug_dir = project_root / "bin" / "Debug"
    dll_candidates = sorted(debug_dir.glob("**/signals.dll"), key=lambda p: p.stat().st_mtime, reverse=True)

    if not dll_candidates:
        raise FileNotFoundError("signals.dll was not found under bin/Debug. Build may have failed.")

    dll_path = dll_candidates[0]
    pdb_path = dll_path.with_suffix(".pdb")

    if not pdb_path.exists():
        raise FileNotFoundError(f"Expected PDB next to DLL, but not found: {pdb_path}")

    return dll_path, pdb_path


def load_version(modinfo_path: Path) -> str:
    data = json.loads(modinfo_path.read_text(encoding="utf-8"))
    version = data.get("version")
    if not version or not isinstance(version, str):
        raise ValueError("modinfo.json is missing a string 'version' field.")
    return version


def add_file(zipf: zipfile.ZipFile, src: Path, arcname: str) -> None:
    zipf.write(src, arcname=arcname)
    print(f"Added {arcname}")


def main() -> int:
    project_root = Path(__file__).resolve().parent.parent
    modinfo_path = project_root / "modinfo.json"
    modicon_path = project_root / "modicon.png"
    assets_dir = project_root / "assets"

    if not modinfo_path.exists():
        raise FileNotFoundError(f"Missing file: {modinfo_path}")
    if not modicon_path.exists():
        raise FileNotFoundError(f"Missing file: {modicon_path}")
    if not assets_dir.is_dir():
        raise FileNotFoundError(f"Missing directory: {assets_dir}")

    print("Building Debug configuration...")
    subprocess.run(
        ["dotnet", "build", "signals.csproj", "-c", "Debug"],
        cwd=project_root,
        check=True,
    )

    version = load_version(modinfo_path)
    zip_name = f"signals-fipil_{version}.zip"
    zip_path = project_root / "bin" / zip_name

    dll_path, pdb_path = find_build_outputs(project_root)
    print(f"Using build output: {dll_path.parent}")

    zip_path.parent.mkdir(parents=True, exist_ok=True)

    if zip_path.exists():
        zip_path.unlink()

    print(f"Creating archive: {zip_path}")
    with zipfile.ZipFile(
        zip_path,
        mode="w",
        compression=zipfile.ZIP_DEFLATED,
        compresslevel=9,
        allowZip64=False,
        strict_timestamps=False,
    ) as zipf:
        add_file(zipf, dll_path, dll_path.name)
        add_file(zipf, pdb_path, pdb_path.name)
        add_file(zipf, modinfo_path, "modinfo.json")
        add_file(zipf, modicon_path, "modicon.png")

        for file_path in sorted(assets_dir.rglob("*")):
            if file_path.is_file():
                arcname = file_path.relative_to(project_root).as_posix()
                add_file(zipf, file_path, arcname)

    print(f"Done: {zip_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
