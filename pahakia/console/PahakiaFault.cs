using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace pahakia.fault
{
    public interface CodeBlock<T>
    {
        T f ();
    }

    public interface KatchBlock<T>
    {
        T f (Fault ex);
    }

    public interface FinaleBlock
    {
        void f ();
    }

    public class MessageCode
    {
        private string code;
        private int numArgs = 0;
        private string messageTemplate;

        public MessageCode (string code, int numArgs, string messageTemplate)
        {
            if (code == null || code.Trim ().Length == 0) {
                throw Fault.create (FaultCodes.CodeIsMandatory);
            }
            if (numArgs < 0) {
                throw Fault.create (FaultCodes.NumArgsNegative, numArgs + "");
            }
            if (messageTemplate == null || messageTemplate.Trim ().Length == 0) {
                throw Fault.create (FaultCodes.EmptyMessageTemplate);
            }
            // validate numArgs match message
            for (int i = 0; i < numArgs; i++) {
                if (messageTemplate.IndexOf ("{" + i + "}") < 0) {
                    throw Fault.create (FaultCodes.InsufficientArgs, code, numArgs + "", messageTemplate);
                }
            }

            this.code = code;
            this.numArgs = numArgs;
            this.messageTemplate = messageTemplate;
        }

        public string getCode ()
        {
            return code;
        }

        public int getNumArgs ()
        {
            return numArgs;
        }

        public string getMessageTemplate ()
        {
            return messageTemplate;
        }

        public bool Equals (MessageCode other)
        {
            return code == other.code;
        }

        public virtual string format (params string[] args)
        {
            return string.Format (messageTemplate, args);
        }

        public override string ToString ()
        {
            return "code=" + code + ", num args=" + numArgs + ", message template=" + messageTemplate;
        }
    }

    public sealed class FaultCode : MessageCode
    {

        public FaultCode (string code, int numArgs, string messageTemplate) : base (code, numArgs, messageTemplate)
        {
        }

        internal static FaultCode create (Type exceptionType)
        {
            return new FaultCode (exceptionType.FullName, 1, "{0}");
        }

        public override string format (params string[] args)
        {
            return getCode () + ": " + base.format (args);
        }
    }

    public sealed class FaultCodes
    {
        private FaultCodes ()
        {
        }

        public static readonly FaultCode CodeIsMandatory = new FaultCode ("pahakia.fault.CodeIsMandatory", 0, "Code is mandatory when creating MessageCode.");
        public static readonly FaultCode NumArgsNegative = new FaultCode ("pahakia.fault.NumArgsNegative", 1, "Number of arguments can not be negative when creating MessageCode: {0}.");
        public static readonly FaultCode InsufficientArgs = new FaultCode ("pahakia.fault.InsufficientArgs", 3, "Message template does not have enough \"'{'n'}'\"s for code: {0}, expected: {1}, message template: \"{2}\".");
        public static readonly FaultCode EmptyMessageTemplate = new FaultCode ("pahakia.fault.EmptyMessageTemplate", 0, "Message template may not be null or empty.");
        public static readonly FaultCode NumArgsNotMatchCode = new FaultCode ("pahakia.fault.CodeNumArgsNoMatch", 3, "The number of String parameters must match the number of arguments specified in code: {0}, expected: {1}, got: {2}.");
    }

    internal sealed class KatchHandler<T>
    {
        internal string code0;
        internal string[] codes;
        internal KatchBlock<T> katchBlock;

        public KatchHandler (String code0, params String[] codes)
        {
            this.code0 = code0;
            this.codes = codes;
        }

        public KatchHandler (KatchBlock<T> katchBlock, String code0, params string[] codes)
        {
            this.code0 = code0;
            this.codes = codes;
            this.katchBlock = katchBlock;
        }
    }

    public class CodeBlockWorker<T>
    {

        private CodeBlock<T> codeBlock;
        private IList<KatchHandler<T>> handlers = new List<KatchHandler<T>> ();
        private FinaleBlock finaleBlock;

        internal CodeBlockWorker (CodeBlock<T> codeBlock)
        {
            this.codeBlock = codeBlock;
        }

        public CodeBlockWorker<T> ignore (string code0, params string[] codes)
        {
            handlers.Add (new KatchHandler<T> (code0, codes));
            return this;
        }

        public CodeBlockWorker<T> ignore (FaultCode code0, params FaultCode[] codes)
        {
            String[] strCodes = new String[codes.Length];
            for (int i = 0; i < codes.Length; i++) {
                FaultCode code = codes [i];
                strCodes [i] = code.getCode ();
            }
            handlers.Add (new KatchHandler<T> (code0.getCode (), strCodes));
            return this;
        }

        public CodeBlockWorker<T> ignore (Type clazz, params Type[] classes)
        {
            String[] codes = new String[classes.Length];
            for (int i = 0; i < codes.Length; i++) {
                Type clz = classes [i];
                codes [i] = clz.FullName;
            }
            handlers.Add (new KatchHandler<T> (clazz.FullName, codes));
            return this;
        }

        public CodeBlockWorker<T> katch (KatchBlock<T> katchBlock, String code0, params String[] codes)
        {
            handlers.Add (new KatchHandler<T> (katchBlock, code0, codes));
            return this;
        }

        public CodeBlockWorker<T> katch (String code, KatchBlock<T> katchBlock)
        {
            return katch (katchBlock, code);
        }

        public CodeBlockWorker<T> katch (KatchBlock<T> katchBlock, FaultCode code0, params FaultCode[] codes)
        {
            String[] strCodes = new String[codes.Length];
            for (int i = 0; i < codes.Length; i++) {
                FaultCode code = codes [i];
                strCodes [i] = code.getCode ();
            }
            handlers.Add (new KatchHandler<T> (katchBlock, code0.getCode (), strCodes));
            return this;
        }

        public CodeBlockWorker<T> katch (FaultCode code, KatchBlock<T> katchBlock)
        {
            return katch (katchBlock, code);
        }

        public CodeBlockWorker<T> katch (Type clz, KatchBlock<T> katchBlock)
        {
            handlers.Add (new KatchHandler<T> (katchBlock, clz.FullName));
            return this;
        }

        public CodeBlockWorker<T> katch (KatchBlock<T> katchBlock, Type clazz,
                                         params Type[] classes)
        {
            String[] codes = new String[classes.Length];
            for (int i = 0; i < codes.Length; i++) {
                Type clz = classes [i];
                codes [i] = clz.FullName;
            }
            handlers.Add (new KatchHandler<T> (katchBlock, clazz.FullName, codes));
            return this;
        }

        public T finale (FinaleBlock finaleBlock)
        {
            this.finaleBlock = finaleBlock;
            return finale ();
        }

        public T finale ()
        {
            try {
                return codeBlock.f ();
            } catch (Exception t) {
                Fault ex = Fault.naturalize (t);
                foreach (KatchHandler<T> handler in handlers) {
                    if (Regex.IsMatch (ex.getCode ().getCode (), handler.code0)) {
                        if (handler.katchBlock != null) {
                            return handler.katchBlock.f (ex);
                        } else {
                            return default(T);
                        }
                    }
                    foreach (string code in handler.codes) {
                        if (Regex.IsMatch (ex.getCode ().getCode (), code)) {
                            return handler.katchBlock.f (ex);
                        }
                    }
                }
                throw ex;
            } finally {
                if (finaleBlock != null) {
                    finaleBlock.f ();
                }
            }
        }
    }

    /**
 * <code>Fault</code> is the only exception type. You no longer katch exception but FaultCode. A fault contains
 * 
 * <ol>
 * <li><code>FaultCode code</code>: which contains a code, a message template and the number of place holders in the
 * message template.</li>
 * <li><code>String... args</code>: the parameters used to fill the place holders in the message template in FaultCode.</li>
 * </ol>
 * 
 * <b>Usage</b>:
 * 
 * <ol>
 * <li>Create Fault:<br>
 * <code>Fault.create(faultCode, "arg0", "arg1");</code></li>
 * <li>Naturalize (convert) exception:<br>
 * <code>Fault.natualize(ex);</code></li>
 * <li>Katch one code:<br>
 * <code>Fautl.tri(codeBlock).katch(fautlCode, katchBlock).finale([optionalFinaleBlock]);</code></li>
 * <li>Katch multiple codes:<br>
 * <code>Fautl.tri(codeBlock).katch(katchBlock, faultCode0,
 * faultCode1).finale([optionalFinaleBlock]);</code></li>
 * <li>Ignore:<br>
 * <code>Fautl.tri(codeBlock).ignore(fautlCode0, faultCode1).finale([optionalFinaleBlock]);</code></li>
 * </ol>
 * 
 * <b>Note</b>:
 * <ul>
 * <li><code>Fault.tri/katch/finale</code> is the replacement of the traditional <code>try/catch/finally</code> in java.
 * </li>
 * <li><code>Fault.tri/ignore/finale</code> can be used to ignore certain fault codes (YES, Exception swallowing made
 * legal!).</li>
 * <li>
 * <code>katch()/ignore()</code> can katch/ignore <code>FaultCode</code>, <code>String</code>, or String regular
 * expression.</li>
 * <li>
 * There can be multiple <code>katch</code>es/<code>ignore</code>s. <code>katch</code> and <code>ignore</code> can be
 * mixed.</li>
 * <li>
 * <code>finale</code> is mandatory. It's a programming error if it is forgotten because codeBlock/katchBlock will not
 * be executed.</li>
 * <li>
 * </ul>
 * 
 * @see FaultCode
 * @see JreFaultCodes
 */
    public sealed class Fault : Exception
    {
        private FaultCode code;
        private string[] args;

        private Fault (FaultCode code, string message, params string[] args) : base (message)
        {
            this.code = code;
            this.args = args;
            if (args.Length != code.getNumArgs ()) {
                throw Fault.create (FaultCodes.NumArgsNotMatchCode, code.getCode (), code.getNumArgs () + "", args.Length + "");
            }
        }

        private Fault (FaultCode code, string message, Exception cause, params string[] args) : base (message, cause)
        {
            this.code = code;
            this.args = args;
            if (args.Length != code.getNumArgs ()) {
                throw Fault.create (FaultCodes.NumArgsNotMatchCode, code.getCode (), code.getNumArgs () + "", args.Length + "");
            }
        }

        public FaultCode getCode ()
        {
            return code;
        }

        public string[] getArgs ()
        {
            return args;
        }

        public static Fault create (FaultCode code, params string[] args)
        {
            return create (code, null, args);
        }

        static Fault create (FaultCode code, Exception ex, params string[] args)
        {
            string message = code.format (args);
            return new Fault (code, message, ex, args);
        }

        /**
     * Convert a <code>Throwable (Error/Exception/RuntimeException)</code> other than <code>Fault</code> into a
     * <code>Fault</code>. This function unwraps the <code>Throwable</code> if it is an
     * <code>InvocationTargetException</code> or <code>UndeclaredThrowableException</code>.
     * 
     * It uses the full class name of the throwable as the fault code with a message template of <code>'{0}'</code>,
     * i.e. just one place holder. The original exception is the cause of the returned Fault. The message of the
     * original exception is used to populate the place holder. Please refer to <code>JreFaultCode</code> for the
     * FaultCodes of all JRE <code>Exceptions/Errors</code>.
     * 
     * @param ex
     *            the exception to be naturalized
     * @return Fault
     */
        public static Fault naturalize (Exception ex)
        {
            if (ex is Fault) {
                return (Fault)ex;
            }
            return Fault.create (FaultCode.create (ex.GetType ()), ex, ex.Message);
        }

        public static CodeBlockWorker<T> tri<T> (CodeBlock<T> code)
        {
            return new CodeBlockWorker<T> (code);
        }
    }
}