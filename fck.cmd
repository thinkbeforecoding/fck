@echo off
set encoding=utf-8

set dir=%~dp0
set cmd=%1
set script="%dir%\fck-cmd\fck-%cmd%.fsx"
set batch="%dir%\fck-cmd\fck-%cmd%.cmd"
shift

set "args="
:parse
if "%~1" neq "" (
  set args=%args% %1
  shift
  goto :parse
)
if defined args set args=%args:~1%


if not exist "%dir%\fck-cmd\packages" (
pushd "%dir%\fck-cmd\"
"%dir%\fck-cmd\.paket\paket.bootstrapper.exe" --run restore
popd 
)

if exist  "%script%" (
"%dir%/fck-cmd/packages/fake/tools/fake.exe" "%script%" -- %args%
) else if exist "%batch%" (
pushd "%dir%\fck-cmd\"
"%batch%" %cmd% %*
popd
) else (
"%dir%/fck-cmd/packages/fake/tools/fake.exe" "%dir%\fck-cmd\fck-help.fsx" -- %cmd% %*
)



