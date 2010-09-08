;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Copyright (c) Microsoft Corporation.  All rights reserved.
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;


    EXPORT  EntryPoint

    EXPORT  PreStackInit_Exit_Pointer

    IMPORT  PreStackInit


    IF HAL_REDUCESIZE = "1"
        IMPORT BootEntryLoader
    ELSE
        IMPORT  BootEntry
    ENDIF
    IMPORT  BootstrapCode
    IMPORT  ARM_Vectors         ; Even if we don't use this symbol, it's required by the linker to properly include the Vector trampolines.


    PRESERVE8

    ;*************************************************************************

PSR_MODE_USER       EQU     0xD0
PSR_MODE_FIQ        EQU     0xD1
PSR_MODE_IRQ        EQU     0xD2
PSR_MODE_SUPERVISOR EQU     0xD3
PSR_MODE_ABORT      EQU     0xD7
PSR_MODE_UNDEF      EQU     0xDB
PSR_MODE_SYSTEM     EQU     0xDF

; main can be use for exception
STACK_MODE_MAIN     EQU     2048
STACK_MODE_PROCESS  EQU     2048

    ;*************************************************************************

    AREA SectionForStackBottom,       DATA
StackBottom       DCD 0
    AREA SectionForStackTop,          DATA
StackTop          DCD 0
    AREA SectionForHeapBegin,         DATA
HeapBegin         DCD 0
    AREA SectionForHeapEnd,           DATA
HeapEnd           DCD 0
    AREA SectionForCustomHeapBegin,   DATA
CustomHeapBegin   DCD 0
    AREA SectionForCustomHeapEnd,     DATA
CustomHeapEnd     DCD 0


    EXPORT StackBottom
    EXPORT StackTop
    EXPORT HeapBegin
    EXPORT HeapEnd
    EXPORT CustomHeapBegin
    EXPORT CustomHeapEnd

  

    IF HAL_REDUCESIZE = "1" :LAND: TargetLocation != "RAM"
    ; -----------------------------------------------
    ; ADD BOOT MARKER HERE IF YOU NEED ONE
    ; -----------------------------------------------
    ENDIF       ;[HAL_REDUCESIZE = "1" :LAND: TargetLocation != "RAM"]

    AREA ||i.EntryPoint||, CODE, READONLY


    ENTRY

EntryPoint_Restart_Pointer 
    DCD     EntryPoint_Restart

EntryPoint

    ; designed to be a vector area for ARM
    ; RESET
    ; keep PortBooter signature the same
 ;   msr     cpsr_c, #PSR_MODE_SYSTEM    ; go into System mode, interrupts off

    ; allow per processor pre-stack initialization initialization

PreStackEntry
    B       PreStackInit

PreStackInit_Exit_Pointer 

    ldr     r0, =StackTop               ; new SYS stack pointer for a full decrementing stack
    msr     msp, r0                      ; stack top
    sub     r0, r0, #STACK_MODE_MAIN    ; ( take the size of main stack, usually for the exception)
    msr     psp,r0                      ; sets for the process stack

    
    ;******************************************************************************************
    ; This ensures that we execute from the real location, regardless of any remapping scheme *
    ;******************************************************************************************

    ldr     pc, EntryPoint_Restart_Pointer

EntryPoint_Restart   

    ;*********************************************************************

    bl      BootstrapCode

    IF HAL_REDUCESIZE = "1"
        b   BootEntryLoader
    ELSE
        b   BootEntry
    ENDIF


    END
