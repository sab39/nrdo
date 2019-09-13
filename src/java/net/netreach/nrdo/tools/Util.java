///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.Util
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2004/01/26
// Description: Shared utility methods for parsing dfn and qu files etc.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool.
// Copyright (c) 2000-2001 NetReach, Inc.
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// The GNU General Public License should be located in the file COPYING.gpl.
// For more information on the specific license terms for nrdo, please see the
// file COPYING.
//
// For more information about nrdo, please contact nrdo@netreach.com or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.nrdo.tools;

// Collections classes used to implement the appropriate behavior.
import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import net.netreach.util.FileLocatedException;
import net.netreach.util.Mappable;

/**
 * Shared utility methods for parsing dfn and qu files etc.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class Util {

  public static class SQLParam implements Mappable {
    public String sqlbefore;

    public SQLParam(String sqlbefore) {
      this.sqlbefore = sqlbefore;
    }

    protected Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.put("sqlbefore", sqlbefore);
      }
      return asMap;
    }
  }

  public static String translateSql(String sql, Config cfg)
      throws FileLocatedException, IOException {
    List result = scanParams(sql, cfg, new HashMap());
    if (result == null) return null;
    if (result.isEmpty()) return "";
    if (result.size() > 1)
      throw new RuntimeException("Named parameters not allowed here");
    return ((SQLParam) result.get(0)).sqlbefore;
  }

  public static List scanParams(String rawSQL, Config cfg, Map paramsByName)
      throws FileLocatedException, IOException {
    if (rawSQL == null) return null;
    List result = new ArrayList();
    boolean instr = false;

    // nameStart gets set when a single ":" is encountered - this is a parameter
    int nameStart = -1;

    // specialStart gets set when a "::" is encountered - this is a special
    // portable-sql directive.
    int specialStart = -1;

    // parenNest and closeStr get set when full, substituting processing
    // of one of the arguments of a portable-sql directive is needed. When
    // parenNest hits zero, closeStr gets inserted into the buffer, the
    // close-paren is skipped, and normal processing resumes.
    // Note that this doesn't support multiply-nested portable-sql declarations,
    // but currently only one level is possible anyway (::assign is the only
    // directive that supports substituted args, and can't appear inside
    // itself).
    int parenNest = 0;
    String closeStr = null;

    // finishStr gets inserted at the end of the entire statement.
    String finishStr = "";

    String padded = rawSQL + " ";
    StringBuffer sub = new StringBuffer(rawSQL.length() + 20);

    // Loop through the characters in the string
    for (int pos = 0; pos < padded.length(); pos++) {
      char ch = padded.charAt(pos);

      // If we're in the middle of parsing a parameter, see if we've reached
      // the end of it, and if so look it up and store it.
      if (nameStart >= 0) {
        if (!Character.isJavaIdentifierPart(ch)) {
          String paramName = padded.substring(nameStart, pos);
          Object param = paramsByName.get(paramName);
          if (param == null) {
            throw new RuntimeException("No such parameter: " + paramName);
          } else if (param instanceof TableDef.Field) {
            result.add(new TableDef.SQLParam((TableDef.Field) param, sub
                .toString()));
          } else if (param instanceof QueryDef.Field) {
            result.add(new QueryDef.SQLParam((QueryDef.Field) param, sub
                .toString()));
          } else {
            throw new RuntimeException("INTERNAL ERROR");
          }
          sub = new StringBuffer(rawSQL.length() + 20 - pos);
          nameStart = -1;
          pos--;
        }

        // If we're in the middle of parsing a portable-sql directive,
        // see if we've reached the end of it, and if so interpret it
        // and append the translated version to 'sub'.
      } else if (specialStart >= 0) {
        if (!Character.isJavaIdentifierPart(ch)) {
          String specialName = padded.substring(specialStart, pos)
              .toLowerCase();

          if ("identity".equals(specialName) || "seq".equals(specialName)) {
            int end = padded.indexOf(')', pos);
            if (ch != '(' || end < 0) {
              throw new RuntimeException(
                  "::identity/::seq must be followed by '(tablename)'");
            }
            String arg = padded.substring(pos + 1, end);
            TableDef td = cfg.dfnbase.loadMinimum(arg);
            if (td == null)
              throw new RuntimeException("Cannot load table " + arg);
            sub.append(cfg.dbadapter.getSeqSql(td));
            ch = padded.charAt(end + 1);
            pos = end + 1;

          } else if ("now".equals(specialName)) {
            sub.append(cfg.dbadapter.getNowSql());

          } else if ("today".equals(specialName)) {
            sub.append(cfg.dbadapter.getTodaySql());

          } else if ("true".equals(specialName)) {
            sub.append(cfg.dbadapter.getTrueSql());

          } else if ("false".equals(specialName)) {
            sub.append(cfg.dbadapter.getFalseSql());

          } else if ("declare".equals(specialName)) {
            if (ch != '(')
              throw new RuntimeException(
                  "::declare must be followed by '(varname vartype)'");
            ch = padded.charAt(++pos);
            int startPos = pos;
            while (Character.isJavaIdentifierPart(ch)) {
              ch = padded.charAt(++pos);
            }
            String varname = padded.substring(startPos, pos);

            if (ch != ' ')
              throw new RuntimeException(
                  "::declare must be followed by '(varname vartype)'");
            int nest = 1;
            startPos = pos;
            boolean found = false;
            while (pos < padded.length() - 1 && nest > 0) {
              ch = padded.charAt(++pos);
              if (ch == '(') {
                nest++;
              } else if (ch == ')') {
                nest--;
                if (nest == 0) {
                  found = true;
                  break;
                }
              }
            }
            if (!found)
              throw new RuntimeException(
                  "Missing close paren ')' after ::declare");

            String vartype = padded.substring(startPos, pos).trim();
            sub.append(cfg.dbadapter.getDeclareSql(varname, cfg.dbtypemap
                .map(vartype)));
            pos++;

          } else if ("assign".equals(specialName)) {
            if (ch != '(')
              throw new RuntimeException(
                  "::assign must be followed by '(varname=value)'");
            ch = padded.charAt(++pos);
            int startPos = pos;
            while (Character.isJavaIdentifierPart(ch)) {
              ch = padded.charAt(++pos);
            }
            String varname = padded.substring(startPos, pos);

            if (ch != ' ' && ch != '=')
              throw new RuntimeException(
                  "::assign must be followed by '(varname=value)'");
            while (pos < padded.length() - 1 && (ch == ' ' || ch == '=')) {
              ch = padded.charAt(++pos);
            }
            ch = ' ';
            pos--;
            sub.append(cfg.dbadapter.getAssignBeginSql(varname));
            closeStr = cfg.dbadapter.getAssignEndSql(varname);
            finishStr = cfg.dbadapter.getAssignEndBlockSql(varname) + finishStr;

            parenNest = 1;

          } else if ("var".equals(specialName)) {
            int end = padded.indexOf(')', pos);
            if (ch != '(' || end < 0) {
              throw new RuntimeException(
                  "::var must be followed by '(varname)'");
            }
            String arg = padded.substring(pos + 1, end);
            if (arg.indexOf(' ') >= 0)
              throw new RuntimeException("variable name cannot contain spaces");
            sub.append(cfg.dbadapter.getVarSql(arg));
            pos = end + 1;

          } else if ("upper".equals(specialName) || "lower".equals(specialName)
              || "substring".equals(specialName) || "len".equals(specialName)) {
            throw new RuntimeException(
                "::upper/::lower/::substring/::len not yet implemented");
            // parse out a parameter (up to the matching ')')
            // sub.append(cfg.dbadapter.getUpperSql(param));
            // or sub.append(cfg.dbadapter.getLowerSql(param));
            // or sub.append(cfg.dbadapter.getSubstringSql(param, param,
            // param));
            // or sub.append(cfg.dbadapter.getLenSql(param));

          } else {
            throw new RuntimeException("Unknown portable-sql directive ::"
                + specialName);
          }
          specialStart = -1;
          pos--;
        }
      } else if (!instr && ch == ':') {
        char next = padded.charAt(pos + 1);
        if (next == ':') {
          pos++;
          specialStart = pos + 1;
        } else if (next == '+') {
          sub.append(cfg.dbadapter.getConcatSql());
        } else if (next == '\\') {
          sub.append(':');
        } else if (Character.isJavaIdentifierPart(next)) {
          nameStart = pos + 1;
        } else {
          throw new RuntimeException("Illegal unescaped colon (:)");
        }
      } else if (!instr && parenNest > 0) {
        if (ch == '(') {
          parenNest++;
          sub.append(ch);
        } else if (ch == ')') {
          parenNest--;
          if (parenNest == 0) {
            sub.append(closeStr);
            closeStr = null;
          } else {
            sub.append(ch);
          }
        } else {
          sub.append(ch);
        }
      } else {
        sub.append(ch);
      }
      if (ch == '\'') instr = !instr;
    }
    if (parenNest > 0) throw new RuntimeException("Missing close paren");
    if (instr) throw new RuntimeException("Unterminated SQL string");
    sub.append(finishStr);
    result.add(new SQLParam(sub.toString()));
    return result;
  }
}
