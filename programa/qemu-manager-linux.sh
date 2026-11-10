#!/usr/bin/env bash
# QEMU Manager — menu de terminal (Linux)
set -euo pipefail

CONFIG_DIR="${QEMU_MANAGER_CONFIGS:-$HOME/QEMU_Manager_Configs}"
WORK_DIR="${QEMU_MANAGER_WORK:-$HOME/vm}"
mkdir -p "$CONFIG_DIR" "$WORK_DIR"

need() { command -v "$1" >/dev/null 2>&1 || { echo "Comando não encontrado: $1"; return 1; }; }

list_vms() {
  echo
  echo "=== VMs em $CONFIG_DIR ==="
  local i=0
  shopt -s nullglob
  for f in "$CONFIG_DIR"/*.json; do
    i=$((i + 1))
    local name disk mark
    name="$(basename "$f" .json)"
    disk="$(python3 - "$f" <<'PY' || true
import json,sys
try:
    c=json.load(open(sys.argv[1],encoding="utf-8"))
    print(c.get("Disk") or c.get("disk") or "")
except Exception:
    print("")
PY
)"
    if [[ -n "$disk" && -f "$disk" ]]; then mark="OK"; else mark="SEM DISCO"; fi
    printf "  %2d) %s  [%s]\n" "$i" "$name" "$mark"
  done
  shopt -u nullglob
  if [[ "$i" -eq 0 ]]; then echo "  (nenhuma)"; fi
  echo
}

pick_json() {
  local files=()
  shopt -s nullglob
  files=("$CONFIG_DIR"/*.json)
  shopt -u nullglob
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "Nenhuma VM salva."
    return 1
  fi
  list_vms
  read -r -p "Número da VM: " n
  if [[ ! "$n" =~ ^[0-9]+$ ]] || [[ "$n" -lt 1 ]] || [[ "$n" -gt ${#files[@]} ]]; then
    echo "Escolha inválida."
    return 1
  fi
  echo "${files[$((n-1))]}"
}

json_get() {
  python3 - "$1" "$2" <<'PY'
import json,sys
c=json.load(open(sys.argv[1],encoding="utf-8"))
key=sys.argv[2]
for k in (key, key.lower(), key.replace("_","")):
    for ck,cv in c.items():
        if ck.lower().replace("_","")==k.lower().replace("_",""):
            if isinstance(cv,bool):
                print("true" if cv else "false")
            else:
                print(cv if cv is not None else "")
            raise SystemExit
print("")
PY
}

start_from_json() {
  local f="$1"
  need qemu-system-x86_64 || true
  local arch disk size iso ram cpus cpu machine diskif nic extra qemu
  arch="$(json_get "$f" Arch)"
  disk="$(json_get "$f" Disk)"
  size="$(json_get "$f" Size)"
  iso="$(json_get "$f" Iso)"
  ram="$(json_get "$f" Ram)"
  cpus="$(json_get "$f" Cpus)"
  cpu="$(json_get "$f" CpuModel)"
  machine="$(json_get "$f" Machine)"
  diskif="$(json_get "$f" DiskIf)"
  nic="$(json_get "$f" Nic)"
  extra="$(json_get "$f" Extra)"
  [[ -z "$ram" ]] && ram=4G
  [[ -z "$cpus" ]] && cpus=2
  [[ -z "$cpu" ]] && cpu=host
  [[ -z "$machine" ]] && machine=q35
  [[ -z "$diskif" ]] && diskif=virtio
  [[ -z "$nic" ]] && nic=virtio-net-pci
  [[ -z "$size" ]] && size=40G

  case "$arch" in
    *i386*) qemu=qemu-system-i386 ;;
    *aarch64*) qemu=qemu-system-aarch64 ;;
    *riscv64*) qemu=qemu-system-riscv64 ;;
    *riscv32*) qemu=qemu-system-riscv32 ;;
    *ppc64*) qemu=qemu-system-ppc64 ;;
    *ppc*) qemu=qemu-system-ppc ;;
    *) qemu=qemu-system-x86_64 ;;
  esac
  if ! command -v "$qemu" >/dev/null; then
    echo "Binário não encontrado: $qemu"
    return 1
  fi
  if [[ -z "$disk" ]]; then
    echo "Disco não definido no JSON."
    return 1
  fi
  if [[ ! -f "$disk" ]]; then
    echo "Criando disco $disk ($size)..."
    qemu-img create -f qcow2 "$disk" "$size"
  fi
  local args=(-machine "$machine" -m "$ram" -smp "$cpus" -cpu "$cpu"
              -drive "file=$disk,format=qcow2,if=$diskif"
              -nic "user,model=$nic" -display gtk -boot menu=on)
  if [[ -n "$iso" && -f "$iso" ]]; then
    args+=(-cdrom "$iso")
  fi
  # shellcheck disable=SC2206
  if [[ -n "$extra" ]]; then
    local extra_arr=($extra)
    args+=("${extra_arr[@]}")
  fi
  echo "Iniciando: $qemu ${args[*]}"
  exec "$qemu" "${args[@]}"
}

create_vm() {
  echo
  read -r -p "Nome da VM: " name
  [[ -z "$name" ]] && return
  read -r -p "Arquivo de disco [$WORK_DIR/${name}.qcow2]: " disk
  disk="${disk:-$WORK_DIR/${name}.qcow2}"
  read -r -p "Tamanho [40G]: " size
  size="${size:-40G}"
  read -r -p "RAM [4G]: " ram
  ram="${ram:-4G}"
  read -r -p "CPUs [2]: " cpus
  cpus="${cpus:-2}"
  read -r -p "ISO (opcional): " iso
  python3 - "$CONFIG_DIR/${name}.json" "$disk" "$size" "$ram" "$cpus" "$iso" <<'PY'
import json,sys
path,disk,size,ram,cpus,iso=sys.argv[1:7]
cfg={
  "Arch":"x86_64 (64-bit)","Disk":disk,"Size":size,"Iso":iso,
  "Ram":ram,"Cpus":cpus,"CpuModel":"host","Machine":"q35",
  "DiskIf":"virtio","Nic":"virtio-net-pci","Usb":True,"Rng":True,
  "Display":"gtk","Vga":"virtio","Boot":"menu","BootMenu":True,
  "NetMode":"user","Sound":"none","Extra":""
}
json.dump(cfg, open(path,"w",encoding="utf-8"), indent=2, ensure_ascii=False)
print("Salvo:", path)
PY
}

while true; do
  echo
  echo "==== QEMU Manager (Linux) ===="
  echo "1) Listar VMs"
  echo "2) Iniciar VM"
  echo "3) Criar VM"
  echo "4) Abrir pasta de configs"
  echo "0) Sair"
  read -r -p "Opção: " op
  case "$op" in
    1) list_vms ;;
    2)
      f="$(pick_json)" || continue
      start_from_json "$f"
      ;;
    3) create_vm ;;
    4) echo "$CONFIG_DIR"; command -v xdg-open >/dev/null && xdg-open "$CONFIG_DIR" || ls -l "$CONFIG_DIR" ;;
    0) exit 0 ;;
    *) echo "Opção inválida." ;;
  esac
done
