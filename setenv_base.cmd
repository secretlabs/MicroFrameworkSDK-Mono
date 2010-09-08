@echo off

set PORT_BUILD=
set NO_ADS_WRAPPER=
SET COMPILER_TOOL=

if NOT "%1"=="" GOTO :ARGSOK

:ERROR
@echo.
@echo Error: Invalid Arguments!
@echo.
@echo Usage: setenv_base COMPILER_TOOL_VERSION
@echo     where COMPILER_TOOL_VERSION is ADI5.0, GCC4.2, ADS1.2, RVDS3.0, RVDS3.1, MDK3.1, MDK3.80a, SHC9.2, VS9, VS10
@echo.
GOTO :EOF


:ARGSOK

SET COMPILER_TOOL_VERSION=%1
SET ARG3=%3
SET TFSCONFIG=MFConfig.xml

if /I "%2"=="PORT"     set PORT_BUILD=1
if /I "%2"=="PORT"     set NO_ADS_WRAPPER=1

set CURRENTCD=%CD%

CALL %~dp0\tools\scripts\init.cmd

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

set SPOCLIENT=%CLRROOT%
pushd %SPOCLIENT%\..
set SPOROOT=%CD%
popd

set NetMfTargetsBaseDir=%SPOCLIENT%\Framework\IDE\Targets\

set _SDROOT_=%SPOROOT:current=%
if "%_SDROOT_:~-1%" == "\" set _SDROOT_=%_SDROOT_:~0,-1%


rem @ make sure we start with a clean path
if "%DOTNETMF_OLD_PATH%"=="" (
goto :save_current_path
) else (
goto :restore_path_from_old
)

:save_current_path
set DOTNETMF_OLD_PATH=%PATH%
goto :after_path_saved_or_restored


:restore_path_from_old
set PATH=%DOTNETMF_OLD_PATH%

:after_path_saved_or_restored

set PATH=%SPOROOT%\tools\NUnit;%SPOROOT%\tools\SDPack;%SPOROOT%\bin;%PATH%
set PATH=%SPOROOT%\tools\x86\perl\bin;%SPOROOT%\tools\x86\bin;%CLRROOT%\tools\bin;%PATH%;%CLRROOT%\tools\scripts
set PATH=%CLRROOT%\BuildOutput\Public\%FLAVOR_WIN%\Test\Server\dll;%PATH%

cd %CURRENTCD%

set CURRENTCD=

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
rem set tool-chains variables 

IF /I "%COMPILER_TOOL_VERSION%" NEQ "VS9" (
IF /I "%COMPILER_TOOL_VERSION%" NEQ "VS10" (
IF NOT "%VS100COMNTOOLS%" == "" (
    CALL "%VS100COMNTOOLS%vsvars32.bat"
) ELSE (
    CALL "%VS90COMNTOOLS%vsvars32.bat"    
)))

IF /I "%COMPILER_TOOL_VERSION%"=="VS9"      CALL :SET_VS9_VARS 
IF /I "%COMPILER_TOOL_VERSION%"=="VS10"     CALL :SET_VS10_VARS 

IF /I "%COMPILER_TOOL_VERSION%"=="ADI5.0"   CALL :SET_BLACKFIN_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="GCC4.2"   CALL :SET_GCC_VARS 
IF /I "%COMPILER_TOOL_VERSION%"=="MDK3.1"   CALL :SET_MDK_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="MDK3.80a" CALL :SET_MDK_VARS

IF /I "%COMPILER_TOOL_VERSION%"=="SHC9.2"   CALL :SET_SHC_VARS 
IF /I "%COMPILER_TOOL_VERSION%"=="GCCOP4.2" CALL :SET_GCC_VARS 

IF "%PORT_BUILD%"=="" (
IF /I "%COMPILER_TOOL_VERSION%"=="ADS1.2"   CALL :SET_RVDS_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="RVDS3.0"  CALL :SET_RVDS_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="RVDS3.1"  CALL :SET_RVDS_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="RVDS4.0"  CALL :SET_RVDS_VARS
) ELSE (
IF /I "%COMPILER_TOOL_VERSION%"=="ADS1.2"   CALL :SET_RVDS_V12_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="RVDS3.0"  CALL :SET_RVDS_V30_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="RVDS3.1"  CALL :SET_RVDS_V31_VARS
IF /I "%COMPILER_TOOL_VERSION%"=="RVDS4.0"  CALL :SET_RVDS_V40_VARS
)


IF "%COMPILER_TOOL%"=="" GOTO :ERROR

set TINYCLR_USE_MSBUILD=1   

Title MF (%FLAVOR_WIN%) (%COMPILER_TOOL_VERSION%) %SPOCLIENT%

GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_RVDS_VARS
REM dotnetmf team internal setting
@echo setting source depot RVDS vars
SET COMPILER_TOOL=RVDS
set DOTNETMF_COMPILER=RVDS3.1
if /I "%COMPILER_TOOL_VERSION%"=="RVDS4.0" set DOTNETMF_COMPILER=RVDS4.0

set RVDS_EXT=
if /I "%COMPILER_TOOL_VERSION%"=="RVDS3.0" set RVDS_EXT=_v3_0
if /I "%COMPILER_TOOL_VERSION%"=="RVDS3.1" set RVDS_EXT=_v3_1
if /I "%COMPILER_TOOL_VERSION%"=="RVDS4.0" set RVDS_EXT=_v4_0
set PATH=%CLRROOT%\tools\ads%RVDS_EXT%\bin;%PATH%
set ARMROOT=%SPOCLIENT%\tools\ads%RVDS_EXT%
set ARMHOME=%ARMROOT%
set ARMLMD_LICENSE_FILE=%ARMROOT%\licenses\license.dat
set ADS_TOOLS=%ARMROOT%
set ARMCONF=%ARMROOT%\BIN
set ARMINC=%ARMROOT%\INCLUDE
set ARMDLL=%ARMROOT%\BIN
set ARMLIB=%ARMROOT%\LIB
set ARMBIN=%ARMROOT%\BIN
set RVCT30BIN=%ARMBIN%
set RVCT30INC=%ARMINC%
set RVCT30LIB=%ARMLIB%
set RVCT31BIN=%ARMBIN%
set RVCT31INC=%ARMINC%
set RVCT31LIB=%ARMLIB%
set RVCT40BIN=%ARMBIN%
set RVCT40INC=%ARMINC%
set RVCT40LIB=%ARMLIB%
set ADS_TOOLS_BIN=%ADS_TOOLS%\BIN

GOTO :EOF


rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_RVDS_V30_VARS

@echo setting vars for RVDS 3.0 compiler
SET COMPILER_TOOL=RVDS
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%
set ARMHOME=%ARMROOT%
set ARMBIN=%RVCT30BIN%
set ARMINC=%RVCT30INC%
set ARMLIB=%RVCT30LIB%
set ADS_TOOLS_BIN=%ARMBIN%
GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_RVDS_V31_VARS
@echo setting vars for RVDS 3.1 compiler
SET COMPILER_TOOL=RVDS
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

set ARMHOME=%ARMROOT%
set ARMBIN=%RVCT31BIN%
set ARMINC=%RVCT31INC%
set ARMLIB=%RVCT31LIB%
set ADS_TOOLS_BIN=%ARMBIN%
GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_RVDS_V40_VARS
@echo setting vars for RVDS 4.0 compiler
SET COMPILER_TOOL=RVDS
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

set ARMHOME=%ARMROOT%
set ARMBIN=%RVCT40BIN%
set ARMINC=%RVCT40INC%
set ARMLIB=%RVCT40LIB%
set ADS_TOOLS_BIN=%ARMBIN%
GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_RVDS_V12_VARS
@echo setting vars for RVDS 1.2 compiler
SET COMPILER_TOOL=RVDS
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

set ARMROOT=%ARMHOME%
set ADS_TOOLS_BIN=%ARMDLL%
GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_BLACKFIN_VARS
@ECHO Setting ADI env var and path

set COMPILER_TOOL=BLACKFIN
set NO_ADS_WRAPPER=1
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%


IF NOT "%PORT_BUILD%"=="" GOTO :DSP_INSTALLED
  set ADI_DSP=%CLRROOT%\tools\adi\
  set ANALOGD_LECENSE_FILE=%ADI_DSP%\license.dat

  echo adding HKLM\Software\Analog Devices\VisualDSP++ 5.0\Install Path=%ADI_DSP%
  call reg add "HKLM\Software\Analog Devices" /f 
  call reg add "HKLM\Software\Analog Devices\VisualDSP++ 5.0" /f 
  call reg add "HKLM\Software\Analog Devices\VisualDSP++ 5.0" /f /v "Install Path" /t REG_SZ /d %ADI_DSP%
  echo adding HKLM\Software\Analog Devices\VisualDSP++ 5.0\License Path=%ADI_DSP%
  call reg add "HKLM\Software\Analog Devices\VisualDSP++ 5.0" /f /v "License Path" /t REG_SZ /d %ADI_DSP%

:DSP_INSTALLED

  IF "%ADI_DSP%"=="" set ADI_DSP=%ARG3%

set PATH=%ADI_DSP%;%PATH%

GOTO :EOF


rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_GCC_VARS
@echo setting vars for GCC compiler

