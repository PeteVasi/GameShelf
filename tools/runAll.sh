#!/bin/bash

# Abort the script if any command in it returns a non-zero status code
# http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

# Change to one level up from /tools
cd `dirname $0`
cd ..

OUTDIR=`pwd`/docs

dotnet build GameShelf

dotnet GameShelf/bin/Debug/netcoreapp3.1/GameShelf.dll -u PeteVasi > ${OUTDIR}/PeteVasi.html

