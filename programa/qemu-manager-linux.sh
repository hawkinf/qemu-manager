#!/usr/bin/env bash
# QEMU Manager — menu de terminal (Linux)
# Compatível com o JSON da GUI C#/Python (PascalCase e snake_case).
set -u

VERSION="1.0.0"
CONFIG_DIR="${QEMU_MANAGER_CONFIGS:-$HOME/QEMU_Manager_Configs}"
WORK_DIR="${QEMU_MANAGER_WORK:-$HOME/vm}"

usage() {
  cat <<EOF
QEMU Manager Linux v${VERSION}

Uso:
  $(basename "$0")                 menu interativo
  $(basename "$0") --help
  $(basename "$0") --check         verifica dependências
  $(basename "$0") --list
  $(basename "$0") --start NOME    inicia a VM (JSON em $CONFIG_DIR)
  $(basename "$0") --print-args ARQUIVO.json
  $(basename "$0") --self-test

Variáveis:
  QEMU_MANAGER_CONFIGS   pasta dos JSON (padrão: ~/QEMU_Manager_Configs)
  QEMU_MANAGER_WORK      pasta de discos (padrão: ~/vm)
  QEMU_MANAGER_DRY_RUN=1 imprime o comando e não executa
EOF
}

need_python() {
  if ! command -v python3 >/dev/null 2>&1; then
    echo "python3 é obrigatório para ler/gravar as configurações JSON." >&2
    return 1
  fi
}

json_py() {
  # Programa Python na stdin (heredoc); argumentos depois de --
  python3 - "$@"
}

field() {
  local file="$1" key="$2" def="${3-}"
  json_py "$file" "$key" "$def" <<'PY'
import json, sys
path, key, default = sys.argv[1], sys.argv[2], sys.argv[3]
try:
    cfg = json.load(open(path, encoding="utf-8"))
except Exception as e:
    sys.stderr.write(f"JSON inválido ({path}): {e}\n")
    sys.exit(2)
idx = {k.lower().replace("_", ""): v for k, v in cfg.items()}
want = key.lower().replace("_", "")
val = idx.get(want, default)
if val is None:
    val = default
if isinstance(val, bool):
    print("true" if val else "false")
else:
    print(val)
PY
}

is_true() {
  case "${1,,}" in
    1|true|yes|on) return 0 ;;
    *) return 1 ;;
  esac
}

qemu_bin_for_arch() {
  local arch="$1" custom="$2"
  if [[ "$arch" == "Personalizado" && -n "$custom" ]]; then
    printf '%s\n' "$custom"
    return
  fi
  case "$arch" in
    *i386*) echo qemu-system-i386 ;;
    *aarch64*|*ARM\ 64*) echo qemu-system-aarch64 ;;
    *ARM\ 32*|*arm\ \(ARM\ 32*) echo qemu-system-arm ;;
    *riscv64*) echo qemu-system-riscv64 ;;
    *riscv32*) echo qemu-system-riscv32 ;;
    *ppc64*) echo qemu-system-ppc64 ;;
    *ppc*) echo qemu-system-ppc ;;
    *mips64el*) echo qemu-system-mips64el ;;
    *mipsel*) echo qemu-system-mipsel ;;
    *sparc64*) echo qemu-system-sparc64 ;;
    *sparc*) echo qemu-system-sparc ;;
    *s390x*) echo qemu-system-s390x ;;
    *) echo qemu-system-x86_64 ;;
  esac
}

accel_for() {
  if [[ -e /dev/kvm && -r /dev/kvm ]]; then
    echo kvm
  else
    echo tcg
  fi
}

is_windows_path() {
  [[ "$1" =~ ^[A-Za-z]:[\\/] ]] || [[ "$1" == *\\* ]]
}

