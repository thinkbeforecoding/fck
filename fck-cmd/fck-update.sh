script=$(readlink -f "$0")
dir=$(dirname $script)

pushd "$dir" > /dev/null
git pull
mono "$dir/.paket/paket.bootstrapper.exe" --run restore
popd > /dev/null
