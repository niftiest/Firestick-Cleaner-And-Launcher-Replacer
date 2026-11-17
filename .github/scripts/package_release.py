"""Build the four release zips from the committed tree and the WinForms build output.

Run from the repository root:
    python .github/scripts/package_release.py --version 1.4.0 --winform-dir build/winform --out dist

Source files come from `git archive HEAD`, so they get the line endings .gitattributes asks for
(CRLF for .bat, LF for .sh/.command) and the file modes git records (the Mac and Linux scripts and
adb binaries stay executable). Paths inside the zips use forward slashes.
"""
import argparse
import io
import os
import stat
import subprocess
import tarfile
import zipfile

SCRIPT = "Firestick-Cleaner-And-Launcher-Replacer"
FIXED_TIME = (2026, 1, 1, 0, 0, 0)


def zip_info(name, mode):
    info = zipfile.ZipInfo(name, date_time=FIXED_TIME)
    info.create_system = 3  # Unix, so unzip tools honor the mode bits
    info.external_attr = ((mode & 0o777) | stat.S_IFREG) << 16
    info.compress_type = zipfile.ZIP_DEFLATED
    return info


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", required=True, help="version string used in the zip names, e.g. 1.4.0")
    ap.add_argument("--winform-dir", required=True, help="dotnet build output folder holding FirestickCleanup.exe")
    ap.add_argument("--out", required=True, help="folder to write the zips to")
    args = ap.parse_args()

    tar_bytes = subprocess.run(["git", "archive", "--format=tar", "HEAD"], check=True, capture_output=True).stdout
    tar = tarfile.open(fileobj=io.BytesIO(tar_bytes))
    files = {m.name: m for m in tar.getmembers() if m.isfile()}

    def from_git(path, dest):
        member = files[path]
        fh = tar.extractfile(member)
        if fh is None:
            raise SystemExit(f"could not read {path} from git archive")
        return dest, member.mode, fh.read()

    def from_disk(path, dest, mode=0o644):
        with open(path, "rb") as fh:
            return dest, mode, fh.read()

    def adb(platform):
        prefix = f"{platform}/adb/"
        return [from_git(p, p[len(platform) + 1:]) for p in sorted(files) if p.startswith(prefix)]

    readme = from_git("README.md", "README.md")
    packages = {
        "Linux": adb("linux") + [from_git(f"linux/{SCRIPT}.sh", f"{SCRIPT}.sh"), readme],
        "Mac": adb("mac") + [from_git(f"mac/{SCRIPT}.command", f"{SCRIPT}.command"), readme],
        "Windows-Batch": adb("windows") + [from_git(f"windows/{SCRIPT}.bat", f"{SCRIPT}.bat"), readme],
        "Windows-WinForm": adb("windows") + [
            from_disk(os.path.join(args.winform_dir, "FirestickCleanup.exe"), "FirestickCleanup.exe"),
            from_disk(os.path.join(args.winform_dir, "FirestickCleanup.exe.config"), "FirestickCleanup.exe.config"),
            readme,
        ],
    }

    # Release builds are named ...-v1.4.0.zip; dev builds (version "dev-<sha>") get no "v".
    label = f"v{args.version}" if args.version[:1].isdigit() else args.version
    os.makedirs(args.out, exist_ok=True)
    for name, entries in packages.items():
        out = os.path.join(args.out, f"Firestick-Cleanup-{name}-{label}.zip")
        with zipfile.ZipFile(out, "w") as zf:
            for dest, mode, data in entries:
                zf.writestr(zip_info(dest, mode), data)
        print(f"{out}: {len(entries)} files")


if __name__ == "__main__":
    main()