rem use a default for GCC
IF "%ARG3%"=="" SET ARG3=\gnu\gcc
IF NOT EXIST %ARG3% GOTO :BAD_GCC_ARG

set ARMROOT=
set ADS_TOOLS_BIN=
set COMPILER_TOOL=GCC
set NO_ADS_WRAPPER=1
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

IF "%COMPILER_TOOL_VERSION%"=="GCC4.2" (

set GNU_VERSION=4.2.1
set ARMINC=%ARG3%\lib\gcc\arm-none-eabi\%GNU_VERSION%\include
set ARMLIB=%ARG3%\lib\gcc\arm-none-eabi\%GNU_VERSION%
set GNU_TOOLS=%ARG3%
set GNU_TOOLS_BIN=%ARG3%\bin
set GNU_TARGET=arm-none-eabi

) ELSE (

set GNU_VERSION=4.2.0
set ARMINC=%ARG3%\include\elips_bs
set ARMLIB=%ARG3%\lib
set GNU_TOOLS=%ARG3%
set GNU_TOOLS_BIN=%ARG3%\bin
set GNU_TARGET=arm-elf
set COMPILER_PATH=%ARG3%

)

GOTO :EOF

:BAD_GCC_ARG
@ECHO.
@ECHO Error - Invalid argument (%ARG3%).  Usage: setenv.cmd GCC_TOOL_PATH
@ECHO         Example:  setenv.cmd c:\tools\gcc
@ECHO.

GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_MDK_VARS
@ECHO Setting MDK env var and path for version %COMPILER_TOOL_VERSION%

rem use a default for MDK
SET COMPILER_TOOL=MDK
set NO_ADS_WRAPPER=1
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

if "%ARG3%"=="" set ARG3=\Keil\ARM
if NOT EXIST "%ARG3%" GOTO :BAD_MDK_ARG

set MDK_TOOL_PATH=%ARG3%
set PATH=%MDK_TOOL_PATH%;%PATH%
if "%COMPILER_TOOL_VERSION%"=="MDK3.1" (
    set RVCT31BIN=%MDK_TOOL_PATH%\ARM\BIN31
) ELSE (    
    set RVCT31BIN=%MDK_TOOL_PATH%\ARM\BIN40
)
set RVCT31LIB=%MDK_TOOL_PATH%\RV31\LIB
set RVCT31INC=%MDK_TOOL_PATH%\RV31\INC

GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_VS9_VARS
SET COMPILER_TOOL=VS9
set NO_ADS_WRAPPER=1
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

IF NOT "%VS90COMNTOOLS%" == "" (
    CALL "%VS90COMNTOOLS%vsvars32.bat"
) ELSE (
    @ECHO WARNING: VISUAL C++ 9.0 IS NOT INSTALLED ON THIS MACHINE
)

GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_VS10_VARS
SET COMPILER_TOOL=VS10
set NO_ADS_WRAPPER=1
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

IF NOT "%VS100COMNTOOLS%" == "" (
    CALL "%VS100COMNTOOLS%vsvars32.bat"
) ELSE (
    @ECHO WARNING: VISUAL C++ 10.0 IS NOT INSTALLED ON THIS MACHINE
)

GOTO :EOF

:BAD_MDK_ARG
@ECHO.
@ECHO Error - Invalid argument.  Usage: setenv.cmd MDK_TOOL_PATH
@ECHO         Example:  setenv.cmd c:\Keil\ARM
@ECHO.

GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
:SET_SHC_VARS
@echo off
SET COMPILER_TOOL=SHC
set NO_ADS_WRAPPER=1
set DOTNETMF_COMPILER=%COMPILER_TOOL_VERSION%

IF "%PORT_BUILD%"=="" (
   set SHC_TOOL_PATH=%SPOCLIENT%\tools\SH\9_2_0
)ELSE (
   IF "%SHC_TOOL_PATH%"=="" set SHC_TOOL_PATH=%ARG3:"=%
)

if NOT EXIST "%SHC_TOOL_PATH%" GOTO :BAD_SHC_ARG

set PATH=%SHC_TOOL_PATH%\bin;%PATH%
set SHC_LIB=%SHC_TOOL_PATH%\bin
set SHC_INC=%SHC_TOOL_PATH%\include
set SHC_TMP=%SHC_TOOL_PATH%\CTemp
set SHC_TOOLS_BIN=%SHC_TOOL_PATH%\bin

GOTO :EOF

:BAD_SHC_ARG
SET SHC_TOOL_PATH=

@ECHO.
@ECHO Error - Invalid argument.  Usage: setenv.cmd SHC_TOOL_PATH
@ECHO         Example:  setenv.cmd c:\sh\9_2_0
@ECHO.

GOTO :EOF

rem @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
