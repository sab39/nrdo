using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace NR.nrdo.Schema.Tool
{
    public enum ErrorResponse
    {
        // Flags that combine to make up the behaviors. Generally not intended for outside consumption, but can be used if some unusual
        // circumstance calls for a response outside of the normal combinations, like "prompt to retry, but if it still fails, ignore it" or
        // "throw without failing the overall process".
        
        /// <summary>
        /// Set a flag to indicate that the overall process failed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        FlagBit_SetFailureState = 1,

        /// <summary>
        /// Throw an exception.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        FlagBit_Throw = 2,
        
        /// <summary>
        /// Prompt to retry the command, if possible, before deciding whether it's an overall failure.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        FlagBit_PromptToRetry = 4,

        /// <summary>
        /// Output a warning (or error if FlagBit_SetFailureState is also true) message to the user.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        FlagBit_Warn = 8,

        // Common combinations of behavior.

        /// <summary>
        /// The default behavior. Prompt to retry, but if it never succeeds, set a flag to indicate the overall process is a failure.
        /// Some processing may still happen before we stop for good (eg, most Steps still run to completion in case there are other things they CAN do).
        /// </summary>
        Fail = FlagBit_Warn | FlagBit_PromptToRetry | FlagBit_SetFailureState,

        /// <summary>
        /// Failures here are expected and recoverable. Warn, but carry on as if nothing happened, no need to prompt to retry.
        /// </summary>
        Ignore = FlagBit_Warn,

        /// <summary>
        /// Prompt to retry, but if it never succeeds, set the failure flag and also throw an exception to abort processing immediately.
        /// </summary>
        Throw = FlagBit_Warn | FlagBit_PromptToRetry | FlagBit_SetFailureState | FlagBit_Throw,
    }
}
