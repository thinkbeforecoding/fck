script=$(readlink -f "$0")
dir=$(dirname $script)

git pull
mono "$dir/.paket/paket.bootstrapper.exe" --run restore