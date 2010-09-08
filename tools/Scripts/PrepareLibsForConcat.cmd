@echo off

REM 
REM This script is used to extract a list of libraries under a .\tmp directory
REM

set PREVCD=%CD%

for %%i IN (%*) do (
  IF "%%i"=="%1" (
    IF NOT EXIST %1\tmp mkdir %1\tmp
    cd %1\tmp
  ) ELSE (
    IF "%GNU_TARGET%"=="arm-elf" (
        %GNU_TOOLS_BIN%\ar -x ..\%%i
    ) ELSE (
        %GNU_TOOLS_BIN%\%GNU_TARGET%-ar -x ..\%%i
    )
  )
)

cd %PREVCD%
set PREVCD=