resolve_path() {
  local p="$1"
  [[ -z "$p" ]] && return
  if [[ "$p" == /* ]]; then
    printf '%s\n' "$p"
  elif is_windows_path "$p"; then
    printf '%s\n' "$p"
  else
    printf '%s\n' "$WORK_DIR/$p"
  fi
}

print_args_from_json() {
  local file="$1"
  [[ -f "$file" ]] || { echo "Arquivo não encontrado: $file" >&2; return 1; }
  local accel
  accel="$(accel_for)"
  json_py "$file" "$WORK_DIR" "$accel" <<'PY'
import json, os, shlex, sys

path, work, accel = sys.argv[1], sys.argv[2], sys.argv[3]
cfg = json.load(open(path, encoding="utf-8"))
idx = {k.lower().replace("_", ""): v for k, v in cfg.items()}

def get(*names, default=""):
    for n in names:
        k = n.lower().replace("_", "")
        if k in idx and idx[k] not in (None, ""):
            return idx[k]
    return default

def flag(*names, default=False):
    v = get(*names, default=None)
    if v is None or v == "":
        return default
    if isinstance(v, bool):
        return v
    return str(v).lower() in ("1", "true", "yes", "on")

def rooted(p):
    if not p:
        return ""
    if os.path.isabs(p) or (len(p) > 1 and p[1] == ":"):
        return p
    return os.path.join(work, p)

arch = str(get("Arch", default="x86_64 (64-bit)"))
custom = str(get("CustomQemu", "custom_qemu"))
disk = rooted(str(get("Disk", "disk")))
size = str(get("Size", "size", default="40G")) or "40G"
iso = rooted(str(get("Iso", "iso")))
virtio = rooted(str(get("Virtio", "virtio")))
ram = str(get("Ram", default="4G")) or "4G"
cpus = str(get("Cpus", default="2")) or "2"
cpu = str(get("CpuModel", "cpu_model", default="host")) or "host"
machine = str(get("Machine", default="q35")) or "q35"
diskif = str(get("DiskIf", "disk_if", default="virtio")) or "virtio"
nic = str(get("Nic", default="virtio-net-pci")) or "virtio-net-pci"
display = str(get("Display", default="gtk")) or "gtk"
sound = str(get("Sound", default="none")) or "none"
vga = str(get("Vga", default="std")) or "std"
bios = rooted(str(get("Bios", "bios")))
pflash = rooted(str(get("Pflash", "pflash")))
share = str(get("ShareHost", "share_host"))
share_name = str(get("ShareName", "share_name", default="hostshare")) or "hostshare"
extra_disk = rooted(str(get("ExtraDisk", "extra_disk")))
extra_diskif = str(get("ExtraDiskIf", "extra_diskif", default="virtio")) or "virtio"
snapshot = str(get("Snapshot", "snapshot"))
kernel = rooted(str(get("Kernel", "kernel")))
initrd = rooted(str(get("Initrd", "initrd")))
append = str(get("Append", "append"))
keyboard = str(get("Keyboard", default="en-us")) or "en-us"
net_mode = str(get("NetMode", "net_mode", default="user")) or "user"
hostfwd = str(get("HostFwd", "hostfwd"))
extra = str(get("Extra", "extra"))
boot = str(get("Boot", default="menu")) or "menu"
boot_custom = str(get("BootCustom", "boot_custom", default="ncd")) or "ncd"
boot_menu = flag("BootMenu", "boot_menu", default=True)
gl = flag("Gl", "gl")
usb = flag("Usb", "usb", default=True)
rng = flag("Rng", "rng", default=True)
balloon = flag("Balloon", "balloon")
tpm = flag("Tpm", "tpm")

def qemu_for(a, custom_bin):
    if a == "Personalizado" and custom_bin:
        return custom_bin
    table = (
        ("i386", "qemu-system-i386"),
        ("aarch64", "qemu-system-aarch64"),
        ("ARM 64", "qemu-system-aarch64"),
        ("ARM 32", "qemu-system-arm"),
        ("riscv64", "qemu-system-riscv64"),
        ("riscv32", "qemu-system-riscv32"),
        ("ppc64", "qemu-system-ppc64"),
        ("ppc", "qemu-system-ppc"),
        ("mips64el", "qemu-system-mips64el"),
        ("mipsel", "qemu-system-mipsel"),
        ("sparc64", "qemu-system-sparc64"),
        ("sparc", "qemu-system-sparc"),
        ("s390x", "qemu-system-s390x"),
    )
    for needle, bin in table:
        if needle.lower() in a.lower():
            return bin
    return "qemu-system-x86_64"

qemu = qemu_for(arch, custom)
is_x86 = ("x86" in arch.lower()) or ("i386" in arch.lower())
if cpu == "host" and accel != "kvm":
    cpu = "max" if "64" in arch or "x86_64" in arch.lower() else "qemu32"

args = [qemu]
if is_x86:
    args += ["-accel", accel]
    m = machine if "accel=" in machine else f"{machine},accel={accel}"
    args += ["-machine", m]
else:
    args += ["-machine", machine]

args += ["-m", ram, "-smp", cpus, "-cpu", cpu]
if diskif != "none" and disk:
    args += ["-drive", f"file={disk},format=qcow2,if={diskif}"]
if extra_disk:
    args += ["-drive", f"file={extra_disk},format=qcow2,if={extra_diskif}"]
if usb:
    args += ["-usb", "-device", "usb-tablet", "-device", "usb-kbd"]
disp = "gtk,gl=on" if gl and display == "gtk" else display
args += ["-display", disp]
if vga != "none":
    args += ["-vga", vga]
if sound != "none":
    if sound == "hda":
        args += ["-device", "intel-hda", "-device", "hda-duplex"]
    else:
        args += ["-device", sound]
if net_mode == "user" and nic != "none":
    n = f"user,model={nic}"
    if hostfwd:
        n += f",hostfwd={hostfwd}"
    args += ["-nic", n]
else:
    args += ["-nic", "none"]
if iso:
    args += ["-cdrom", iso]
if virtio and is_x86:
    args += ["-drive", f"file={virtio},media=cdrom,index=1"]
if bios:
    args += ["-bios", bios]
if pflash:
    args += ["-drive", f"if=pflash,format=raw,file={pflash}"]
if share:
    args += ["-fsdev", f"local,id=fs0,path={share},security_model=none",
             "-device", f"virtio-9p-pci,fsdev=fs0,mount_tag={share_name}"]
if snapshot:
    args += ["-loadvm", snapshot]
if kernel:
    args += ["-kernel", kernel]
if initrd:
    args += ["-initrd", initrd]
if append:
    args += ["-append", append]
if rng:
    args += ["-device", "virtio-rng-pci"]
if balloon:
    args += ["-device", "virtio-balloon-pci"]
if tpm:
    args += ["-tpmdev", "emulator,id=tpm0,chardev=chrtpm",
             "-chardev", "socket,id=chrtpm,path=./swtpm-sock",
             "-device", "tpm-tis,tpmdev=tpm0"]
if keyboard.lower() != "en-us":
    args += ["-k", keyboard]

if boot == "menu":
    args += ["-boot", "menu=on"]
elif boot.startswith("once_"):
    args += ["-boot", f"once={boot.split('_', 1)[1]}"]
else:
    order = boot_custom if boot == "custom" else boot
    if order and order not in ("menu", "custom"):
        s = f"order={order}"
        if boot_menu:
            s += ",menu=on"
        args += ["-boot", s]
    elif boot_menu:
        args += ["-boot", "menu=on"]

if extra:
    args += shlex.split(extra)

meta = {
    "disk": disk,
    "size": size,
    "iso": iso,
    "bios": bios,
    "pflash": pflash,
    "kernel": kernel,
    "initrd": initrd,
    "extra_disk": extra_disk,
    "share": share,
}
# linha 1 = binário; demais = args; última = META json
print(args[0])
for a in args[1:]:
    print(a)
print("---META---")
json.dump(meta, sys.stdout, ensure_ascii=False)
print()
PY
}

load_cmd_arrays() {
  # Preenche QEMU_BIN e QEMU_ARGS[@] a partir do JSON. META_* com caminhos.
  local file="$1"
  local lines=()
  mapfile -t lines < <(print_args_from_json "$file") || return 1
  local meta_i=-1
  local i
  for i in "${!lines[@]}"; do
    if [[ "${lines[$i]}" == "---META---" ]]; then
      meta_i=$i
      break
    fi
  done
  if [[ $meta_i -lt 1 ]]; then
    echo "Falha ao montar argumentos." >&2
    return 1
  fi
  QEMU_BIN="${lines[0]}"
  QEMU_ARGS=("${lines[@]:1:$((meta_i - 1))}")
  META_JSON="${lines[$((meta_i + 1))]-}"
}

check_deps() {
  echo "=== Dependências ==="
  local ok=0
  for c in python3 qemu-img; do
    if command -v "$c" >/dev/null 2>&1; then
      echo "  OK  $c  ($(command -v "$c"))"
    else
      echo "  FALTA  $c"
      ok=1
    fi
  done
  if command -v qemu-system-x86_64 >/dev/null 2>&1; then
    echo "  OK  qemu-system-x86_64"
  else
    echo "  FALTA  qemu-system-x86_64  (instale o pacote qemu-system)"
    ok=1
  fi
  if [[ -e /dev/kvm && -r /dev/kvm ]]; then
    echo "  OK  KVM (/dev/kvm)"
  else
    echo "  AVISO  KVM indisponível — a VM usa TCG (mais lenta)"
  fi
  echo "  configs: $CONFIG_DIR"
  echo "  discos:  $WORK_DIR"
  return "$ok"
}

list_vms() {
  echo
  echo "=== VMs em $CONFIG_DIR ==="
  local i=0 f name disk mark
  shopt -s nullglob
  for f in "$CONFIG_DIR"/*.json; do
    i=$((i + 1))
    name="$(basename "$f" .json)"
    disk="$(resolve_path "$(field "$f" Disk "")")"
    if [[ -n "$disk" && -f "$disk" ]]; then
      mark="disco OK"
    elif [[ -n "$disk" ]] && is_windows_path "$disk"; then
      mark="caminho Windows — ajuste o Disco"
    elif [[ -n "$disk" ]]; then
      mark="sem disco (será criado)"
    else
      mark="disco vazio"
    fi
    printf "  %2d) %s  [%s]\n" "$i" "$name" "$mark"
  done
  shopt -u nullglob
  if [[ "$i" -eq 0 ]]; then
    echo "  (nenhuma)"
  fi
  echo
}

json_files() {
  shopt -s nullglob
  local files=("$CONFIG_DIR"/*.json)
  shopt -u nullglob
  printf '%s\n' "${files[@]}"
}

pick_json() {
  local files=()
  mapfile -t files < <(json_files)
  if [[ ${#files[@]} -eq 0 || -z "${files[0]-}" ]]; then
    echo "Nenhuma VM salva em $CONFIG_DIR" >&2
    return 1
  fi
  list_vms >&2
  local n
  read -r -p "Número da VM: " n
  if [[ ! "${n:-}" =~ ^[0-9]+$ ]] || [[ "$n" -lt 1 ]] || [[ "$n" -gt ${#files[@]} ]]; then
    echo "Escolha inválida." >&2
    return 1
  fi
  printf '%s\n' "${files[$((n - 1))]}"
}

ensure_disk() {
  local disk="$1" size="$2"
  if [[ -z "$disk" ]]; then
    echo "Disco obrigatório na configuração." >&2
    return 1
  fi
  if is_windows_path "$disk"; then
    echo "O caminho do disco é do Windows: $disk" >&2
    echo "Edite o JSON e use um caminho Linux (ex: $WORK_DIR/win10.qcow2)." >&2
    return 1
  fi
  if [[ -f "$disk" ]]; then
    return 0
  fi
  if ! command -v qemu-img >/dev/null 2>&1; then
    echo "Disco inexistente e qemu-img não está instalado." >&2
    return 1
  fi
  mkdir -p "$(dirname "$disk")"
  echo "Criando disco $disk ($size)..."
  qemu-img create -f qcow2 "$disk" "$size"
}

warn_if_missing() {
  local label="$1" path="$2" required="${3:-0}"
  [[ -z "$path" ]] && return 0
  if [[ -e "$path" ]]; then
    return 0
  fi
  if [[ "$required" == "1" ]]; then
    echo "$label não encontrado: $path" >&2
    return 1
  fi
  echo "Aviso: $label não encontrado ($path) — ignorado." >&2
}

start_from_json() {
  local file="$1"
  need_python || return 1
  load_cmd_arrays "$file" || return 1

  eval "$(json_py "$META_JSON" <<'PY'
import json, sys, shlex
m = json.loads(sys.argv[1])
for k in ("disk","size","iso","bios","pflash","kernel","initrd","extra_disk","share"):
    print(f"{k.upper()}={shlex.quote(str(m.get(k, '') or ''))}")
PY
)"

  if [[ "${QEMU_MANAGER_DRY_RUN:-0}" == "1" ]]; then
    printf 'DRY-RUN: %q' "$QEMU_BIN"
    local a
    for a in "${QEMU_ARGS[@]}"; do
      printf ' %q' "$a"
    done
    printf '\n'
    return 0
  fi

  ensure_disk "$DISK" "${SIZE:-40G}" || return 1
  warn_if_missing "ISO" "$ISO" 0 || true
  warn_if_missing "BIOS/OVMF" "$BIOS" 1 || return 1
  warn_if_missing "pflash" "$PFLASH" 1 || return 1
  warn_if_missing "kernel" "$KERNEL" 1 || return 1
  warn_if_missing "initrd" "$INITRD" 1 || return 1
  warn_if_missing "disco extra" "$EXTRA_DISK" 1 || return 1
  if [[ -n "$SHARE" && ! -d "$SHARE" ]]; then
    echo "Pasta compartilhada não encontrada: $SHARE" >&2
    return 1
  fi

  if ! command -v "$QEMU_BIN" >/dev/null 2>&1; then
    echo "QEMU não encontrado: $QEMU_BIN" >&2
    echo "Instale o pacote correspondente (ex: qemu-system-x86)." >&2
    return 1
  fi

  echo "Iniciando: $QEMU_BIN ${QEMU_ARGS[*]}"
  mkdir -p "$WORK_DIR"
  ( cd "$WORK_DIR" && "$QEMU_BIN" "${QEMU_ARGS[@]}" )
}

save_preset_json() {
  json_py "$@" <<'PY'
import json, sys
path = sys.argv[1]
cfg = json.loads(sys.argv[2])
json.dump(cfg, open(path, "w", encoding="utf-8"), indent=2, ensure_ascii=False)
print("Salvo:", path)
PY
}

PRESET_JSON='{
  "Windows 11 x64": {"Arch":"x86_64 (64-bit)","Disk":"win11.qcow2","Size":"80G","Ram":"8G","Cpus":"4","CpuModel":"host","Machine":"q35","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"std","Usb":true,"Rng":true},
  "Windows 10 x64": {"Arch":"x86_64 (64-bit)","Disk":"win10.qcow2","Size":"80G","Ram":"8G","Cpus":"4","CpuModel":"host","Machine":"q35","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"std","Usb":true,"Rng":true},
  "Ubuntu 24.04 x64": {"Arch":"x86_64 (64-bit)","Disk":"ubuntu.qcow2","Size":"40G","Ram":"4G","Cpus":"4","CpuModel":"host","Machine":"q35","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"virtio","Gl":true,"Usb":true,"Rng":true},
  "Debian x64": {"Arch":"x86_64 (64-bit)","Disk":"debian.qcow2","Size":"30G","Ram":"2G","Cpus":"2","CpuModel":"host","Machine":"q35","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"virtio","Gl":true,"Usb":true,"Rng":true},
  "Alpine / Minimo": {"Arch":"x86_64 (64-bit)","Disk":"alpine.qcow2","Size":"10G","Ram":"1G","Cpus":"1","CpuModel":"host","Machine":"q35","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"virtio","Usb":true},
  "RISC-V 64": {"Arch":"riscv64","Disk":"riscv.qcow2","Size":"20G","Ram":"2G","Cpus":"2","CpuModel":"rv64","Machine":"virt","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"virtio","Usb":true},
  "Ubuntu ARM64": {"Arch":"aarch64 (ARM 64-bit)","Disk":"ubuntu-arm.qcow2","Size":"40G","Ram":"4G","Cpus":"4","CpuModel":"cortex-a72","Machine":"virt","DiskIf":"virtio","Nic":"virtio-net-pci","Vga":"virtio","Usb":true,"Rng":true}
}'

create_vm() {
  need_python || return 1
  mkdir -p "$CONFIG_DIR" "$WORK_DIR"
  echo
  echo "Perfis:"
  json_py "$PRESET_JSON" <<'PY'
import json,sys
presets=json.loads(sys.argv[1])
for i,name in enumerate(presets,1):
    print(f"  {i}) {name}")
print("  0) Personalizado")
PY
  local op name
  read -r -p "Perfil: " op
  local cfg
  if [[ "$op" == "0" ]]; then
    read -r -p "Nome da VM: " name
    [[ -z "${name:-}" ]] && return
    cfg="$(json_py "$name" "$WORK_DIR" <<'PY'
import json,sys
name, work = sys.argv[1], sys.argv[2]
print(json.dumps({
  "Arch":"x86_64 (64-bit)","Disk":f"{work}/{name}.qcow2","Size":"40G",
  "Iso":"","Ram":"4G","Cpus":"2","CpuModel":"host","Machine":"q35",
  "DiskIf":"virtio","Nic":"virtio-net-pci","Usb":True,"Rng":True,
  "Display":"gtk","Vga":"virtio","Boot":"menu","BootMenu":True,
  "NetMode":"user","Sound":"none","Extra":""
}))
PY
)"
  else
    cfg="$(json_py "$PRESET_JSON" "$op" "$WORK_DIR" <<'PY'
import json,sys
presets=json.loads(sys.argv[1])
names=list(presets)
try:
    i=int(sys.argv[2])
except Exception:
    sys.exit(1)
if i<1 or i>len(names):
    sys.exit(1)
name=names[i-1]
cfg=presets[name]
disk=cfg.get("Disk","vm.qcow2")
if not disk.startswith("/"):
    cfg["Disk"]=sys.argv[3].rstrip("/")+"/"+disk
cfg.setdefault("Display","gtk")
cfg.setdefault("Boot","menu")
cfg.setdefault("BootMenu", True)
cfg.setdefault("NetMode","user")
cfg.setdefault("Sound","none")
print(name)
print(json.dumps(cfg))
PY
)" || { echo "Perfil inválido."; return 1; }
    name="$(printf '%s\n' "$cfg" | head -n1)"
    cfg="$(printf '%s\n' "$cfg" | tail -n +2)"
  fi
  read -r -p "ISO de instalação (opcional): " iso
  if [[ -n "${iso:-}" ]]; then
    cfg="$(json_py "$cfg" "$iso" <<'PY'
import json,sys
c=json.loads(sys.argv[1]); c["Iso"]=sys.argv[2]; print(json.dumps(c))
PY
)"
  fi
  save_preset_json "$CONFIG_DIR/${name}.json" "$cfg"
}

edit_vm() {
  local f
  f="$(pick_json)" || return 1
  echo "Arquivo: $f"
  python3 -m json.tool "$f"
  echo
  echo "Campos: Disk Iso Ram Cpus Extra HostFwd Bios Kernel Snapshot"
  local key val
  read -r -p "Campo para alterar (vazio = cancelar): " key
  [[ -z "${key:-}" ]] && return
  read -r -p "Novo valor: " val
  json_py "$f" "$key" "$val" <<'PY'
import json,sys
path,key,val=sys.argv[1],sys.argv[2],sys.argv[3]
c=json.load(open(path,encoding="utf-8"))
# preserva o nome original da chave se já existir
found=None
for k in list(c):
    if k.lower().replace("_","")==key.lower().replace("_",""):
        found=k
        break
c[found or key]=val
json.dump(c, open(path,"w",encoding="utf-8"), indent=2, ensure_ascii=False)
print("Atualizado", found or key)
PY
}

delete_vm() {
  local f name
  f="$(pick_json)" || return 1
  name="$(basename "$f" .json)"
  read -r -p "Excluir a configuração '$name'? O disco permanece. [s/N] " a
  [[ "${a:-}" == "s" || "${a:-}" == "S" ]] || return
  rm -f "$f"
  echo "Removido $f"
}

list_snapshots() {
  local f disk
  f="$(pick_json)" || return 1
  disk="$(resolve_path "$(field "$f" Disk "")")"
  if [[ -z "$disk" || ! -f "$disk" ]]; then
    echo "Disco não encontrado: $disk"
    return 1
  fi
  if ! command -v qemu-img >/dev/null 2>&1; then
    echo "qemu-img não instalado."
    return 1
  fi
  qemu-img snapshot -l "$disk"
}

open_folder() {
  mkdir -p "$CONFIG_DIR"
  echo "$CONFIG_DIR"
  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$CONFIG_DIR" >/dev/null 2>&1 || true
  fi
  ls -l "$CONFIG_DIR"
}

start_by_name() {
  local name="$1"
  local f="$CONFIG_DIR/${name}.json"
  if [[ ! -f "$f" ]]; then
    echo "Não achei $f" >&2
    return 1
  fi
  start_from_json "$f"
}

self_test() {
  need_python || return 1
  local dir
  dir="$(mktemp -d /tmp/qemu-manager-test.XXXXXX)"

  cat >"$dir/win.json" <<EOF
{
  "Arch": "x86_64 (64-bit)",
  "Disk": "$dir/win.qcow2",
  "Size": "20G",
  "Ram": "4G",
  "Cpus": "2",
  "CpuModel": "host",
  "Machine": "q35",
  "DiskIf": "virtio",
  "Nic": "virtio-net-pci",
  "Usb": true,
  "Rng": true,
  "Gl": false,
  "Display": "gtk",
  "Vga": "std",
  "Boot": "menu",
  "BootMenu": true,
  "NetMode": "user",
  "HostFwd": "tcp::2222-:22",
  "Sound": "hda",
  "Extra": "-smbios type=2"
}
EOF
  cat >"$dir/snake.json" <<EOF
{
  "arch": "aarch64 (ARM 64-bit)",
  "disk": "$dir/arm.qcow2",
  "cpu_model": "cortex-a72",
  "machine": "virt",
  "disk_if": "virtio",
  "nic": "virtio-net-pci",
  "ram": "2G",
  "cpus": "2",
  "usb": true,
  "boot": "dc",
  "boot_menu": true,
  "display": "gtk",
  "vga": "virtio",
  "net_mode": "user",
  "extra": "-serial mon:stdio"
}
EOF
  cat >"$dir/quoted.json" <<EOF
{
  "Arch": "x86_64 (64-bit)",
  "Disk": "$dir/q.qcow2",
  "Ram": "1G",
  "Cpus": "1",
  "CpuModel": "qemu64",
  "Machine": "pc",
  "DiskIf": "ide",
  "Nic": "e1000",
  "Usb": false,
  "Rng": false,
  "Display": "gtk",
  "Vga": "cirrus",
  "Boot": "custom",
  "BootCustom": "ncd",
  "NetMode": "none",
  "Extra": "-smbios type=2 -uuid 12345678-1234-1234-1234-123456789abc"
}
EOF

  local fail=0
  assert_contains() {
    local haystack="$1" needle="$2" msg="$3"
    if [[ "$haystack" == *"$needle"* ]]; then
      echo "  OK  $msg"
    else
      echo "  FALHOU  $msg"
      echo "    esperado: $needle"
      fail=1
    fi
  }

  echo "=== self-test ==="
  local out
  out="$(print_args_from_json "$dir/win.json")"
  assert_contains "$out" "qemu-system-x86_64" "binário x86_64"
  assert_contains "$out" "-m" "RAM"
  assert_contains "$out" "4G" "valor RAM"
  assert_contains "$out" "hostfwd=tcp::2222-:22" "hostfwd"
  assert_contains "$out" "intel-hda" "som hda"
  assert_contains "$out" "virtio-rng-pci" "rng"
  assert_contains "$out" "usb-tablet" "usb"
  assert_contains "$out" "-smbios" "extra args"
  assert_contains "$out" "menu=on" "boot menu"

  out="$(print_args_from_json "$dir/snake.json")"
  assert_contains "$out" "qemu-system-aarch64" "arch snake_case aarch64"
  assert_contains "$out" "cortex-a72" "cpu_model snake_case"
  assert_contains "$out" "order=dc,menu=on" "boot dc"
  assert_contains "$out" "-serial" "extra serial"

  out="$(print_args_from_json "$dir/quoted.json")"
  assert_contains "$out" "qemu-system-x86_64" "quoted extra bin"
  assert_contains "$out" "-uuid" "extra com vários tokens"
  assert_contains "$out" "12345678-1234-1234-1234-123456789abc" "uuid preservado"
  assert_contains "$out" "order=ncd,menu=on" "boot custom"
  assert_contains "$out" "-nic" "nic none vira -nic none"

  # pick_json não deve vazar a lista no stdout: simulamos via json_files
  mkdir -p "$dir/cfgs"
  cp "$dir/win.json" "$dir/cfgs/alpha.json"
  local listed
  listed="$(CONFIG_DIR="$dir/cfgs" bash -c 'shopt -s nullglob; printf "%s\n" "$CONFIG_DIR"/*.json')"
  assert_contains "$listed" "alpha.json" "lista de json"

  # DRY-RUN não exige qemu instalado
  if QEMU_MANAGER_DRY_RUN=1 start_from_json "$dir/win.json" >/tmp/qm-dry.out 2>/tmp/qm-dry.err; then
    echo "  OK  dry-run executou"
  else
    echo "  FALHOU  dry-run"
    cat /tmp/qm-dry.err
    fail=1
  fi

  rm -rf "$dir"
  if [[ "$fail" -eq 0 ]]; then
    echo "self-test: PASSOU"
    return 0
  fi
  echo "self-test: FALHOU"
  return 1
}

menu() {
  mkdir -p "$CONFIG_DIR" "$WORK_DIR"
  need_python || exit 1
  while true; do
    echo
    echo "==== QEMU Manager (Linux) v${VERSION} ===="
    echo "1) Listar VMs"
    echo "2) Iniciar VM"
    echo "3) Criar VM (perfil)"
    echo "4) Editar campo"
    echo "5) Excluir configuração"
    echo "6) Snapshots do disco"
    echo "7) Checar dependências"
    echo "8) Abrir pasta de configs"
    echo "0) Sair"
    local op
    read -r -p "Opção: " op || exit 0
    case "${op:-}" in
      1) list_vms ;;
      2)
        local f
        f="$(pick_json)" || continue
        start_from_json "$f" || true
        ;;
      3) create_vm || true ;;
      4) edit_vm || true ;;
      5) delete_vm || true ;;
      6) list_snapshots || true ;;
      7) check_deps || true ;;
      8) open_folder ;;
      0) exit 0 ;;
      *) echo "Opção inválida." ;;
    esac
  done
}

# --- META parse helpers used by start_from_json ---
# json_py with a JSON string as argv[1] for META:
# already handled.

main() {
  mkdir -p "$CONFIG_DIR" "$WORK_DIR"
  case "${1:-}" in
    "" ) menu ;;
    -h|--help) usage ;;
    --check) check_deps ;;
    --list) list_vms ;;
    --start)
      [[ -n "${2:-}" ]] || { echo "Informe o nome da VM."; exit 1; }
      start_by_name "$2"
      ;;
    --print-args)
      [[ -n "${2:-}" ]] || { echo "Informe o JSON."; exit 1; }
      print_args_from_json "$2"
      ;;
    --self-test) self_test ;;
    *) usage; exit 1 ;;
  esac
}

main "$@"
