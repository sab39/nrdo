using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.PortableSql
{
    public abstract class SqlTranslatorBase
    {
        protected abstract void ReadParameter(string paramName);
        protected abstract void ReadIdentifier(string identifier);
        protected abstract void ReadSqlChar(char ch);
        protected abstract void ReadSqlString(string str);

        protected abstract void ReadSyntaxError(string errorMessage);

        protected abstract void ReadSqlForSequenceValue(string tableName);
        protected abstract void ReadSqlForVariableValue(string varName);
        protected abstract void ReadSqlForDeclare(string varName, string varType, out Action finishAction);
        protected abstract void ReadSqlForAssign(string varName, out Action closeAction);
        protected abstract void ReadSqlForNow();
        protected abstract void ReadSqlForToday();
        protected abstract void ReadSqlForTrue();
        protected abstract void ReadSqlForFalse();
        protected abstract void ReadSqlForConcat();

        // This algorithm is hard to read and follow, but it's a straight port of the Java version with the API abstracted.
        protected void Process(string input)
        {
            // Whether we are currently in the middle of an SQL string
            bool instr = false;

            // Whether we are currently in the middle of an SQL comment
            bool incomment = false;

            // nameStart gets set when a single : is encountered - this is a parameter
            int nameStart = -1;

            // identifierStart gets set when :" or :[ are encountered - this is an identifier
            // Both syntaxes are supported because :" is easier to use in Smelt files and :[ is easier to use in c# strings
            // Also :" resembles SQL standard identifier quoting with double-quotes and :[ resembles SQL server's quoting with square brackets.
            int identifierStart = -1;

            // specialStart gets set when a :: is encountered - this is a special
            // portable-sql directive.
            int specialStart = -1;

            // parenNest and closeAction get set when full, substituting processing
            // of one of the arguments of a portable-sql directive is needed. When
            // parenNest hits zero, closeAction gets called, the
            // close-paren is skipped, and normal processing resumes.
            // Note that this doesn't support multiply-nested portable-sql declarations,
            // but currently only one level is possible anyway (::assign is the only
            // directive that supports substituted args, and can't appear inside
            // itself).
            int parenNest = 0;
            Action closeAction = null;

            // finishStr gets inserted at the end of the entire statement.
            List<Action> finishActions = new List<Action>();

            // To avoid specialcasing the end of the statement, we add an extra character at the end.
            input += " ";

            // Loop through the characters in the string
            for (int pos = 0; pos < input.Length; pos++)
            {
                var ch = input[pos];

                // If we're in the middle of parsing a parameter, see if we've reached
                // the end of it, and if so look it up and store it.
                if (nameStart >= 0)
                {
                    if (ch != '_' && !char.IsLetterOrDigit(ch))
                    {
                        var paramName = input.Substring(nameStart, pos - nameStart);
                        ReadParameter(paramName);
                        nameStart = -1;
                        pos--;
                    }
                }

                // If we're in the middle of parsing an identifier, see if we've reached
                // the end of it, and if so, output it.
                else if (identifierStart >= 0)
                {
                    char closeQuote;
                    switch (input[identifierStart - 1]) {
                        case '"': closeQuote = '"'; break;
                        case '[': closeQuote = ']'; break;
                        default: throw new ApplicationException("Identifier open quote character wasn't \" or [, something weird is going on");
                    }
                    if (ch == closeQuote)
                    {
                        var identifier = input.Substring(identifierStart, pos - identifierStart);
                        ReadIdentifier(identifier);
                        identifierStart = -1;
                    }
                }

                // If we're in the middle of parsing a portable-sql directive,
                // see if we've reached the end of it, and if so interpret and
                // translate it.
                else if (specialStart >= 0)
                {
                    if (ch != '_' && !char.IsLetterOrDigit(ch))
                    {
                        var specialName = input.Substring(specialStart, pos - specialStart)
                            .ToLowerInvariant();

                        switch (specialName)
                        {
                            case "identity":
                            case "seq":
                                int end = pos + 1;
                                while (end < input.Length && (input[end] == '_' || input[end] == ':' ||
                                    char.IsWhiteSpace(input[end]) || char.IsLetterOrDigit(input[end]))) end++;

                                if (ch != '(' || end >= input.Length || input[end] != ')')
                                {
                                    ReadSyntaxError("::" + specialName + " must be followed by (tablename)");
                                    return;
                                }

                                var tableName = input.Substring(pos + 1, end - pos - 1);
                                ReadSqlForSequenceValue(tableName);

                                pos = end + 1;
                                ch = input[pos];
                                break;

                            case "now":
                                ReadSqlForNow();
                                break;

                            case "today":
                                ReadSqlForToday();
                                break;

                            case "true":
                                ReadSqlForTrue();
                                break;

                            case "false":
                                ReadSqlForFalse();
                                break;

                            case "declare":
                                if (ch != '(')
                                {
                                    ReadSyntaxError("::declare must be followed by (varname vartype)");
                                    return;
                                }

                                ch = input[++pos];
                                int startPos = pos;
                                while (ch == '_' || char.IsLetterOrDigit(ch))
                                {
                                    ch = input[++pos];
                                }
                                var declareName = input.Substring(startPos, pos - startPos);

                                if (!char.IsWhiteSpace(ch))
                                {
                                    ReadSyntaxError("::declare must be followed by (varname vartype)");
                                    return;
                                }
                                while (char.IsWhiteSpace(ch))
                                {
                                    ch = input[++pos];
                                }

                                int nest = 1;
                                startPos = pos--;
                                var found = false;
                                while (pos < input.Length - 1 && nest > 0)
                                {
                                    ch = input[++pos];
                                    if (ch == '(')
                                    {
                                        nest++;
                                    }
                                    else if (ch == ')')
                                    {
                                        nest--;
                                        if (nest == 0)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                                if (!found)
                                {
                                    ReadSyntaxError("Missing close paren ')' after ::declare");
                                    return;
                                }

                                var declareType = input.Substring(startPos, pos - startPos).Trim();
                                Action declareFinish;
                                ReadSqlForDeclare(declareName, declareType, out declareFinish);
                                if (declareFinish != null) finishActions.Insert(0, declareFinish);
                                pos++;
                                break;

                            case "assign":
                                if (ch != '(')
                                {
                                    ReadSyntaxError("::assign must be followed by '(varname=value)'");
                                    return;
                                }
                                ch = input[++pos];
                                var astartPos = pos;
                                while (ch == '_' || char.IsLetterOrDigit(ch))
                                {
                                    ch = input[++pos];
                                }
                                var varname = input.Substring(astartPos, pos - astartPos);

                                if (ch != ' ' && ch != '=')
                                {
                                    ReadSyntaxError("::assign must be followed by '(varname=value)'");
                                    return;
                                }
                                while (pos < input.Length - 1 && (ch == ' ' || ch == '='))
                                {
                                    ch = input[++pos];
                                }
                                ch = ' ';
                                pos--;
                                Action assignClose;
                                ReadSqlForAssign(varname, out assignClose);
                                if (assignClose != null) closeAction = assignClose;

                                parenNest = 1;
                                break;

                            case "var":
                                int endv = pos + 1;
                                while (endv < input.Length && (input[endv] == '_' || char.IsLetterOrDigit(input[endv]))) endv++;

                                if (ch != '(' || endv >= input.Length || input[endv] != ')')
                                {
                                    ReadSyntaxError("::" + specialName + " must be followed by (varname)");
                                    return;
                                }

                                var varName = input.Substring(pos + 1, endv - pos - 1);
                                ReadSqlForVariableValue(varName);

                                pos = endv + 1;
                                ch = input[pos];
                                break;

                            // other things we might want to support...
                            //case "upper":
                            //case "lower":
                            //case "substring":
                            //case "len":

                            default:
                                ReadSyntaxError("Unknown portable-sql directive ::" + specialName);
                                return;
                        }
                        specialStart = -1;
                        pos--;
                    }
                }
                else if (incomment)
                {
                    if (ch == '\r' || ch == '\n') incomment = false;
                }
                else if (!instr && ch == '-' && input[pos + 1] == '-')
                {
                    incomment = true;
                }
                else if (!instr && ch == ':')
                {
                    char next = input[pos + 1];
                    if (next == ':')
                    {
                        pos++;
                        specialStart = pos + 1;
                    }
                    else if (next == '+')
                    {
                        ReadSqlForConcat();
                    }
                    else if (next == '\\')
                    {
                        ReadSqlChar(':');
                    }
                    else if (next == '_' || char.IsLetter(next))
                    {
                        nameStart = pos + 1;
                    }
                    else if (next == '"' || next == '[')
                    {
                        pos++;
                        identifierStart = pos + 1;
                    }
                    else
                    {
                        ReadSyntaxError("Illegal unescaped colon (:)");
                        return;
                    }
                }
                else if (!instr && parenNest > 0)
                {
                    if (ch == '(')
                    {
                        parenNest++;
                        ReadSqlChar(ch);
                    }
                    else if (ch == ')')
                    {
                        parenNest--;
                        if (parenNest == 0)
                        {
                            if (closeAction != null) closeAction();
                            closeAction = null;
                        }
                        else
                        {
                            ReadSqlChar(ch);
                        }
                    }
                    else
                    {
                        ReadSqlChar(ch);
                    }
                }
                else
                {
                    ReadSqlChar(ch);
                }
                if (!incomment && ch == '\'') instr = !instr;
            }
            if (parenNest > 0)
            {
                ReadSyntaxError("Missing close paren");
                return;
            }
            if (instr)
            {
                ReadSyntaxError("Unterminated SQL string");
                return;
            }
            if (identifierStart >= 0)
            {
                ReadSyntaxError("Unterminated SQL identifier");
            }
            foreach (var finish in finishActions)
            {
                finish();
            }
        }
    }
}
