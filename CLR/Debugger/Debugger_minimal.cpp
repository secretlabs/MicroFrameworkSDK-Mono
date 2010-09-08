////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#include "..\core\Core.h"
#include <TinyCLR_Debugging.h>

#define DEFINE_CMD(cmd)  { CLR_DBG_Debugger::Debugging_##cmd, CLR_DBG_Commands::c_Debugging_##cmd }
#define DEFINE_CMD2(cmd) { CLR_DBG_Debugger::Monitor_##cmd  , CLR_DBG_Commands::c_Monitor_##cmd   }

const CLR_Messaging_CommandHandlerLookup c_Debugger_Lookup_Request[] =
{
    DEFINE_CMD2(Ping       ),
    DEFINE_CMD2(Reboot     ),
    DEFINE_CMD(Execution_QueryCLRCapabilities),
};

const CLR_Messaging_CommandHandlerLookup c_Debugger_Lookup_Reply[] =
{
    DEFINE_CMD2(Ping),
};

#undef DEFINE_CMD
#undef DEFINE_CMD2

const CLR_UINT32 c_Debugger_Lookup_Request_count = ARRAYSIZE(c_Debugger_Lookup_Request);
const CLR_UINT32 c_Debugger_Lookup_Reply_count   = ARRAYSIZE(c_Debugger_Lookup_Reply);

