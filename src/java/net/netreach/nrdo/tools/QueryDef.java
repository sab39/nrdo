///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.QueryDef
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/31
// Description: Represent a query definition, and load one from a .qu file.
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
// For more information about nrdo, please contact nrdo@netreach.net or
// write to Stuart Ballard at NetReach Inc, 1 eCommerce Plaza, 124 S Maple
// Street, Ambler, PA  19002  USA.
///////////////////////////////////////////////////////////////////////////////

package net.netreach.nrdo.tools;

// Collections classes used to implement the appropriate behavior.
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Set;

import net.netreach.cgl.CGLParser;
import net.netreach.cgl.CGLRuntimeException;
import net.netreach.cgl.Context;
import net.netreach.cgl.Definition;
import net.netreach.cgl.Expr;
import net.netreach.smelt.ParseException;
import net.netreach.smelt.SmeltReader;
import net.netreach.util.FileLocatedException;
import net.netreach.util.LazyMap;
import net.netreach.util.Mappable;
import net.netreach.util.MappedList;
import net.netreach.util.Output;

/**
 * Represent a query definition, and load one from a .qu file.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class QueryDef implements Mappable, Comparable {

  String fullName;
  String name;
  String module;
  String description;
  String longDesc;
  List sql;
  String rawSQL;
  List params;
  List results;
  boolean multi;
  boolean isVoid;
  boolean storedproc;
  boolean storedfunction;
  boolean preUpgradeHook;
  Map paramsByName = new HashMap();
  DfnBase dfnbase;
  long age;
  Map beforeStmts = new HashMap();
  Definition extra;

  public int compareTo(Object o) {
    return fullName.compareTo(((TableDef) o).fullName);
  }

  public static class Field implements Mappable {
    public String name;
    public String javaType;
    public String description;
    public boolean nullable;
    public String sqlType;

    public Field(String name, String javaType, String sqlType,
        String description, boolean nullable) {
      this.name = name;
      this.javaType = javaType;
      this.sqlType = sqlType;
      this.description = description;
      this.nullable = nullable;
    }

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.put("type", javaType);
        asMap.put("fname", name);
        asMap.put("sqltype", sqlType);
        asMap.put("tblalias", "self");
        asMap.put("nullable", new Boolean(nullable));
        asMap.put("fielddesc", description);
      }
      return asMap;
    }

    public Field(Field f) {
      this(f.name, f.javaType, f.sqlType, f.description, f.nullable);
    }
  }

  static class SQLParam extends Util.SQLParam {
    Field field;

    SQLParam(Field field, String sqlbefore) {
      super(sqlbefore);
      this.field = field;
    }

    public Map toMap() {
      if (asMap == null) {
        super.toMap();
        if (field == null) {
          asMap.put("fname", null);
        } else {
          asMap.putAll(field.toMap());
        }
      }
      return asMap;
    }
  }

  List scanParams(String rawSQL) throws FileLocatedException, IOException {
    return Util.scanParams(rawSQL, dfnbase.cfg, paramsByName);
  }

  public QueryDef(DfnBase dfnbase, File file, String fullName) {
    this.dfnbase = dfnbase;
    this.age = file.lastModified();
    this.dfnFile = file;
    this.fullName = fullName;
    int colonpos = fullName.lastIndexOf(':');
    name = fullName.substring(colonpos + 1);
    module = (colonpos == -1) ? "" : fullName.substring(0, colonpos);
  }

  private boolean resolved = false;
  public final File dfnFile;

  public void resolve() throws FileLocatedException, IOException {
    resolve(null);
  }

  public void resolve(String reason) throws FileLocatedException, IOException {

    if (resolved) return;
    String whystr = reason == null ? "" : " (due to " + reason + ")";

    Output.println("Loading query " + fullName + whystr);

    // Load the definition file.
    SmeltReader sr = new SmeltReader(dfnFile);
    sr.readToken();

    sr.skipToken("query");

    String resultType = sr.checkString();
    if ("multi".equals(resultType)) {
      multi = true;
    } else if ("void".equals(resultType)) {
      isVoid = true;
    } else if (!"single".equals(resultType)) {
      throw new ParseException(sr, "Expected 'single', 'multi' or 'void', "
          + "found " + sr.lastToString());
    }
    sr.skipToken(resultType);
    sr.skipToken(fullName);

    sr.skipSOB();
    while (!sr.wasEOB()) {
      sr.checkString();
      if (sr.wasToken("description")) {
        if (description != null) {
          throw new ParseException(sr, "'description' set twice");
        }
        sr.skipString();
        description = sr.skipString();
      } else if (sr.wasToken("longdesc")) {
        if (longDesc != null) {
          throw new ParseException(sr, "'longdesc' set twice");
        }
        sr.skipString();
        longDesc = sr.skipString();
      } else if (sr.wasToken("sql")) {
        if (rawSQL != null) {
          throw new ParseException(sr, "'sql' set twice");
        }
        sr.skipString();
        rawSQL = sr.skipString();
      } else if (sr.wasToken("params")) {
        if (params != null) {
          throw new ParseException(sr, "'params' set twice");
        }
        params = new ArrayList();
        sr.skipString();
        sr.skipSOB();
        while (!sr.wasEOB()) {
          Field f = parseParam(sr);
          params.add(f);
          paramsByName.put(f.name, f);
        }
        sr.skipEOB();

      } else if (sr.wasToken("results")) {
        if (results != null) {
          throw new ParseException(sr, "'results' set twice");
        }
        if (isVoid) {
          throw new ParseException(sr, "Void query cannot specify results");
        }
        results = new ArrayList();
        sr.skipString();
        sr.skipSOB();
        while (!sr.wasEOB()) {
          Field f = parseParam(sr);
          results.add(f);
        }
        sr.skipEOB();

      } else if (sr.wasToken("storedproc")) {
        if (storedproc) {
          throw new ParseException(sr, "'storedproc' set twice");
        }
        sr.skipToken("storedproc");
        if (!dfnbase.cfg.hacknobefore) storedproc = true; // Unclear whether the !hacknobefore check here is deliberate or a bug

      } else if (sr.wasToken("storedfunction")) {
        if (storedfunction) {
          throw new ParseException(sr, "'storedfunction' set twice");
        }
        if (multi || isVoid) {
          throw new ParseException(sr, "Stored function must be a query single");
        }
        
        sr.skipToken("storedfunction");
        if (!dfnbase.cfg.hacknobefore) storedfunction = true; // Unclear whether the !hacknobefore check here is deliberate or a bug

      } else if (sr.wasToken("pre-upgrade-hook")) {
        if (preUpgradeHook) {
          throw new ParseException(sr, "'pre-upgrade-hook' set twice");
        }
        
        if (!isVoid) {
          throw new ParseException(sr, "Pre upgrade hook must be a void storedproc");
        }

        sr.skipToken("pre-upgrade-hook");
        preUpgradeHook = true;

      } else if (sr.wasToken("before")) {
        sr.skipToken("before");
        String step = sr.skipString();
        BeforeStmt before = new BeforeStmt();
        if ("initially".equals(step)) {
          before.upgrade = false;
          step = sr.skipString();
        } else if ("upgrade".equals(step)) {
          before.initially = false;
          step = sr.skipString();
        }
        before.step = step;
        if (!BeforeStmt.legalBefores.contains(step)) {
          throw new RuntimeException("unknown before step " + step + " in "
              + fullName);
        }
        before.name = sr.skipString();
        sr.skipToken("by");

        Expr expr = CGLParser.loadExpr(dfnFile, sr);
        before.sql = expr.evaluate(new Context(dfnbase.cfg.globalVars))
            .toString();
        if (!dfnbase.cfg.hacknobefore) {
          List blist;
          if (beforeStmts.containsKey(step)) {
            blist = (List) beforeStmts.get(step);
          } else {
            blist = new LinkedList();
            beforeStmts.put(step, blist);
          }
          blist.add(before);
        }

      } else if (sr.wasToken("extra")) {
        if (extra != null) {
          throw new ParseException(sr, "'extra' set twice");
        }
        sr.skipToken("extra");
        extra = CGLParser.loadDefinition(dfnFile, sr);
       
      } else {
        throw new ParseException(sr, "Unknown keyword '" + sr.lastToString()
            + "'");
      }
      sr.skipEOL();
    }
    sr.skipEOB();
    sr.skipEOL();
    sr.checkEOF();
    
    if (storedproc && storedfunction) {
      throw new ParseException(sr, "Cannot be both storedproc and storedfunction");
    }
    
    if (storedfunction && results.size() != 1) {
      throw new ParseException(sr, "Stored function must only have one result");
    }

    if (preUpgradeHook && !storedproc) {
      throw new ParseException(sr, "Pre upgrade hook must be a void storedproc");
    }

    if (rawSQL == null) {
      throw new ParseException(sr, "No SQL statement specified");
    }
    // if (description == null) {
    // throw new ParseException(sr, "No description specified");
    // }
    if (results == null) results = new ArrayList();
    if (params == null) params = new ArrayList();
    if (storedproc) {
      for (Iterator i = params.iterator(); i.hasNext();) {
        Field f = (Field) i.next();
        if (f.sqlType == null)
          throw new ParseException(sr, "SQL type is required for storedprocs ("
              + fullName + "." + f.name + ")");
      }
    }

    sql = scanParams(rawSQL);
    resolved = true;
  }

  Field parseParam(SmeltReader sr) throws ParseException {
    String type = sr.skipString();
    String name = sr.skipString();

    // Parse the optional SQL type.
    String sqlType = null;
    if (!sr.wasToken("nullable") && !sr.wasToken("notnull")) {
      sqlType = sr.skipString();
    }

    boolean nullable;
    if (sr.wasToken("nullable")) {
      nullable = true;
    } else if (sr.wasToken("notnull")) {
      nullable = false;
    } else {
      throw new ParseException(sr, "Expected nullable or notnull, found "
          + sr.lastToString());
    }
    sr.skipString();
    String fieldDesc = sr.skipString();
    sr.skipEOL();
    return new Field(name, type, sqlType, fieldDesc, nullable);
  }

  private static final Set availSet = new HashSet(Arrays.asList(new String[] {
      // Fields that can be populated with no load at all:
      "module", "dbobject",
      // Fields that require initial load and field-resolution:
      "description", "longdesc", "multi", "void", "sql", "rawsql", "params",
      "results", "storedproc", "storedfunction", "pre-upgrade-hook" }));
  private Map asMap;

  public Map toMap() {
    if (asMap == null) {
      asMap = new LazyMap() {
        private int state = 0;

        protected Set getAvailSet() {
          return availSet;
        }

        protected void fillIn(Object key) {
          if (key instanceof String && ((String) key).indexOf('$') >= 0)
            return;

          try {
            if (state == 0) { // Nothing loaded, no information populated
              dfnbase.cfg.global.setOnMap(backing);
              backing.put("module", module);
              backing.put("dbobject", name);
              dfnbase.cfg.local.setOnMap(this);
              state = 1;
              if (backing.containsKey(key)) return;
            }
            if (state == 1) { // Nothing loaded but basic info populated
              resolve((String) key);
              backing.put("description", description);
              backing.put("longdesc", longDesc);
              backing.put("multi", new Boolean(multi));
              backing.put("void", new Boolean(isVoid));
              backing.put("rawsql", rawSQL);
              backing.put("sql", new MappedList(sql));
              backing.put("params", new MappedList(params));
              backing.put("results", new MappedList(results));
              backing.put("storedproc", new Boolean(storedproc));
              backing.put("storedfunction", new Boolean(storedfunction));
              backing.put("pre-upgrade-hook", new Boolean(preUpgradeHook));
              ArrayList befores = new ArrayList();
              for (Iterator i = beforeStmts.values().iterator(); i.hasNext();) {
                befores.addAll((List) i.next());
              }
              backing.put("before", new MappedList(befores));
              state = 2;
              if (extra != null) extra.setOnMap(this);
              if (backing.containsKey(key)) return;
            }
          } catch (Exception e) {
            throw new CGLRuntimeException(e);
          }
        }
      };
    }
    return asMap;
  }
}
