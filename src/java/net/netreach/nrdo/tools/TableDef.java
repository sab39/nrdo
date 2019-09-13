///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.TableDef
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/09/12
// Description: Represent a table definition, and load one from a .dfn file.
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nrdo - Object-Relational database development tool.
// Copyright (c) 2000-2007 NetReach, Inc.
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
import net.netreach.smelt.LineList;
import net.netreach.smelt.LineListReader;
import net.netreach.smelt.OMParser;
import net.netreach.smelt.SmeltReader;
import net.netreach.util.FileLocatedException;
import net.netreach.util.LazyMap;
import net.netreach.util.Mappable;
import net.netreach.util.MappedList;
import net.netreach.util.Output;

/**
 * Represent a table definition, and load one from a .dfn file.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class TableDef implements Mappable, Comparable {

  String fullName;
  String name;
  String module;
  String description;
  String longDesc;
  String existingName;
  List fields = new ArrayList();
  Map fieldsByName = new HashMap();
  List pkey = new ArrayList();
  List gets = new ArrayList();
  List references = new ArrayList();
  List renamedFrom = new ArrayList();
  Definition extra;
  boolean isPkeySequenced;
  TableRef self;
  DfnBase dfnbase;
  String hash;
  long age;
  Map beforeStmts = new HashMap();

  public int compareTo(Object o) {
    return fullName.compareTo(((TableDef) o).fullName);
  }

  public Field newField(Field f) {
    return new Field(f);
  }

  public Field newField(String name, String javaType, String dbType,
      String description, boolean nullable, boolean writable) {
    return new Field(name, javaType, dbType, description, nullable, writable);
  }

  public class Field implements Mappable {
    public String name;
    public String javaType;
    public String dbType;
    public String description;
    public boolean nullable;
    public boolean writable;
    public String fullName;
    public TableRef table;
    public boolean isproperty;
    public String origName;
    public Definition fieldExtra;

    public boolean equals(Object o) {
      if (!(o instanceof Field)) return false;
      Field f = (Field) o;
      return name.equals(f.name) && table.table == f.table.table;
    }

    public int hashCode() {
      return name.hashCode() ^ System.identityHashCode(table.table);
    }

    public Field(String name, String javaType, String dbType,
        String description, boolean nullable, boolean writable) {
      this.name = name;
      this.javaType = javaType;
      this.dbType = dbType;
      this.description = description;
      this.nullable = nullable;
      this.writable = writable;
      this.fullName = name;
      this.table = self;
    }

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.putAll(table.toMap());
        asMap.put("type", javaType);
        asMap.put("sqltype", dbType);
        asMap.put("fname", name);
        asMap.put("nullable", new Boolean(nullable));
        asMap.put("writable", new Boolean(writable));
        asMap.put("fielddesc", description);
        asMap.put("pkey", new Boolean(getPkey().contains(this)));
        asMap.put("isproperty", new Boolean(isproperty));
        asMap.put("origname", origName);
        if (dbType != null && dbType.indexOf('(') >= 0) {
          asMap.put("dblength", dbType.substring(dbType.indexOf('(') + 1,
              dbType.indexOf(')')));
        } else {
          asMap.put("dblength", null);
        }
        try {
          if (fieldExtra != null)
            fieldExtra.setOnMap(asMap, dfnbase.cfg.globalVars);
        } catch (Exception e) {
          throw new RuntimeException(e.toString());
        }
      }
      return asMap;
    }

    public String toString() {
      return table.alias + "." + name;
    }

    public Field(Field f) {
      this(f.name, f.javaType, f.dbType, f.description, f.nullable, f.writable);
    }
  }

  static class SQLParam extends Util.SQLParam {
    TableDef.Field field;

    SQLParam(TableDef.Field field, String sqlbefore) {
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

  public class Get implements Mappable {
    public boolean pkey;
    public boolean multi;
    public List fields = new ArrayList();
    public List params = new ArrayList();
    public List paramsByWhere = new ArrayList();
    public Map paramsByName = new HashMap();
    public List tables = new ArrayList();
    public Map tablesByAlias = new HashMap();
    public List joins = new ArrayList();
    public List selfFields = new ArrayList();
    public List where;
    public String rawWhere;
    public String name;
    public boolean nocode;
    public boolean noindex;
    public List orderby;
    public String description;
    public Definition getExtra;

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.put("gname", name);
        asMap.put("getdesc", description);
        asMap.put("pkey", new Boolean(pkey));
        if (pkey) asMap.put("sequenced", new Boolean(isPkeySequenced()));
        asMap.put("nocode", new Boolean(nocode));
        asMap.put("noindex", new Boolean(noindex));
        asMap.put("multi", new Boolean(multi));
        asMap.put("fields", new MappedList(fields));
        asMap.put("params", new MappedList(params));
        asMap.put("tables", new MappedList(tables));
        asMap.put("joins", new MappedList(joins));
        asMap.put("where", where == null ? null : new MappedList(where));
        asMap.put("rawwhere", rawWhere);
        asMap.put("orderby", orderby == null ? (List) new ArrayList()
            : (List) new MappedList(orderby));
        try {
          if (getExtra != null)
            getExtra.setOnMap(asMap, dfnbase.cfg.globalVars);
        } catch (Exception e) {
          throw new CGLRuntimeException(e);
        }
      }
      return asMap;
    }

    public boolean indexIsUnique;

    List scanParams(String rawSQL) throws FileLocatedException, IOException {
      return Util.scanParams(rawSQL, dfnbase.cfg, paramsByName);
    }

    public void makeConsistent() throws FileLocatedException, IOException {
      if (pkey) {
        if (nocode && noindex && name != null) {
          throw new RuntimeException("nocode and noindex, so called is illegal");
        }
      } else {
        // if (fields.isEmpty() && joins.isEmpty()) noindex = true;
        if (rawWhere == null
            && (!params.isEmpty() || (!tables.isEmpty() && joins.isEmpty()))) {
          throw new RuntimeException("Can't use tables or params without where or joins");
        }
        indexIsUnique = (!multi && rawWhere == null);
        if (nocode
            && (fields.isEmpty() || !params.isEmpty() || !tables.isEmpty()
                || rawWhere != null || noindex || orderby != null)) {
          throw new RuntimeException("Illegal parameter when nocode present");
        }
      }
      if (name == null || "".equals(name)) {
        throw new RuntimeException("Nothing to get name from: "
            + "must specify called");
      }
      if (description == null) {
        description = "get by " + name;
      }
      where = scanParams(rawWhere);
      if (orderby != null) {
        for (Iterator i = orderby.iterator(); i.hasNext();) {
          OrderByClause clause = (OrderByClause) i.next();
          clause.resolve(this);
        }
      }
    }
  }

  public static class OrderByClause implements Mappable {
    private TableDef.Field field;
    private String rawSql;
    private List sql;
    private boolean descending;

    public OrderByClause(TableDef.Field field, boolean descending) {
      this.field = field;
      this.descending = descending;
    }

    public OrderByClause(String sql, boolean descending) {
      this.rawSql = sql;
      this.descending = descending;
    }

    public boolean equals(Object o) {
      return equals((OrderByClause) o);
    }

    public boolean equals(OrderByClause clause) {
      if (descending != clause.descending) return false;
      if (rawSql != null) {
        return rawSql.equals(clause.rawSql);
      } else {
        return field.equals(clause.field);
      }
    }

    public void resolve(TableDef.Get get) throws FileLocatedException, IOException {
      if (rawSql != null) sql = get.scanParams(rawSql);
    }

    public void checkOnlyOne() {
      if (field != null && !field.table.isonlyone)
        throw new RuntimeException("Cannot order by " + field.table.alias + "."
            + field.name + " because there may be more than one "
            + field.table.alias + " record associated with each result.");
    }

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.put("descending", new Boolean(descending));
        if (field != null) {
          asMap.putAll(field.toMap());
          asMap.put("sql", null);
          asMap.put("rawsql", null);
        } else {
          asMap.put("sql", new MappedList(sql));
          asMap.put("rawsql", rawSql);
          asMap.put("tblmodule", null);
          asMap.put("tbldbobject", null);
          asMap.put("tblalias", null);
          asMap.put("fname", null);
        }
      }
      return asMap;
    }
  }

  public class TableRef implements Mappable {
    public TableDef table;
    public String name;
    public String alias;
    public boolean isparam;
    public boolean isonlyone;
    public String description;

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.put("tblmodule", table.module);
        asMap.put("tbldbobject", table.name);
        asMap.put("tblexisting", table.existingName);
        asMap.put("tblalias", alias);
        asMap.put("tbldesc", description);
        asMap.put("onlyone", new Boolean(isonlyone));
      }
      return asMap;
    }

    public boolean equals(Object o) {
      if (!(o instanceof TableRef)) return false;
      TableRef tr = (TableRef) o;
      return name.equals(tr.name) && alias.equals(tr.alias)
          && isparam == tr.isparam;
    }

    public TableDef resolve() throws FileLocatedException, IOException {
      if (table == null) {
        table = dfnbase.loadFieldsOnly(name);
      }
      return table;
    }

    public void resolveGets() throws FileLocatedException, IOException {
      if (table == null) resolve();
      table.resolveGets();
    }
  }

  public class JoinAtom implements Mappable {
    public TableRef t1;
    public TableRef t2;
    public String f1;
    public String f2;

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.put("tblalias1", t1.alias);
        asMap.put("tblalias2", t2.alias);
        asMap.put("fname1", f1);
        asMap.put("fname2", f2);
        asMap.put("onlyone", new Boolean(t1.isonlyone && t2.isonlyone));
      }
      return asMap;
    }

    public boolean equals(Object o) {
      if (!(o instanceof JoinAtom)) return false;
      JoinAtom ja = (JoinAtom) o;
      return ((t1.equals(ja.t1) && f1.equals(ja.f1) && t2.equals(ja.t2) && f2
          .equals(ja.f2)) || (t1.equals(ja.t2) && f1.equals(ja.f2)
          && t2.equals(ja.t1) && f2.equals(ja.f1)));
    }
  }

  public class Reference implements Mappable {
    public String name;
    public TableDef otherTable;
    public Get get;
    public TableRef thisTable;
    public List paramTables = new ArrayList();
    public List paramJoins = new ArrayList();
    public List fieldsAndParams = new ArrayList();
    public boolean isStatic;
    public boolean fkey;
    public String fkeyName;
    public boolean cascading;
    public boolean nocode;
    public String description;
    public String rwhere;
    public List rorderby = new ArrayList();
    public List rparams = new ArrayList();
    public List rfields = new ArrayList();
    public List rtables = new ArrayList();
    public List rjoins = new ArrayList();

    private Map asMap;

    public Map toMap() {
      if (asMap == null) {
        asMap = new HashMap();
        asMap.putAll(get.toMap());
        asMap.put("rmodule", otherTable.getModule());
        asMap.put("rdbobject", otherTable.getName());
        asMap.put("rhash", otherTable.hash);
        asMap.put("rname", name);
        asMap.put("refdesc", description);
        asMap.put("nocode", new Boolean(nocode));
        asMap.put("static", new Boolean(isStatic));
        asMap.put("multi", new Boolean(get.multi));
        asMap.put("arguments", new MappedList(fieldsAndParams));
        asMap.put("fkey", new Boolean(fkey));
        asMap.put("cascading", new Boolean(cascading));
        asMap.put("fkeyname", fkeyName);
        asMap.put("rtables", new MappedList(paramTables));
        asMap.put("rwhere", rwhere);
        asMap.put("rorderby", new MappedList(rorderby));
        asMap.put("rparams", new MappedList(rparams));
        asMap.put("rfields", new MappedList(rfields));
        asMap.put("rrtables", new MappedList(rtables));
        asMap.put("rjoins", new MappedList(rjoins));

        // Construct the list of mappedFields from the fields,
        // params and paramJoins lists.
        List mappedFields = new ArrayList();

        // Iterate over the fields list of the get.
        for (Iterator fli = get.fields.iterator(); fli.hasNext();) {
          TableDef.Field f = (TableDef.Field) fli.next();

          // Search for the field in the paramJoins list. If it exists,
          // construct a Field object corresponding to the field that *joins
          // to* it. Otherwise, just insert it (from the fields list of the
          // get).
          // Note: We set nullable from the original field, not the joined one.
          // This enables the template to call getWrappedFoo or getFoo as
          // appropriate.
          for (Iterator jli = paramJoins.iterator(); jli.hasNext();) {
            TableDef.JoinAtom ja = (TableDef.JoinAtom) jli.next();
            if (ja.f1.equals(f.name) && ja.t1.equals(f.table)) {
              f = ja.t2.table
                  .newField((TableDef.Field) ja.t2.table.fieldsByName
                      .get(ja.f2));
              f.table = ja.t2;
              f.fullName = ja.t2.alias + "." + f.name;
              f.nullable = ((TableDef.Field) ja.t1.table.fieldsByName
                  .get(ja.f1)).nullable;
              f.isproperty = true;
              f.origName = ja.f1;
              break;
            } else if (ja.f2.equals(f.name) && ja.t2.equals(f.table)) {
              f = ja.t1.table
                  .newField((TableDef.Field) ja.t1.table.fieldsByName
                      .get(ja.f1));
              f.table = ja.t1;
              f.fullName = ja.t1.alias + "." + f.name;
              f.nullable = ((TableDef.Field) ja.t2.table.fieldsByName
                  .get(ja.f2)).nullable;
              f.isproperty = true;
              f.origName = ja.f2;
              break;
            }
          }
          mappedFields.add(f);
        }
        // Then (for convenience) add everything from the get's params list.
        mappedFields.addAll(get.params);
        asMap.put("mappedfields", new MappedList(mappedFields));
      }
      return asMap;
    }
  }

  public ArrayList parseOrderBy(Map tablesByAlias, List ln) {
    ArrayList orderby = new ArrayList();
    if (ln.get(1) instanceof String) {
      if (dfnbase.cfg.strict("orderby"))
        throw new RuntimeException(
            "Strict orderby parsing is active but an orderby on "
                + TableDef.this.name + " (" + ln.get(1)
                + ") is using the old syntax.");
      // Legacy parsing actually ignored any words after the first; for
      // backcompat we need to continue to do that.
      orderby.add(new OrderByClause((String) ln.get(1), false));
    } else {
      if (ln.size() != 2)
        throw new RuntimeException("Syntax error on 'orderby' line: " + ln);
      List clauses = (List) ln.get(1);
      if (clauses.isEmpty())
        throw new RuntimeException("orderby given without any clauses!");
      for (Iterator i = clauses.iterator(); i.hasNext();) {
        List clauseLine = (List) i.next();
        if (clauseLine.isEmpty() || clauseLine.size() > 3)
          throw new RuntimeException("Syntax error in orderby clause: "
              + clauseLine);
        // Legal combinations are "fieldname", "fieldname desc", "something sql"
        // or "something sql desc"

        if (clauseLine.size() >= 2 && "sql".equals(clauseLine.get(1))) {
          boolean descending = false;
          if (clauseLine.size() == 3) {
            if (!"desc".equals(clauseLine.get(2)))
              throw new RuntimeException("Expected 'desc', found "
                  + clauseLine.get(2));
            descending = true;
          }
          orderby
              .add(new OrderByClause((String) clauseLine.get(0), descending));
        } else if (clauseLine.size() == 3) {
          throw new RuntimeException("Syntax error in orderby clause: "
              + clauseLine);
        } else {
          boolean descending = false;
          if (clauseLine.size() == 2) {
            if (!"desc".equals(clauseLine.get(1)))
              throw new RuntimeException("Expected 'desc' or 'sql', found "
                  + clauseLine.get(1));
            descending = true;
          }
          String fName = (String) clauseLine.get(0);
          Field f;
          if (fName.indexOf('.') > 0) {
            String tblAlias = fName.substring(0, fName.indexOf('.'));
            String fn = fName.substring(fName.indexOf('.') + 1);
            TableRef tr = (TableRef) tablesByAlias.get(tblAlias);
            Field of = (Field) tr.table.fieldsByName.get(fn);
            if (of == null)
              throw new RuntimeException("No such field as " + fn + " in "
                  + tblAlias + " from orderby clause");
            f = new Field(of);
            f.table = tr;
            f.fullName = fName;
          } else {
            f = (Field) fieldsByName.get(fName);
            if (f == null)
              throw new RuntimeException("No such field as " + fName
                  + " in orderby clause (did you mean to specify 'sql'?)");
          }
          orderby.add(new OrderByClause(f, descending));
        }
      }
    }
    return orderby;
  }

  public void createGets(List lines, List into) throws FileLocatedException,
      IOException {
    GET: for (Iterator li = lines.iterator(); li.hasNext();) {
      List line = (List) li.next();
      Get get = new Get();
      get.multi = "multi".equals(line.get(1));
      get.name = "";
      String splitter = "";
      String uName = "";
      LineList glines = (LineList) line.get(2);
      List ln;
      List paramTables = new ArrayList();
      List paramJoins = new ArrayList();
      List fieldsAndParams = new ArrayList();
      Map pTablesByAlias = new HashMap();

      // Find the tables associated with the get.
      ln = glines.getOneByFirstWord("tables");
      if (ln != null) {
        LineList tbls = (LineList) ln.get(1);
        for (Iterator tli = tbls.iterator(); tli.hasNext();) {
          List tline = (List) tli.next();
          TableRef tr = new TableRef();
          tr.name = (String) tline.get(0);
          tr.alias = (String) tline.get(1);
          if (tr.resolve() == null) {
            // In the past nrdo permitted gets and references that could not
            // be resolved. This is generally a bad idea and can now be
            // disabled by "strict deps".
            if (dfnbase.cfg.strict("deps"))
              throw new RuntimeException("Get on " + TableDef.this.name
                  + " refers to table " + tr.name + " that cannot be found.");
            Output.println("Skipped get due to missing table " + tr.name);
            continue GET;
          }
          if (tline.size() == 3) {
            tr.description = (String) tline.get(2);
            tr.isparam = true;
            tr.isonlyone = true;
            paramTables.add(tr);
            pTablesByAlias.put(tr.alias, tr);
            get.name += splitter + tr.alias;
            splitter = "_";
          } else if (tline.size() > 2) {
            throw new RuntimeException("tables line too long");
          } else {
            get.tables.add(tr);
            get.tablesByAlias.put(tr.alias, tr);
          }
        }
      }

      // Find the fields associated with the get.
      ln = glines.getOneByFirstWord("fields");
      if (ln != null) {
        LineList flds = (LineList) ln.get(1);
        for (Iterator fli = flds.iterator(); fli.hasNext();) {
          String fName = (String) ((List) fli.next()).get(0);
          Field f;
          if (fName.indexOf('.') > 0) {
            String tblAlias = fName.substring(0, fName.indexOf('.'));
            String fn = fName.substring(fName.indexOf('.') + 1);
            TableRef tr = (TableRef) get.tablesByAlias.get(tblAlias);
            f = new Field((Field) tr.table.fieldsByName.get(fn));
            f.table = tr;
            f.fullName = fName;
          } else {
            f = (Field) fieldsByName.get(fName);
            get.selfFields.add(f);
          }
          get.fields.add(f);
          fieldsAndParams.add(f);
          get.name += splitter + f.fullName.replace('.', '_');
          splitter = "_";
          uName += "_" + f.fullName.replace('.', '_');
        }
      }

      // Find the extra parameters associated with the get.
      ln = glines.getOneByFirstWord("params");
      if (ln != null) {
        LineList prms = (LineList) ln.get(1);
        for (Iterator pli = prms.iterator(); pli.hasNext();) {
          List pline = (List) pli.next();
          String nullability = (String) pline.get(2);
          boolean nullable;
          if ("nullable".equals(nullability)) {
            nullable = true;
          } else if ("notnull".equals(nullability)) {
            nullable = false;
          } else {
            throw new RuntimeException("Must specify 'nullable' or 'notnull'");
          }
          Field f = new Field((String) pline.get(1), (String) pline.get(0),
              null, (String) pline.get(3), nullable, true);
          if (pline.size() == 5) {
            SmeltReader sr = new LineListReader((LineList) pline.get(4));
            sr.readToken();
            f.fieldExtra = CGLParser.loadDefinition(dfnFile, sr);
            sr.skipEOL();
            sr.checkEOF();
          } else if (pline.size() > 5) {
            throw new RuntimeException("Too many words on 'params' line");
          }
          get.params.add(f);
          fieldsAndParams.add(f);
          get.paramsByName.put(f.name, f);
          get.name += splitter + f.name;
          splitter = "_";
          uName += "_" + f.name;
        }
      }

      // Find the joins associated with the get (relies on tables already having
      // been defined)
      ln = glines.getOneByFirstWord("joins");
      if (ln != null) {
        LineList joins = (LineList) ln.get(1);
        for (Iterator jli = joins.iterator(); jli.hasNext();) {
          List jline = (List) jli.next();
          if (!"to".equals(jline.get(1))) {
            throw new RuntimeException("Expected 'to'");
          }
          String tbl1 = (String) jline.get(0);
          String tbl2 = (String) jline.get(2);
          LineList jfields = (LineList) jline.get(3);
          TableRef tr1 = "*".equals(tbl1) ? self : (TableRef) get.tablesByAlias
              .get(tbl1);
          TableRef tr2 = (TableRef) get.tablesByAlias.get(tbl2);
          if (tr1 == null) tr1 = (TableRef) pTablesByAlias.get(tbl1);
          if (tr2 == null) tr2 = (TableRef) pTablesByAlias.get(tbl2);
          if (tr1.isparam && tr2.isparam) {
            throw new RuntimeException("Cannot join two param tables");
          }
          for (Iterator fli = jfields.iterator(); fli.hasNext();) {
            List fline = (List) fli.next();
            JoinAtom ja = new JoinAtom();
            ja.t1 = tr1;
            ja.t2 = tr2;
            ja.f1 = (String) fline.get(0);
            ja.f2 = (String) fline.get(1);
            if (tr1.isparam) {
              Field f = new Field((Field) tr2.table.fieldsByName.get(ja.f2));
              f.table = tr2;
              if (tr2 != self) f.fullName = tbl2 + "." + ja.f2;
              get.fields.add(f);
              uName += "_" + f.fullName.replace('.', '_');
              paramJoins.add(ja);
            } else if (tr2.isparam) {
              Field f = new Field((Field) tr1.table.fieldsByName.get(ja.f1));
              f.table = tr1;
              if (tr1 != self) f.fullName = tbl1 + "." + ja.f1;
              get.fields.add(f);
              uName += "_" + f.fullName.replace('.', '_');
              paramJoins.add(ja);
            } else {
              get.joins.add(ja);
            }
            if (tr1 == self) {
              get.selfFields.add(fieldsByName.get(ja.f1));
            }
          }
        }
      }

      // Get all the other available parameters on a get.
      ln = glines.getOneByFirstWord("where");
      if (ln != null) get.rawWhere = (String) ln.get(1);

      ln = glines.getOneByFirstWord("called");
      if (ln != null) get.name = (String) ln.get(1);

      ln = glines.getOneByFirstWord("orderby");
      if (ln != null) get.orderby = parseOrderBy(get.tablesByAlias, ln);

      ln = glines.getOneByFirstWord("description");
      if (ln != null) get.description = (String) ln.get(1);

      get.noindex = (existingName != null || glines
          .getOneByFirstWord("noindex") != null);
      get.nocode = (glines.getOneByFirstWord("nocode") != null);

      ln = glines.getOneByFirstWord("extra");
      if (ln != null) {
        SmeltReader sr = new LineListReader((LineList) ln.get(1));
        sr.readToken();
        get.getExtra = CGLParser.loadDefinition(dfnFile, sr);
        sr.skipEOL();
        sr.checkEOF();
      }

      get.makeConsistent();
      if (!paramTables.isEmpty()) {
        Reference ref = new Reference();
        ref.otherTable = this;
        ref.isStatic = true;
        ref.get = get;
        ref.paramTables = paramTables;
        ref.paramJoins = paramJoins;
        ref.nocode = get.nocode;
        ref.name = "by_" + get.name;
        ref.description = get.description;
        ref.fieldsAndParams = fieldsAndParams;
        get.name = "_" + uName + "__" + get.name;
        references.add(ref);
      }

      into.add(get);
    }
  }

  public static String calcHash(String fullName) {
    StringBuffer sb = new StringBuffer(10);
    int pos = 0;
    for (int i = 0; i >= 0; i = fullName.indexOf(':', i)) {
      if (i > 0) i++;
      sb.append(fullName.substring(i, i + 1));
      pos = i;
    }
    pos = fullName.indexOf('_', pos) + 1;
    while (pos > 0) {
      sb.append(fullName.substring(pos, pos + 1));
      pos = fullName.indexOf('_', pos) + 1;
    }
    int hc = fullName.hashCode();
    short sc = (short) (hc ^ hc >> 16);
    String hx = Integer.toHexString(sc);
    if (sc < 0) {
      sb.append(hx.substring(4));
    } else {
      sb.append("0000");
      int len = sb.length();
      sb.replace(len - hx.length(), len, hx);
    }
    return sb.toString();
  }

  public TableDef(DfnBase dfnbase, File file, String fullName) {
    this.dfnbase = dfnbase;
    if (file != null) this.age = file.lastModified();
    this.dfnFile = file;
    this.fullName = fullName;
    int colonpos = fullName.lastIndexOf(':');
    name = fullName.substring(colonpos + 1);
    module = (colonpos == -1) ? "" : fullName.substring(0, colonpos);
    hash = calcHash(fullName);

    // Construct the 'self' TableRef that will be used to represent '*' in joins
    self = new TableRef();
    self.table = this;
    self.name = fullName;
    self.alias = "self";
    self.isparam = false;
    self.isonlyone = true;
  }

  LineList ll = null;
  private boolean fieldsResolved = false;
  public final File dfnFile;

  public void resolveFields() throws FileLocatedException, IOException {
    resolveFields(null);
  }

  public void resolveFields(String reason) throws FileLocatedException, IOException {

    if (fieldsResolved) return;
    String whystr = reason == null ? "" : " (due to " + reason + ")";

    Output.println("Loading table " + fullName + whystr);

    // Load the definition file.
    List dfnLine = OMParser.parseLine(dfnFile);

    // Check the basic structure of the file.
    if (!(dfnLine.size() == 3 && "table".equals(dfnLine.get(0))
        && dfnLine.get(1) instanceof String && dfnLine.get(2) instanceof LineList)) {
      throw new RuntimeException("File not of form 'table <nm> { ... };");
    }

    // Get the single-stringed attributes...
    if (!fullName.equalsIgnoreCase((String) dfnLine.get(1))) {
      throw new RuntimeException("Wrong table name found in " + fullName + " ("
          + dfnLine.get(1) + ")");
    }
    fullName = (String) dfnLine.get(1);
    ll = (LineList) dfnLine.get(2);
    List dline = ll.getOneByFirstWord("description");
    description = (dline == null) ? "" : (String) dline.get(1);
    dline = ll.getOneByFirstWord("longdesc");
    longDesc = (dline == null) ? "" : (String) dline.get(1);
    List x = ll.getOneByFirstWord("extra");
    if (x != null) {
      SmeltReader sr = new LineListReader((LineList) x.get(1));
      sr.readToken();
      extra = CGLParser.loadDefinition(dfnFile, sr);
      sr.skipEOL();
      sr.checkEOF();
    }
    ;

    // Get the 'existing' line, if any.
    List existLine = ll.getOneByFirstWord("existing");
    if (existLine != null) {
      if (existLine.size() == 1) {
        existingName = fullName.replace(':', '_');
      } else if ("as".equals(existLine.get(1)) && existLine.size() == 3) {
        existingName = (String) existLine.get(2);
      } else {
        throw new RuntimeException(
            "Expected 'existing;' or 'existing as <name>;', found " + existLine);
      }
    }

    // Parse any 'renamed from' lines that indicate table renaming.
    List renameds = ll.getAllByFirstWord("renamed");
    for (Iterator ri = renameds.iterator(); ri.hasNext();) {
      List ren = (List) ri.next();
      if (ren.size() != 3 || !"from".equals(ren.get(1))) {
        throw new RuntimeException("Expected 'renamed from <name>;', found "
            + ren);
      }
      renamedFrom.add(0, ren.get(2));
    }

    // Parse any 'before' lines that provide code to run during table creation.
    if (!dfnbase.cfg.hacknobefore) {
      List befores = ll.getAllByFirstWord("before");
      for (Iterator bi = befores.iterator(); bi.hasNext();) {
        List bef = (List) bi.next();
        String step = (String) bef.get(1);
        int offset = 2;
        BeforeStmt before = new BeforeStmt();
        if ("initially".equals(step)) {
          before.upgrade = false;
          step = (String) bef.get(2);
          offset++;
        } else if ("upgrade".equals(step)) {
          before.initially = false;
          step = (String) bef.get(2);
          offset++;
        }
        before.step = step;
        if (!BeforeStmt.legalBefores.contains(step)) {
          throw new RuntimeException("unknown before step " + step + " in "
              + fullName);
        }
        before.name = (String) bef.get(offset);
        if (!"by".equals(bef.get(offset + 1))) {
          throw new RuntimeException("expected 'by', found '" + bef.get(3)
              + "' in before " + before.name + " in " + fullName);
        }
        LineList ll = new LineList();
        ll.add(bef.subList(offset + 2, bef.size()));
        SmeltReader sr = new LineListReader(ll);
        sr.readToken();
        Expr expr = CGLParser.loadExpr(dfnFile, sr);
        before.sql = expr.evaluate(new Context(dfnbase.cfg.globalVars))
            .toString();
        List blist;
        if (beforeStmts.containsKey(step)) {
          blist = (List) beforeStmts.get(step);
        } else {
          blist = new LinkedList();
          beforeStmts.put(step, blist);
        }
        blist.add(before);
      }
    }

    // Parse the 'fields' entry in the file.
    List rawFields = (List) (ll.getOneByFirstWord("fields").get(1));
    for (Iterator li = rawFields.iterator(); li.hasNext();) {
      List fld = (List) li.next();
      if (fld.size() < 6 || fld.size() > 7) {
        throw new RuntimeException("fields must have 6 or 7 terms: " + fullName
            + "/" + fld);
      }
      String nullability = (String) fld.get(3);
      String readability = (String) fld.get(4);
      boolean nullable;
      boolean readonly;
      if ("nullable".equals(nullability)) {
        nullable = true;
      } else if ("notnull".equals(nullability)) {
        nullable = false;
      } else {
        throw new RuntimeException("Must specify 'nullable' or 'notnull'");
      }
      if ("readonly".equals(readability)) {
        readonly = true;
      } else if ("readwrite".equals(readability)) {
        readonly = false;
      } else {
        throw new RuntimeException("Must specify 'readonly' or 'readwrite'");
      }
      Field f = new Field((String) fld.get(1), (String) fld.get(0),
          dfnbase.cfg.dbtypemap.map((String) fld.get(2)), (String) fld.get(5),
          nullable, !readonly);
      if (fieldsByName.containsKey(f.name)) {
        throw new RuntimeException("Duplicate field " + f.name);
      }
      if (fld.size() == 7) {
        SmeltReader sr = new LineListReader((LineList) fld.get(6));
        sr.readToken();
        f.fieldExtra = CGLParser.loadDefinition(dfnFile, sr);
        sr.skipEOL();
        sr.checkEOF();
      }
      fields.add(f);
      fieldsByName.put(f.name, f);
    }

    // Identify the primary key and whether it is sequenced or not.
    List pkeyLine = ll.getOneByFirstWord("pkey");
    Get pkeyGet = new Get();
    pkeyGet.pkey = true;
    LineList info = null;
    if ("sequenced".equals(pkeyLine.get(1))) {
      Field f = (Field) fieldsByName.get(pkeyLine.get(2));
      if (f.nullable || f.writable) {
        throw new RuntimeException("Pk fields must be readonly notnull");
      }
      pkey.add(f);
      pkeyGet.name = f.name;
      isPkeySequenced = true;
      if (pkeyLine.size() > 3) info = (LineList) pkeyLine.get(3);
      if (pkeyLine.size() > 4) {
        throw new RuntimeException("Expected EOL on pkey");
      }
    } else {
      LineList pkeyFields = (LineList) pkeyLine.get(1);
      String splitter = "";
      pkeyGet.name = "";
      for (Iterator li = pkeyFields.iterator(); li.hasNext();) {
        Field f = (Field) fieldsByName.get(((List) li.next()).get(0));
        if (f.nullable || f.writable) {
          throw new RuntimeException("Pk fields must be readonly notnull");
        }
        pkey.add(f);
        pkeyGet.name += splitter + f.name;
        splitter = "_";
      }
      isPkeySequenced = false;
      if (pkeyLine.size() > 2) info = (LineList) pkeyLine.get(2);
      if (pkeyLine.size() > 3) {
        throw new RuntimeException("Expected EOL on pkey");
      }
    }
    pkeyGet.fields = pkeyGet.selfFields = pkey;
    if (info != null) {
      if (existingName != null || info.getOneByFirstWord("noindex") != null)
        pkeyGet.noindex = true;
      if (info.getOneByFirstWord("nocode") != null) pkeyGet.nocode = true;
      List called = info.getOneByFirstWord("called");
      if (called != null) pkeyGet.name = (String) called.get(1);

      List ln = info.getOneByFirstWord("extra");
      if (ln != null) {
        SmeltReader sr = new LineListReader((LineList) ln.get(1));
        sr.readToken();
        pkeyGet.getExtra = CGLParser.loadDefinition(dfnFile, sr);
        sr.skipEOL();
        sr.checkEOF();
      }
    }
    pkeyGet.makeConsistent();

    // Construct the lists of single and multiple gets in the table.
    if (!(pkeyGet.noindex && pkeyGet.nocode)) gets.add(pkeyGet);

    fieldsResolved = true;
  }

  private boolean getsResolved;

  public void resolveGets() throws FileLocatedException, IOException {
    if (getsResolved) return;
    resolveFields();
    if (dfnbase.verbose) Output.println("Resolving gets " + fullName);
    createGets(ll.getAllByFirstWord("get"), gets);
    getsResolved = true;
  }

  private boolean getsFullyResolved;

  public void resolveGetsFully() throws FileLocatedException, IOException {
    if (getsFullyResolved) return;
    resolveGets();
    if (dfnbase.verbose) Output.println("Tweaking gets " + fullName);

    // Resolve all other known tables.
    for (Iterator gli = gets.iterator(); gli.hasNext();) {
      Get g = (Get) gli.next();
      for (Iterator tli = g.tables.iterator(); tli.hasNext();) {
        ((TableRef) tli.next()).resolveGets();
      }
    }

    // Scan the gets and calculate which tables are "only-one" tables - tables
    // for which, in this get, there will only ever be one row per row in this
    // table.
    for (Iterator i = gets.iterator(); i.hasNext();) {
      Get g = (Get) i.next();

      // Construct a set of fields that we know we only have one value of.
      Set uFields = new HashSet();
      for (Iterator fi = g.fields.iterator(); fi.hasNext();) {
        Field f = (Field) fi.next();
        if (f.table != self) uFields.add(f);
      }

      // Repeatedly try to grow the set of only-one tables until it cannot grow
      // further. This set starts out as the current table (self.isonlyone is
      // always true) plus any param tables which have already had isonlyone set
      boolean grew = true;
      while (grew) {
        grew = false;
        for (Iterator ji = g.joins.iterator(); ji.hasNext();) {
          JoinAtom ja = (JoinAtom) ji.next();

          // Add all fields joined to only-one tables to the list of known
          // single-valued fields.
          TableRef tr = null;
          String fName = null;
          if (ja.t1.isonlyone && !ja.t2.isonlyone) {
            tr = ja.t2;
            fName = ja.f2;
          } else if (ja.t2.isonlyone && !ja.t1.isonlyone) {
            tr = ja.t1;
            fName = ja.f1;
          }
          if (tr != null) {
            Field f = new Field((Field) tr.table.fieldsByName.get(fName));
            f.table = tr;
            uFields.add(f);
          }
        }

        // Attempt to find any new tables that are unique by the set of known
        // single-valued fields. If any such table is found, this table must be
        // an only-one table.
        for (Iterator ti = g.tables.iterator(); ti.hasNext();) {
          TableRef tr = (TableRef) ti.next();
          if (!tr.isonlyone && tr.table.isUniqueBy(uFields)) {
            grew = true;
            tr.isonlyone = true;
          }
        }
      }

      if (g.orderby != null) {
        for (Iterator obi = g.orderby.iterator(); obi.hasNext();) {
          OrderByClause clause = (OrderByClause) obi.next();
          clause.checkOnlyOne();
        }
      }
    }
    getsFullyResolved = true;
  }

  public List indexes;
  public Map indexMap;

  public void resolveIndexes() throws FileLocatedException, IOException {
    if (indexes != null) return;
    resolveGets();
    if (dfnbase.verbose) Output.println("Resolving indexes " + fullName);

    List uniques = new ArrayList();
    List multis = new ArrayList();
    for (Iterator li = gets.iterator(); li.hasNext();) {
      Get get = (Get) li.next();
      if (!get.noindex && !get.selfFields.isEmpty()) {
        Index ix = new Index();
        for (Iterator gli = get.selfFields.iterator(); gli.hasNext();) {
          Field f = (Field) gli.next();
          ix.add(f);
        }
        if (get.multi || get.where != null) {
          if (get.where == null && get.orderby != null) {
            for (Iterator obi = get.orderby.iterator(); obi.hasNext();) {
              OrderByClause clause = (OrderByClause) obi.next();
              if (clause.field != null && clause.field.table == self) {
                ix.cut();
                ix.add(clause.field);
              } else {
                break;
              }
            }
          }
          multis.add(ix);
        } else {
          ix.isUnique = true;
          uniques.add(ix);
        }
        ix.name = (get.pkey ? "pk_" : ix.isUnique ? "uk_" : "ix_") + hash + "_"
            + calcHash(get.name);
      }
    }
    uniques = Index.trimUniques(uniques);
    uniques.addAll(Index.trimMultis(uniques, multis));

    indexes = new ArrayList(uniques);
    indexMap = new HashMap();
    for (Iterator i = uniques.iterator(); i.hasNext();) {
      Index ind = (Index) i.next();
      if (indexMap.containsKey(ind.name)) {
        Output.println("EEK, added dup of " + ind.name);
      }
      indexMap.put(ind.name, ind);
    }
  }

  public void resolveRefs() throws FileLocatedException, IOException {
    if (getsFullyResolved && ll == null) return;
    resolveGetsFully();
    if (dfnbase.verbose) Output.println("Resolving refs " + fullName);

    // Parse the references and match them up to the relevant gets on the other
    // table.
    REF: for (Iterator rli = ll.getAllByFirstWord("references").iterator(); rli
        .hasNext();) {
      List line = (List) rli.next();
      boolean multi = "multi".equals(line.get(1));

      TableDef otherTd = dfnbase.loadGetsFully((String) line.get(2));
      if (otherTd == null) {
        // In the past nrdo permitted gets and references that could not
        // be resolved. This is generally a bad idea and can now be
        // disabled by "strict deps".
        if (dfnbase.cfg.strict("deps"))
          throw new RuntimeException("Get on " + TableDef.this.name
              + " refers to table " + line.get(2) + " that cannot be found.");
        Output
            .println("Skipped reference due to missing table " + line.get(2));
        continue;
      }
      TableRef otherTable = otherTd.self;
      TableRef thisTable = new TableRef();
      thisTable.name = fullName;
      thisTable.alias = "this";
      thisTable.isparam = true;
      thisTable.table = this;
      thisTable.isonlyone = true;

      Reference ref = new Reference();
      ref.otherTable = otherTd;
      ref.thisTable = thisTable;
      String where = null;
      ArrayList orderby = null;
      List params = new ArrayList();
      List fields = new ArrayList();
      List tables = new ArrayList();
      Map tablesByAlias = new HashMap();
      List joins = new ArrayList();

      LineList lines = (LineList) line.get(3);
      List ln;

      // Identify the extra parameters associated with this reference.
      ln = lines.getOneByFirstWord("params");
      if (ln != null) {
        LineList prms = (LineList) ln.get(1);
        for (Iterator pli = prms.iterator(); pli.hasNext();) {
          List pline = (List) pli.next();
          String nullability = (String) pline.get(2);
          boolean nullable;
          if ("nullable".equals(nullability)) {
            nullable = true;
          } else if ("notnull".equals(nullability)) {
            nullable = false;
          } else {
            throw new RuntimeException("Must specify 'nullable' or 'notnull'");
          }
          Field f = new Field((String) pline.get(1), (String) pline.get(0),
              null, (String) pline.get(3), nullable, true);
          f.table = otherTable;
          params.add(f);
          ref.fieldsAndParams.add(f);
          ref.rparams.add(f);
        }
      }

      // Identify the tables associated with this reference, excluding this
      // table.
      ln = lines.getOneByFirstWord("tables");
      if (ln != null) {
        LineList tbls = (LineList) ln.get(1);
        for (Iterator tli = tbls.iterator(); tli.hasNext();) {
          List tline = (List) tli.next();
          TableRef tr = new TableRef();
          tr.name = (String) tline.get(0);
          tr.alias = (String) tline.get(1);
          if (tr.resolve() == null) {
            // In the past nrdo permitted gets and references that could not
            // be resolved. This is generally a bad idea and can now be
            // disabled by "strict deps".
            if (dfnbase.cfg.strict("deps"))
              throw new RuntimeException("Get on " + TableDef.this.name
                  + " refers to table " + tr.name + " that cannot be found.");
            Output.println("Skipped ref due to missing table " + tr.name);
            continue REF;
          }
          if (tline.size() == 3) {
            tr.description = (String) tline.get(2);
            tr.isparam = true;
            tr.isonlyone = true;
            ref.paramTables.add(tr);
          } else if (tline.size() > 2) {
            throw new RuntimeException("tables line too long");
          } else {
            tables.add(tr);
          }
          tablesByAlias.put(tr.alias, tr);
          ref.rtables.add(tr);
          tr.resolve();
        }
      }

      // Identify the fields associated with this reference.
      ln = lines.getOneByFirstWord("fields");
      if (ln != null) {
        LineList flds = (LineList) ln.get(1);
        for (Iterator fli = flds.iterator(); fli.hasNext();) {
          String fName = (String) ((List) fli.next()).get(0);
          Field f;
          if (fName.indexOf('.') > 0) {
            String tblAlias = fName.substring(0, fName.indexOf('.'));
            String fn = fName.substring(fName.indexOf('.') + 1);
            TableRef tr = (TableRef) tablesByAlias.get(tblAlias);
            f = new Field((Field) tr.table.fieldsByName.get(fn));
            f.table = tr;
            f.fullName = fName;
          } else {
            f = (Field) otherTd.fieldsByName.get(fName);
          }
          fields.add(f);
          ref.rfields.add(f);
          ref.fieldsAndParams.add(f);
        }
      }

      // Identify the joins associated with this reference (simple form).
      ln = lines.getOneByFirstWord("by");
      if (ln != null) {
        LineList jns = (LineList) ln.get(1);
        for (Iterator jli = jns.iterator(); jli.hasNext();) {
          List jline = (List) jli.next();
          JoinAtom ja = new JoinAtom();
          ja.t1 = thisTable;
          ja.t2 = otherTable;
          ja.f1 = (String) jline.get(0);
          ja.f2 = (String) jline.get(1);
          ref.paramJoins.add(ja);
          Field f = (Field) otherTd.fieldsByName.get(ja.f2);
          fields.add(f);
          ref.rjoins.add(ja);
        }
      }

      // Identify the joins associated with this reference (full form).
      ln = lines.getOneByFirstWord("joins");
      if (ln != null) {
        LineList jns = (LineList) ln.get(1);
        for (Iterator jli = jns.iterator(); jli.hasNext();) {
          List jline = (List) jli.next();
          if (!"to".equals(jline.get(1))) {
            throw new RuntimeException("Expected 'to'");
          }
          String tbl1 = (String) jline.get(0);
          String tbl2 = (String) jline.get(2);
          LineList jfields = (LineList) jline.get(3);
          TableRef tr1 = "*".equals(tbl1) ? thisTable
              : (TableRef) tablesByAlias.get(tbl1);
          TableRef tr2 = "*".equals(tbl2) ? otherTable
              : (TableRef) tablesByAlias.get(tbl2);
          for (Iterator fli = jfields.iterator(); fli.hasNext();) {
            List fline = (List) fli.next();
            JoinAtom ja = new JoinAtom();
            ja.t1 = tr1;
            ja.t2 = tr2;
            ja.f1 = (String) fline.get(0);
            ja.f2 = (String) fline.get(1);
            if (tr1.isparam && tr2.isparam) {
              throw new RuntimeException("Cannot join two param tables");
            } else if (tr1.isparam || tr2.isparam) {
              ref.paramJoins.add(ja);
              TableRef jtr = tr1.isparam ? tr2 : tr1;
              String jfn = tr1.isparam ? ja.f2 : ja.f1;
              Field f = new Field((Field) jtr.table.fieldsByName.get(jfn));
              f.table = jtr;
              if (jtr != otherTable) {
                f.fullName = jtr.alias + "." + jfn;
              }
              fields.add(f);
            } else {
              joins.add(ja);
            }
            ref.rjoins.add(ja);
          }
        }
      }

      // Identify the other parameters associated with this reference.
      ln = lines.getOneByFirstWord("where");
      if (ln != null) {
        where = (String) ln.get(1);
        ref.rwhere = where;
      }

      ln = lines.getOneByFirstWord("called");
      if (ln != null) ref.name = (String) ln.get(1);

      ln = lines.getOneByFirstWord("orderby");
      if (ln != null) {
        orderby = otherTd.parseOrderBy(tablesByAlias, ln);
        ref.rorderby = orderby;
      }

      ln = lines.getOneByFirstWord("description");
      if (ln != null) ref.description = (String) ln.get(1);

      ref.nocode = (lines.getOneByFirstWord("nocode") != null);

      ln = lines.getOneByFirstWord("fkey");
      if (ln != null) {
        ref.fkey = true;
        ref.cascading = ln.size() > 1 && "cascade".equals(ln.get(1));
      }

      if (ref.fkey
          && (!tables.isEmpty() || !params.isEmpty()
              || fields.size() != ref.paramJoins.size() || where != null)) {
        throw new RuntimeException("Illegal argument with fkey");
      }
      if (ref.fkey && ref.otherTable.existingName != null) {
        throw new RuntimeException(
            "Fkey cannot be used against 'existing' tables");
      }
      String defaultMinName = "";
      String splitter = "";
      for (Iterator li = ref.paramTables.iterator(); li.hasNext();) {
        defaultMinName += splitter + ((TableRef) li.next()).alias;
        splitter = "_";
      }
      for (Iterator li = ref.fieldsAndParams.iterator(); li.hasNext();) {
        defaultMinName += splitter + ((Field) li.next()).name;
        splitter = "_";
      }
      String defaultName = otherTable.table.name;
      if (multi) defaultName += "s";
      if (!"".equals(defaultMinName)) defaultName += "_by_" + defaultMinName;
      
      if (ref.name == null) ref.name = defaultName;

      if (ref.description == null) {
        ref.description = "get " + ref.name;
      }

      // Go and find the get on the other table that corresponds to this ref.
      for (Iterator gli = otherTd.gets.iterator(); gli.hasNext();) {
        Get get = (Get) gli.next();
        boolean diagnose = get.name.endsWith(ref.name);
        if (get.nocode && !ref.nocode) {
          diag(diagnose, "Nocode");
          continue;
        }
        if (get.noindex && ref.fkey) {
          diag(diagnose, "noindex/fkey");
          continue;
        }
        if (where == null ? get.rawWhere != null : !where.equals(get.rawWhere)) {
          {
            diag(diagnose, "where");
            continue;
          }
        }
        if (orderby != null && !orderby.equals(get.orderby)) {
          diag(diagnose, "orderby");
          continue;
        }

        if (params.size() != get.params.size()
            || !params.containsAll(get.params)) {
          diag(diagnose, "params");
          continue;
        }

        // "Fields" now includes fields joined to param tables.
        if (fields.size() != get.fields.size()
            || !fields.containsAll(get.fields)) {
          diag(diagnose, "fields " + fields + get.fields);
          continue;
        }

        if (tables.size() != get.tables.size()
            || !tables.containsAll(get.tables)) {
          diag(diagnose, "tables");
          continue;
        }

        if (joins.size() != get.joins.size() || !joins.containsAll(get.joins)) {
          diag(diagnose, "joins");
          continue;
        }

        ref.get = get;
        break;
      }
      if (ref.get == null) {
        throw new RuntimeException("no matching get for reference: "
            + this.fullName + "/" + otherTd.fullName + "/" + ref.name);
      }
      if (ref.fkey) {
        ref.fkeyName = "fk_" + ref.thisTable.table.hash + "_"
            + ref.otherTable.hash;
        if (!ref.name.equals(defaultName)) {
          ref.fkeyName += "_" + ref.name;
        } else if (!"".equals(defaultMinName)) {
          ref.fkeyName += "_" + defaultMinName;
        }
      }
      references.add(ref);
    }
    ll = null;
    if (dfnbase.verbose) Output.println("Fully resolved table " + fullName);
  }

  void diag(boolean diag, String msg) {
    if (diag) Output.println("Match failed due to " + msg);
  }

  boolean isUniqueBy(Set fields) {
    for (Iterator i = gets.iterator(); i.hasNext();) {
      Get g = (Get) i.next();
      if (!g.multi && g.rawWhere == null && fields.containsAll(g.selfFields)) {
        return true;
      }
    }
    return false;
  }

  private static final Set availSet = new HashSet(Arrays.asList(new String[] {
      // Fields that can be populated with no load at all:
      "module", "dbobject", "hash", "modparts",
      // Fields that require initial load and field-resolution:
      "description", "longdesc", "existing_table", "sequenced", "fields",
      "indexes", "before", "renamed",
      // Fields that require references to be resolved:
      "gets", "references" }));
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
              backing.put("module", getModule());
              backing.put("dbobject", getName());
              backing.put("hash", hash);
              List modparts = new ArrayList();
              String[] parts = module.split(":");
              String cumul = "";
              for (int i = 0; i < parts.length; i++) {
                String part = parts[i];
                if (cumul.length() != 0) cumul += ':';
                cumul += part;
                Map map = new HashMap();
                map.put("part", part);
                map.put("cumul", cumul);
                modparts.add(map);
              }
              backing.put("modparts", modparts);
              dfnbase.cfg.local.setOnMap(this);
              state = 1;
              if (backing.containsKey(key)) return;
            }
            if (state == 1) { // Nothing loaded but basic info populated
              resolveFields((String) key);
              backing.put("description", getDescription());
              backing.put("longdesc", getLongDesc());
              backing.put("existing_table", existingName);
              backing.put("sequenced", new Boolean(isPkeySequenced()));
              backing.put("fields", new MappedList(fields));

              ArrayList befores = new ArrayList();
              for (Iterator i = beforeStmts.values().iterator(); i.hasNext();) {
                befores.addAll((List) i.next());
              }
              backing.put("before", new MappedList(befores));

              ArrayList ren = new ArrayList();
              for (Iterator i = renamedFrom.iterator(); i.hasNext();) {
                HashMap map = new HashMap();
                map.put("from", i.next());
                ren.add(map);
              }
              backing.put("renamed", ren);

              state = 2;
              if (extra != null) extra.setOnMap(this);
              if (backing.containsKey(key)) return;
            }
            if (!availSet.contains(key)) return;

            // Only load indexes if they are specifically asked for, because
            // they aren't a requirement for anything else.
            if ("indexes".equals(key) && !backing.containsKey(key)) {
              resolveIndexes();
              backing.put("indexes", new MappedList(indexes));
              return;
            }

            if (state == 2) { // Fields loaded
              resolveGetsFully();
              backing.put("gets", new MappedList(gets));
              state = 3;
              if (backing.containsKey(key)) return;
            }
            if (state == 3) { // Gets loaded
              resolveRefs();
              backing.put("references", new MappedList(references));
              state = 4;
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

  public String toString() {
    return "Table " + name + ": " + description + fields + "\nPrimary key is "
        + (isPkeySequenced ? "sequenced " : "") + pkey;
  }

  public String getName() {
    return name;
  }

  public String getModule() {
    return module;
  }

  public List getFields() {
    return fields;
  }

  public String getDescription() {
    return description;
  }

  public String getLongDesc() {
    return longDesc;
  }

  public boolean isPkeySequenced() {
    return isPkeySequenced;
  }

  public List getPkey() {
    return pkey;
  }

  public List getGets() {
    return gets;
  }

  public List getReferences() {
    return references;
  }
}
