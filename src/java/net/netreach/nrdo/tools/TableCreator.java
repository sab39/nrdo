///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.TableCreator
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/10/23
// Description: Create tables on the DB server, or update them with changes.
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
import java.io.BufferedReader;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.net.InetAddress;
import java.sql.Connection;
import java.sql.Driver;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Set;

import net.netreach.smelt.LineList;
import net.netreach.smelt.OMParser;
import net.netreach.smelt.ParseException;
import net.netreach.util.CVSDir;
import net.netreach.util.CVSFile;
import net.netreach.util.FileLocatedException;
import net.netreach.util.Output;
import net.netreach.util.PathUtil;

/**
 * Create tables on the DB server, or update them with changes.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class TableCreator {

  private String tableNameToSQL(String fullName) {
    String base = "";
    if (cfg.schema != null) base = cfg.schema + ".";
    return base + fullName.replace(':', '_');
  }

  public class Field {
    public Tbl tbl;
    public String name;
    public String dbType;
    public boolean nullable;
    public boolean identity;

    public boolean equals(Object o) {
      if (o == null || !(o instanceof Field)) return false;
      return name.equals(((Field) o).name)
          && dbType.equals(((Field) o).dbType)
          && (!cfg.dbadapter.usesIdentity() || identity == ((Field) o).identity);
    }

    // New fields are always created nullable and modified to notnull later.
    public String toAddString() {
      return cfg.dbadapter.getAddFieldSql(this);
    }

    public String toDropString() {
      return cfg.dbadapter.getDropFieldSql(this);
    }

    public String toNotNullString() {
      return cfg.dbadapter.getSetFieldNotNullSql(this);
    }

    public String toNullString() {
      return cfg.dbadapter.getSetFieldNullSql(this);
    }

    public String toAlterString() {
      return cfg.dbadapter.getAlterFieldTypeSql(this);
    }

    public String toString() {
      return "  " + name + " " + dbType + (nullable ? " nullable" : " notnull")
          + (identity ? " identity" : "") + ";\n";
    }

    public Field(Tbl tbl, List line) {
      this.tbl = tbl;
      name = (String) line.get(0);
      dbType = (String) line.get(1);
      nullable = "nullable".equals(line.get(2));
      identity = line.size() > 3 && "identity".equals(line.get(3));
    }

    public Field(Tbl tbl, TableDef.Field f) {
      this.tbl = tbl;
      name = f.name;
      dbType = f.dbType;
      nullable = f.nullable;
      if (f.table.table.isPkeySequenced && f.table.table.pkey.contains(f)) {
        identity = true;
      }
    }

    public Field(Tbl tbl, Field f) {
      this.tbl = tbl;
      name = f.name;
      dbType = f.dbType;
      nullable = f.nullable;
      identity = f.identity;
    }
  }

  public class Key {
    public Tbl tbl;
    public String name;
    public List fields;
    public int type;
    public static final int TYPE_PK = 1;
    public static final int TYPE_UK = 2;
    public static final int TYPE_IX = 3;

    public boolean equals(Object o) {
      if (o == null || !(o instanceof Key)) return false;

      return name.equals(((Key) o).name) && type == ((Key) o).type
          && fields.equals(((Key) o).fields);
    }

    public String toAddString() {
      if (type == TYPE_IX) {
        return cfg.dbadapter.getCreateIndexSql(this);
      } else {
        return cfg.dbadapter.getCreateUniqueSql(this);
      }
    }

    public String toDropString() {
      if (type == TYPE_IX) {
        return cfg.dbadapter.getDropIndexSql(this);
      } else {
        return cfg.dbadapter.getDropUniqueSql(this);
      }
    }

    public String toString() {
      StringBuffer sb = new StringBuffer("  "
          + (type == TYPE_PK ? "pk " : type == TYPE_UK ? "uk " : "ix ") + name
          + " { ");
      for (Iterator li = fields.iterator(); li.hasNext();) {
        sb.append(((Field) li.next()).name);
        sb.append("; ");
      }
      sb.append("};\n");
      return sb.toString();
    }

    public Key(Tbl tbl, List line) {
      this.tbl = tbl;
      name = (String) line.get(1);
      type = "pk".equals(line.get(0)) ? TYPE_PK
          : "uk".equals(line.get(0)) ? TYPE_UK : TYPE_IX;
      LineList f = (LineList) line.get(2);
      fields = new ArrayList();
      for (Iterator li = f.iterator(); li.hasNext();) {
        fields.add(tbl.fieldMap.get(((List) li.next()).get(0)));
      }
    }

    public Key(Tbl tbl, Index ix) {
      this.tbl = tbl;
      name = ix.name;
      type = name.startsWith("pk") ? TYPE_PK : name.startsWith("uk") ? TYPE_UK
          : TYPE_IX;
      fields = new ArrayList(ix);
    }

    public Key(Tbl tbl, Key key) {
      this.tbl = tbl;
      name = key.name;
      type = key.type;
      fields = new ArrayList();
      for (Iterator li = key.fields.iterator(); li.hasNext();) {
        fields.add(new Field(tbl, (Field) li.next()));
      }
    }
  }

  public class FFld {
    public FKey fkey;
    public Field thisFld;
    public String otherFld;

    public FFld(FKey fkey, List line) {
      this.fkey = fkey;
      String fieldName = (String) line.get(0);
      thisFld = (Field) fkey.tbl.fieldMap.get(fieldName);
      otherFld = (String) line.get(1);
    }

    public FFld(FKey fkey, TableDef.JoinAtom ja) {
      this.fkey = fkey;
      thisFld = (Field) fkey.tbl.fieldMap.get(ja.f1);
      otherFld = ja.f2;
    }

    public FFld(FKey fkey, FFld ffld) {
      this.fkey = fkey;
      thisFld = (Field) fkey.tbl.fieldMap.get(ffld.thisFld.name);
      otherFld = ffld.otherFld;
    }

    public boolean equals(Object o) {
      if (o == null || !(o instanceof FFld)) return false;
      return thisFld.equals(((FFld) o).thisFld)
          && otherFld.equals(((FFld) o).otherFld);
    }
  }

  public class FKey {
    public Tbl tbl;
    public String name;
    public String otherTable;
    public List flds;
    public boolean cascading;

    public boolean equals(Object o) {
      if (o == null || !(o instanceof FKey)) return false;
      return name.equals(((FKey) o).name)
          && otherTable.equals(((FKey) o).otherTable)
          && flds.equals(((FKey) o).flds) && cascading == ((FKey) o).cascading;
    }

    public String toAddString() {
      return cfg.dbadapter.getAddFkeySql(this);
    }

    public String toDropString() {
      return cfg.dbadapter.getDropFkeySql(this);
    }

    public String toString() {
      StringBuffer sb = new StringBuffer("  " + name + " " + otherTable + " { ");
      for (Iterator li = flds.iterator(); li.hasNext();) {
        FFld ff = (FFld) li.next();
        sb.append(ff.thisFld.name + " " + ff.otherFld + "; ");
      }
      sb.append("}" + (cascading ? " cascade" : "") + ";\n");
      return sb.toString();
    }

    public FKey(Tbl tbl, List line) {
      this.tbl = tbl;
      name = (String) line.get(0);
      otherTable = (String) line.get(1);
      LineList f = (LineList) line.get(2);
      cascading = line.size() > 3 && "cascade".equals(line.get(3));
      flds = new ArrayList();
      for (Iterator li = f.iterator(); li.hasNext();) {
        flds.add(new FFld(this, (List) li.next()));
      }
    }

    public FKey(Tbl tbl, TableDef.Reference r) throws IOException,
        FileLocatedException {
      this.tbl = tbl;
      name = r.fkeyName;
      cascading = r.cascading;
      otherTable = tableNameToSQL(r.otherTable.fullName);
      flds = new ArrayList();
      List ixflds = new ArrayList();
      TableDef t2 = null;

      // We need to ensure that the fields of the foreign key are in the same
      // order as the fields in the index it will depend on. We do this by
      // first making a list of just the fields in (the other table of) the
      // foreign key.
      for (Iterator li = r.paramJoins.iterator(); li.hasNext();) {
        TableDef.JoinAtom ja = (TableDef.JoinAtom) li.next();
        if (t2 != null && t2 != ja.t2.table) {
          throw new RuntimeException("Eek, bogus fkey");
        }
        t2 = ja.t2.table;
        ixflds.add(t2.fieldsByName.get(ja.f2));
      }

      // Then we scan through all the indexes on the other table and find the
      // one that's unique and also has exactly the right fields.
      Index ix = null;
      t2.resolveIndexes();
      for (Iterator li = t2.indexes.iterator(); li.hasNext();) {
        Index l = (Index) li.next();
        if (l.isUnique && l.containsAll(ixflds) && ixflds.containsAll(l)) {
          ix = l;
        }
      }
      if (ix == null) {
        throw new RuntimeException("Eek, couldn't find index for fkey");
      }

      // Finally we iterate over the fields of that index, hunt down the
      // JoinAtom for each field, and create FFld objects in that order.
      for (Iterator ixi = ix.iterator(); ixi.hasNext();) {
        TableDef.Field f = (TableDef.Field) ixi.next();
        TableDef.JoinAtom ja = null;
        for (Iterator li = r.paramJoins.iterator(); li.hasNext();) {
          TableDef.JoinAtom tja = (TableDef.JoinAtom) li.next();
          if (tja.t2.table.fieldsByName.get(tja.f2).equals(f)) ja = tja;
        }
        if (ja == null) {
          throw new RuntimeException("Eek, mismatched index field");
        } else {
          flds.add(new FFld(this, ja));
        }
      }
    }

    public FKey(Tbl tbl, FKey fkey) {
      this.tbl = tbl;
      name = fkey.name;
      otherTable = fkey.otherTable;
      cascading = fkey.cascading;
      flds = new ArrayList();
      for (Iterator li = fkey.flds.iterator(); li.hasNext();) {
        flds.add(new FFld(this, (FFld) li.next()));
      }
    }
  }

  public class Tbl extends Thing {
    List fields;
    List indexes;
    List fkeys;
    Map fieldMap;
    Map indexMap;
    Map fkeyMap;
    String seqpkfld;
    String seqpkseq;
    String seqpktrg;
    boolean existing;

    public boolean isEmpty() {
      return !existing && fields.isEmpty();
    }

    public Tbl(String name) {
      version = "";
      this.name = name;
      fields = new ArrayList();
      indexes = new ArrayList();
      fkeys = new ArrayList();
      fieldMap = new HashMap();
      indexMap = new HashMap();
      fkeyMap = new HashMap();
      existing = false;
    }

    public Tbl(Tbl tbl) {
      version = tbl.version;
      name = tbl.name;
      fields = new ArrayList(tbl.fields);
      indexes = new ArrayList();
      fkeys = new ArrayList();
      fieldMap = new HashMap(tbl.fieldMap);
      indexMap = new HashMap();
      fkeyMap = new HashMap();
      existing = tbl.existing;
    }

    public Tbl(TableDef td, String version) throws IOException,
        FileLocatedException {
      this.version = version;
      name = tableNameToSQL(td.fullName);
      if (td.existingName != null) {
        existing = true;
        return;
      }
      if (td.isPkeySequenced) {
        seqpkfld = ((TableDef.Field) td.pkey.get(0)).name;
        seqpkseq = tableNameToSQL("sq_" + td.hash + "_" + seqpkfld);
        seqpktrg = tableNameToSQL("sqt_" + td.hash + "_" + seqpkfld);
      }
      fields = new ArrayList();
      fkeys = new ArrayList();
      fieldMap = new HashMap();
      fkeyMap = new HashMap();
      for (Iterator li = td.fields.iterator(); li.hasNext();) {
        Field fld = new Field(this, (TableDef.Field) li.next());
        fields.add(fld);
        fieldMap.put(fld.name, fld);
      }
      td.resolveIndexes();
      indexes = new ArrayList();
      indexMap = new HashMap();
      for (Iterator kli = td.indexes.iterator(); kli.hasNext();) {
        Index ind = (Index) kli.next();
        Index ni = new Index();
        ni.name = ind.name;
        ni.isUnique = ind.isUnique;
        for (Iterator ii = ind.iterator(); ii.hasNext();) {
          TableDef.Field f = (TableDef.Field) ii.next();
          ni.add(fieldMap.get(f.name));
        }
        Key key = new Key(this, ni);
        indexes.add(key);
        indexMap.put(key.name, key);
      }
      for (Iterator li = td.references.iterator(); li.hasNext();) {
        TableDef.Reference r = (TableDef.Reference) li.next();
        if (r.fkey) {
          FKey fk = new FKey(this, r);
          fkeys.add(fk);
          fkeyMap.put(fk.name, fk);
        }
      }
    }

    public Tbl(List tcache) throws IOException {
      int offset = 1;
      if ("tcache".equals(tcache.get(0))) {
        offset = 0;
        version = "";
      } else {
        version = (String) tcache.get(0);
      }
      if (!"tcache".equals(tcache.get(offset))) {
        throw new RuntimeException("Incorrect table cache file");
      }
      name = (String) tcache.get(offset + 1);
      if ("existing".equals(tcache.get(offset + 2))) {
        existing = true;
        if (tcache.size() > offset + 3) {
          befores.addAll((List) ((LineList) tcache.get(offset + 3)).get(0));
        }
        return;
      }
      indexes = new ArrayList();
      fields = new ArrayList();
      fkeys = new ArrayList();
      fieldMap = new HashMap();
      indexMap = new HashMap();
      fkeyMap = new HashMap();
      LineList tcfields = (LineList) tcache.get(offset + 2);
      LineList tcindexes = (LineList) tcache.get(offset + 3);
      LineList tcfkeys = (LineList) tcache.get(offset + 4);
      if (tcache.size() > offset + 5) {
        LineList tcseq = (LineList) tcache.get(offset + 5);
        if (tcseq.size() > 0) {
          List sq = (List) tcseq.get(0);
          if (sq.size() > 0) {
            seqpkfld = (String) sq.get(0);
            if (sq.size() > 1) {
              seqpkseq = (String) sq.get(1);
              if (sq.size() > 2) seqpktrg = (String) sq.get(2);
            }
          }
        }
        // Second line is the 'before's.
        if (tcseq.size() > 1) {
          befores.addAll((List) tcseq.get(1));
        }
      }
      for (Iterator li = tcfields.iterator(); li.hasNext();) {
        Field f = new Field(this, (List) li.next());
        fields.add(f);
        fieldMap.put(f.name, f);
      }
      for (Iterator li = tcindexes.iterator(); li.hasNext();) {
        Key k = new Key(this, (List) li.next());
        indexes.add(k);
        indexMap.put(k.name, k);
      }
      for (Iterator li = tcfkeys.iterator(); li.hasNext();) {
        FKey f = new FKey(this, (List) li.next());
        if (!fkeyMap.containsKey(f.name)) {
          fkeys.add(f);
          fkeyMap.put(f.name, f);
        }
      }
    }

    public String toAddString() {
      return cfg.dbadapter.getCreateTableSql(this);
    }

    public String toDropString() {
      return cfg.dbadapter.getDropTableSql(this);
    }

    public String toRenameString(String newName) {
      return cfg.dbadapter.getRenameTableSql(this, newName);
    }

    public String toAddTriggerString() {
      return cfg.dbadapter.getCreateTriggerSql(this);
    }

    public String toDropTriggerString() {
      return cfg.dbadapter.getDropTriggerSql(this);
    }

    public String toAddSeqString() {
      return cfg.dbadapter.getCreateSequenceSql(this);
    }

    public String toDropSeqString() {
      return cfg.dbadapter.getDropSequenceSql(this);
    }

    private String list2str(Collection l) {
      StringBuffer sb = new StringBuffer();
      for (Iterator li = l.iterator(); li.hasNext();) {
        sb.append(li.next());
      }
      return sb.toString();
    }

    private String list2strsep(Collection l) {
      StringBuffer sb = new StringBuffer();
      for (Iterator li = l.iterator(); li.hasNext();) {
        sb.append(li.next());
        sb.append(" ");
      }
      return sb.toString();
    }

    private String nvl(String s) {
      return s == null ? "" : s + " ";
    }

    public String toString() {
      if (existing) {
        return "[" + version + "] tcache " + name + " existing {\n"
            + list2strsep(befores) + ";\n};\n";
      } else {
        return "[" + version + "] tcache " + name + " {\n" + list2str(fields)
            + "} {\n" + list2str(indexes) + "} {\n" + list2str(fkeys) + "} {\n"
            + nvl(seqpkfld) + nvl(seqpkseq) + nvl(seqpktrg) + ";\n"
            + list2strsep(befores) + ";\n};\n";
      }
    }
  }

  Config cfg;

  public static class AbortException extends RuntimeException {
    private static final long serialVersionUID = 680658798228716018L;
  }

  public static void main(String[] args) throws Throwable {

    // Test for bad arguments and throw out a usage message.
    if (args.length < 1) {
      Output.reportError(null, "Usage: TableCreator <config> <table> ...");
      System.exit(1);
    }

    // Parse and load the configuration file.
    Config cfg = Config.get(args[0]);

    // Do the main processing.
    try {
      new TableCreator().doMain(cfg, false, args);
    } catch (AbortException e) {
      System.exit(1);
    }
  }

  private PrintWriter logSql = null;

  private boolean exceptionThrown = false;

  public void doMain(Config cfg, boolean dropTables, String[] args)
      throws Throwable {

    this.cfg = cfg;

    Output.setTotalProgress(27);
    checkLocks();
    int currentProgress = 0;
    Output.setCurrentProgress(++currentProgress);
    try {
      if (cfg.logsql != null) {
        logSql = new PrintWriter(new FileWriter(cfg.logsql, true));
      }
      openDB();
      Output.setCurrentProgress(++currentProgress);
      // createDBCache();
      loadTables(dropTables, args);
      Output.setCurrentProgress(++currentProgress);
      loadQueries(args);
      if (isUpgrade == null) isUpgrade = Boolean.FALSE;
      Output.setCurrentProgress(++currentProgress);
      runPreUpgradeHooks();
      Output.setCurrentProgress(++currentProgress);
      dropProcs();
      Output.setCurrentProgress(++currentProgress);
      dropTrigs();
      Output.setCurrentProgress(++currentProgress);
      dropSeqs();
      Output.setCurrentProgress(++currentProgress);
      dropFKeys();
      Output.setCurrentProgress(++currentProgress);
      dropKeys();
      Output.setCurrentProgress(++currentProgress);
      alterFields();
      Output.setCurrentProgress(++currentProgress);
      dropChangedFields();
      Output.setCurrentProgress(++currentProgress);
      setFieldsNull();
      Output.setCurrentProgress(++currentProgress);
      renameTables();
      Output.setCurrentProgress(++currentProgress);
      addTables();
      Output.setCurrentProgress(++currentProgress);
      addFields();
      Output.setCurrentProgress(++currentProgress);
      setFieldsNotNull();
      Output.setCurrentProgress(++currentProgress);
      addKeys();
      Output.setCurrentProgress(++currentProgress);
      addFKeys();
      Output.setCurrentProgress(++currentProgress);
      addSeqs();
      Output.setCurrentProgress(++currentProgress);
      addTrigs();
      Output.setCurrentProgress(++currentProgress);
      addProcs();
      Output.setCurrentProgress(++currentProgress);
      dropFields();
      Output.setCurrentProgress(++currentProgress);
      dropTables();
      Output.setCurrentProgress(++currentProgress);
      before("finishing");
      Output.setCurrentProgress(++currentProgress);
    } catch (Throwable t) {
      exceptionThrown = true;
      throw t;
    } finally {
      saveCache();
      Output.setCurrentProgress(++currentProgress);
      debugMsg("About to clear locks");
      clearLocks();
      debugMsg("Locks cleared");
      if (logSql != null) logSql.close();
      closeDB();
    }
    Output.setCurrentProgress(++currentProgress);
    printSummary();
  }

  void debugMsg(String msg) {
    if (cfg.verboseCVS) {
      Output.println(msg);
    }
  }

  boolean isAborted() {
    return !failures.isEmpty();
  }

  Connection conn;

  void printSummary() {
    if (failures.isEmpty()) {
      Output.println("All " + statementsExecuted + " statements successful.");
    } else {
      Output.println("The following statements failed:");
      for (Iterator i = failures.iterator(); i.hasNext();) {
        Output.println((String) i.next());
      }
      throw new AbortException();
    }
  }

  void openDB() throws SQLException {
    if (false) {
      try {
        Class.forName(cfg.dbdriver).newInstance();
      } catch (Exception e) {
        throw new SQLException(e.toString());
      }
      for (Driver driver : Collections.list(DriverManager.getDrivers())) {
        Output.println("Found driver: " + driver.getClass().getName());
      }
      conn = DriverManager.getConnection(cfg.dburl, cfg.dbuser, cfg.dbpasswd);
    } else {
      // EEEEVIL hack bypassing DriverManager entirely to get things to work
      try {
        Driver driver = (Driver) Class.forName(cfg.dbdriver).newInstance();
        conn = driver.connect(cfg.dburl, null);
      } catch (Exception e) {
        throw new SQLException(e.toString());
      }
    }
    // FIXMECACHE
    // if (cfg.dbadapter.getCloseDbToUsersSql() != null) {
    // executeSQL(null, cfg.dbadapter.getCloseDbToUsersSql(), true);
    // }
  }

  // FIXME: since the open database statement can theoretically fail, consider
  // moving that
  // up into a function that gets called in the try, not the finally block.
  // OTOH, we want
  // to open up even on failure, otherwise the webserver could end up locking us
  // out by
  // grabbing the only connection.
  void closeDB() throws SQLException {
    if (conn != null) {
      // FIXMECACHE
      // try {
      // if (cfg.dbadapter.getOpenDbToUsersSql() != null) {
      // executeSQL(null, cfg.dbadapter.getOpenDbToUsersSql(), true);
      // }
      // } finally {
      conn.close();
      // }
    }
  }

  void createDBCache() throws SQLException, FileLocatedException, IOException {
    // boolean stateRowInserted = false;
    // boolean tableTableCreated = false;
    // boolean tablePkeyCreated = false;
    // boolean initialBuildComplete = false;
    // try {
    // Statement s = conn.createStatement();
    // ResultSet rs = s.executeQuery("select * from nrdo_table");
    // if (rs.next()) {
    // stateRowInserted = true;
    // tableTableCreated = cfg.dbadapter.getResultSetBoolean(rs,
    // "is_table_table_created");
    // tablePkeyCreated = cfg.dbadapter.getResultSetBoolean(rs,
    // "is_table_pkey_created");
    // initialBuildComplete = cfg.dbadapter.getResultSetBoolean(rs,
    // "is_initial_build_complete");
    // }
    // } catch (SQLException e) {
    // TableDef td = new TableDef(null, null, "nrdo:table");
    // td.fields.add(td.newField("is_table_table_created", null,
    // cfg.dbadapter.getBooleanType(), null, false, true));
    // td.fields.add(td.newField("is_table_pkey_created", null,
    // cfg.dbadapter.getBooleanType(), null, false, true));
    // td.fields.add(td.newField("is_initial_build_complete", null,
    // cfg.dbadapter.getBooleanType(), null, false, true));
    // executeSQL(null, new Tbl(td, "").toAddString(), true);
    // }
  }

  public abstract class Changes {
    public boolean success = true;
    public boolean unlockWhenDone = true;
    public Map beforeStmts = new HashMap();

    public abstract Thing getCurrent();

    public abstract Thing getDesired();

    public Set befores = new HashSet();
  }

  public class TblChanges extends Changes {
    public Tbl current;
    public Tbl desired;

    public Thing getCurrent() {
      return current;
    }

    public Thing getDesired() {
      return desired;
    }
  }

  public class ProcChanges extends Changes {
    public Qry current;
    public Qry desired;

    public Thing getCurrent() {
      return current;
    }

    public Thing getDesired() {
      return desired;
    }
  }

  public abstract class Thing {
    public String name;
    public String version;
    public Set befores = new HashSet();

    public abstract boolean isEmpty();
  }

  public class Qry extends Thing {
    public String body;
    public boolean isfunction;
    public boolean preUpgradeHook;

    // isEmpty is used to determine whether a cache file should be saved;
    // we always want to save one for queries, to aid in determining
    // whether we should bother reading the .qu file next time.
    public boolean isEmpty() {
      return false;
    }

    public boolean equals(Object o) {
      if (o == null || !(o instanceof Qry)) return false;
      return name.equals(((Qry) o).name)
          && (isfunction == ((Qry) o).isfunction)
          && (preUpgradeHook == ((Qry) o).preUpgradeHook)
          && (body == null ? (((Qry) o).body == null) : body
              .equals(((Qry) o).body));
    }

    public Qry(QueryDef qd, String qdVersion) {
      this.name = tableNameToSQL(qd.fullName);
      this.version = qdVersion;
      this.isfunction = qd.storedfunction;
      this.preUpgradeHook = qd.preUpgradeHook;
      this.body = cfg.dbadapter.getProcBodyString(qd);
    }

    public Qry(String name) {
      this.name = name;
      this.version = "";
    }

    public Qry(List qryCache) {
      this.version = (String) qryCache.get(0);
      String fword = (String) qryCache.get(1);
      if ("spcache".equals(fword)) {
        isfunction = false;
      } else if ("sfcache".equals(fword)) {
        isfunction = true;
      } else if ("spcache-preupgrade".equals(fword)) {
        isfunction = false;
        preUpgradeHook = true;
      } else {
        throw new RuntimeException("Incorrect storedproc cache file");
      }
      this.name = (String) qryCache.get(2);
      this.body = (String) qryCache.get(3);
      if ("".equals(this.body)) this.body = null;
      LineList beforeList = (LineList) qryCache.get(4);
      this.befores.addAll((List) beforeList.get(0));
    }

    public String toCreateString() {
      if (body == null) return null;
      return cfg.dbadapter.getCreateProcSql(this);
    }

    public String toDropString() {
      if (body == null) return null;
      return cfg.dbadapter.getDropProcSql(this);
    }
    
    public String toExecuteString() {
      if (!preUpgradeHook) throw new RuntimeException("Cannot get execute string for a non-upgrade-hook");
      return cfg.dbadapter.getExecuteProcSql(this);
    }

    private String list2strsep(Collection l) {
      StringBuffer sb = new StringBuffer();
      for (Iterator li = l.iterator(); li.hasNext();) {
        sb.append(li.next());
        sb.append(" ");
      }
      return sb.toString();
    }

    private String escape(String s) {
      if (body == null) return "";
      StringBuffer sb = new StringBuffer(s.length() + 40);
      for (int i = 0; i < s.length(); i++) {
        char ch = s.charAt(i);
        if (ch == '[' || ch == ']') sb.append('[');
        sb.append(ch);
      }
      return sb.toString();
    }

    public String toString() {
      return "[" + version + "] " + (isfunction ? "sfcache " : preUpgradeHook ? "spcache-preupgrade " : "spcache ")
          + name + " [" + escape(body) + "] {\n" + list2strsep(befores)
          + "\n;};\n";
    }
  }

  List allChanges = new LinkedList();
  List tblChanges = new LinkedList();
  List procChanges = new LinkedList();
  String userString;
  Boolean isUpgrade;

  private static final int NOT_LOCKED = 4;
  private static final int LOCKED_UPDATING = 5;
  private static final int LOCKED_BY_SELF = 6;
  private static final int LOCKED_BY_OTHER = 7;

  private CVSDir cachedir;
  private CVSFile lockfile;

  void checkLocks() throws IOException, InterruptedException {
    cachedir = CVSDir.getInstance(cfg.cachebase);
    cachedir.verboseCVS = cfg.verboseCVS;
    char sep = File.pathSeparatorChar;
    userString = System.getProperty("user.name").toLowerCase() + "@"
        + InetAddress.getLocalHost().getHostName().toLowerCase() + sep
        + PathUtil.canonicalPath(cfg.cfgfile);
    lockfile = new CVSFile(cachedir, "_lock");
    boolean success = false;
    while (!success) {
      if (lockfile.create()) {
        try {
          lockfile.commit();
          success = true;
        } catch (IOException e) {
          lockfile.delete();
          lockfile.sync();
        }
      }
      if (!success) {
        if (cfg.forcedrops
            || !prompt(null, "Table update in progress by someone else",
                "Table update in progress by someone else", "Try again?")) {
          Output
              .reportError(null,
                  "Table update in progress by someone else - Could not create lockfile.");
          throw new AbortException();
        }
      }
    }
    CVSFile stateFile = new CVSFile(cachedir, "_state");
    if (stateFile.exists()) {
      isUpgrade = new Boolean("complete".equals(stateFile.getContents()));
    }
  }

  String lockingUser;

  // This is what we put in the lockfile:
  // XuserName@fqhn:cfgfile
  // X is ! for LOCKED_UPDATING and ~ for LOCKED_BY_SELF.
  // userName@fqhn:cfgfile is stored in userString
  // : is replaced by the path separator char for your platform (';' for
  // windows)
  // cfgfile is the canonical path to the configuration file.
  // If force is passed, NOT_LOCKED will be returned even if the table was
  // previously locked. In most cases you cannot decide whether to use force
  // without calling lockTable once without it anyway.
  int lockTable(String desired, int lockType, boolean force)
      throws IOException, InterruptedException {
    CVSFile tblLock = new CVSFile(cachedir, "_lock." + desired);
    if (force || tblLock.create()) {
      tblLock.setContents((lockType == LOCKED_UPDATING ? "!" : "~")
          + userString);
      return NOT_LOCKED;
    } else {
      String lockString = tblLock.getContents();

      // Look for lockStrings that contain uppercase username or hostnames -
      // these are
      // obsolete and need to be corrected.
      int pos = lockString.indexOf(File.pathSeparatorChar);
      lockString = lockString.substring(0, pos).toLowerCase()
          + lockString.substring(pos);

      if (!lockString.substring(1).equals(userString)) {
        lockingUser = lockString.substring(1);
        return LOCKED_BY_OTHER;
      } else {
        switch (lockString.charAt(0)) {
        case '!':
          return LOCKED_UPDATING;
        case '~':
          return LOCKED_BY_SELF;
        default:
          throw new IOException("malformed lockfile: " + lockString);
        }
      }
    }
  }

  void unlockTable(String desired) throws IOException, InterruptedException {
    new CVSFile(cachedir, "_lock." + desired).delete();
  }

  static class ProcessDecision {
    boolean processMe;
    boolean unlockWhenDone;
    String tdVersion;
    BufferedReader cr;
    CVSFile f;
    int lockedness;
    String desiredName;
  }

  private Boolean aggressive = null;

  private boolean getAggressive() {
    if (aggressive == null) {
      aggressive = Boolean.valueOf(cfg.stampfile != null
          && !cfg.stampfile.exists());
    }
    return aggressive.booleanValue();
  }

  ProcessDecision shouldProcess(File dfnFile, String module, String fullName,
      int i) throws IOException, InterruptedException, ParseException {
    ProcessDecision result = new ProcessDecision();

    CVSFile tdFile = new CVSFile(dfnFile);
    if (cfg.vssroot != null) {
      tdFile.getParent().setVssRoot(
          cfg.vssroot + (module == null ? "" : module.replace(':', '/')));
      tdFile.getParent().setVssAggressive(getAggressive());
      tdFile.getParent().setVssDir(
          cfg.vssdir == null ? "\\\\Vss\\Tracker" : cfg.vssdir);
    }
    result.tdVersion = tdFile.getVersion();
    result.desiredName = tableNameToSQL(fullName);

    result.f = new CVSFile(cachedir, result.desiredName);
    result.lockedness = lockTable(result.desiredName, LOCKED_UPDATING, false);
    int fileState;
    int realFileState;
    // In VSS currently we are guaranteed not to need to process if the dfn is
    // older
    // than the cache. We can save an "ss.exe" invocation by taking advantage of
    // this assumption.
    if (!getAggressive() && cfg.vssroot != null && result.f.exists()
        && result.f.lastModified() > tdFile.lastModified()) {
      fileState = CVSDir.UNMODIFIED;
      realFileState = CVSDir.UNMODIFIED;
    } else {
      fileState = tdFile.getState();
      realFileState = (getAggressive() && fileState == CVSDir.UNMODIFIED) ? CVSDir.MODIFIED
          : fileState;
    }
    boolean modified = fileState != CVSDir.UNMODIFIED
        && fileState != CVSDir.OUT_OF_DATE;
    String cacheVersion;
    if (result.f.exists()) {
      if (isUpgrade == null) isUpgrade = Boolean.TRUE;
      result.cr = new BufferedReader(result.f.getReader());
      result.cr.mark(32);
      StringBuffer sb = new StringBuffer(30);
      int ch = result.cr.read();
      if (ch == '[') {
        while ((ch = result.cr.read()) >= 0 && ch != ']') {
          sb.append((char) ch);
        }
        cacheVersion = sb.toString();
      } else {
        cacheVersion = "";
      }
      result.cr.reset();
    } else {
      cacheVersion = "";
    }
    int verCmp = CVSDir.compareVersions(result.tdVersion, cacheVersion);
    if (cfg.vssroot != null && fileState == CVSDir.MODIFIED) verCmp = 1;
    result.processMe = true;

    // See design/cvs-modifications for rationale behind this if statement.
    if (modified && verCmp < 1 && result.lockedness == LOCKED_BY_OTHER) {
      // Warn 1
      String warn1 = lockingUser + " is in the process of modifying "
          + fullName + ".";
      Output.reportError(null, warn1 + "\nPlease coordinate with that user"
          + " to decide how to proceed.");
      failures.add(warn1);
      result.processMe = false;
    } else if (modified && verCmp < 0 && result.lockedness == NOT_LOCKED) {
      // Warn 2
      String warn2 = "You have modified " + tdFile + ", but you do not "
          + "have the latest version from cvs.";
      Output.reportError(null, warn2 + "\nYou should use 'cvs update -dP' to "
          + "get the latest version before proceeding.");
      failures.add(warn2);
      result.processMe = false;
    } else if (verCmp < 0
        && (result.lockedness == LOCKED_BY_SELF || result.lockedness == LOCKED_UPDATING)) {
      // Error 1
      String error1 = "Internal error: You hold the lock on " + fullName
          + " but your version of " + tdFile + " is outdated.";
      Output.reportError(null, error1 + "\nThis should not be able to happen. "
          + "Your best bet is to 'cvs update -dP' and try " + "again.");
      failures.add(error1);
      result.processMe = false;
    } else if (!modified
        && result.f.exists()
        && (result.lockedness == LOCKED_BY_OTHER || (result.lockedness == NOT_LOCKED && verCmp <= 0))) {
      // Ignore
      result.processMe = getAggressive()
          && result.lockedness != LOCKED_BY_OTHER;
    } else if (modified
        && verCmp == 0
        && (result.lockedness == NOT_LOCKED || result.lockedness == LOCKED_BY_SELF)) {
      // Upd+FooXX
      result.processMe = getAggressive() || Boolean.TRUE.equals(isUpgrade)
          || !result.f.exists()
          || result.f.lastModified() <= tdFile.lastModified();
    } else {
      // Upd+Foo
      result.processMe = true;
    }
    result.unlockWhenDone = realFileState == CVSDir.UNMODIFIED
        || realFileState == CVSDir.NOT_VERSIONED
        || realFileState == CVSDir.OUT_OF_DATE;
    if (result.processMe) {
      Output
          .println("Chose to process " + tdFile + " and "
              + (result.unlockWhenDone ? "unlock" : "leave locked")
              + " when done.");
    }
    return result;
  }

  void loadTables(boolean dropTables, String[] args) throws IOException,
      InterruptedException, FileLocatedException {
    cachedir.update();
    // cfg.dfnbase.verbose = true;
    List tables = cfg.dfnbase.getFromArgs(args);
    int i = 0;
    HashSet known = new HashSet();

    for (Iterator li = tables.iterator(); li.hasNext();) {
      TableDef td = (TableDef) li.next();
      known.add(tableNameToSQL(td.fullName));

      ProcessDecision decision = shouldProcess(td.dfnFile, td.module,
          td.fullName, i);
      if (decision.processMe) {
        lockTable(decision.desiredName, LOCKED_UPDATING, true);
        TblChanges tc = new TblChanges();
        tc.unlockWhenDone = decision.unlockWhenDone;
        // if (td == null) {
        // tc.desired = new Tbl(decision.desiredName);
        // tc.beforeStmts = new HashMap();
        // } else {
        td.resolveRefs();
        tc.desired = new Tbl(td, decision.tdVersion);
        tc.beforeStmts = td.beforeStmts;
        // }
        if (decision.cr != null) {
          tc.current = new Tbl(OMParser.parseLine(decision.cr,
              decision.f.toString()));
        } else if (!td.renamedFrom.isEmpty()) {
          for (Iterator j = td.renamedFrom.iterator(); j.hasNext();) {
            String oldName = (String) j.next();
            String oldDbName = tableNameToSQL(oldName);
            known.add(oldDbName);
            CVSFile oldCacheFile = new CVSFile(cachedir, oldDbName);
            if (oldCacheFile.exists()) {
              tc.current = new Tbl(OMParser.parseLine(oldCacheFile.getReader(),
                  oldCacheFile.toString()));
            }
          }
        }
        if (tc.desired.existing) {
          if (tc.current == null) tc.current = tc.desired;
        } else {
          tblChanges.add(tc);
        }
        allChanges.add(tc);
      } else {
        if (decision.cr != null) decision.cr.close();
        if (decision.unlockWhenDone && decision.lockedness != LOCKED_BY_OTHER) {
          unlockTable(decision.desiredName);
        }
      }
      i++;
    }
    if (dropTables) {
      File[] files = cfg.cachebase.listFiles();
      for (i = 0; i < files.length; i++) {
        if (files[i].isFile()
            && files[i].getName().startsWith(cfg.schema + ".")
            && !known.contains(files[i].getName())) {
          List contents = OMParser.parseLine(files[i]);
          if (contents.get(1).equals("tcache") &&
              (contents.size() <= 3 || !contents.get(3).equals("existing"))) {
            Output.println("Processing table for deletion: "
                + files[i].getName());
            known.add(files[i].getName());
            TblChanges tc = new TblChanges();
            tc.current = new Tbl(contents);
            tc.desired = new Tbl(files[i].getName());
            tblChanges.add(tc);
            allChanges.add(tc);
          }
        }
      }
    }
  }

  void loadQueries(String[] args) throws IOException, InterruptedException,
      FileLocatedException {
    cachedir.update();
    List queries = cfg.dfnbase.getQueriesFromArgs(args);
    int i = 0;
    for (Iterator li = queries.iterator(); li.hasNext();) {
      QueryDef qd = (QueryDef) li.next();
      ProcessDecision decision = shouldProcess(qd.dfnFile, qd.module,
          qd.fullName, i);
      if (decision.processMe) {
        lockTable(decision.desiredName, LOCKED_UPDATING, true);
        ProcChanges pc = new ProcChanges();
        pc.unlockWhenDone = decision.unlockWhenDone;
        // if (qd == null) {
        // pc.desired = new Qry(decision.desiredName);
        // pc.beforeStmts = new HashMap();
        // } else {
        qd.resolve();
        pc.desired = new Qry(qd, decision.tdVersion);
        pc.beforeStmts = qd.beforeStmts;
        // }
        if (decision.cr != null) {
          pc.current = new Qry(OMParser.parseLine(decision.cr,
              decision.f.toString()));
        }
        procChanges.add(pc);
        allChanges.add(pc);
      } else {
        if (decision.cr != null) decision.cr.close();
        if (decision.unlockWhenDone && decision.lockedness != LOCKED_BY_OTHER) {
          unlockTable(decision.desiredName);
        }
      }
      i++;
    }
  }

  void saveCache() throws IOException, InterruptedException {
    boolean success = true;
    if (conn == null || exceptionThrown) success = false;
    for (Iterator li = allChanges.iterator(); li.hasNext();) {
      Changes tc = (Changes) li.next();
      if (tc.getCurrent() != null) {
        if (tc.getDesired() != null)
          tc.getCurrent().version = tc.getDesired().version;
        CVSFile f = new CVSFile(cachedir, tc.getCurrent().name);
        if (tc.getCurrent().isEmpty()) {
          if (f.exists()) {
            f.delete();
          }
        } else {
          f.setContents(tc.getCurrent().toString());
        }
        if (tc.success) {
          if (tc.unlockWhenDone) {
            unlockTable(tc.getDesired().name);
          } else {
            lockTable(tc.getDesired().name, LOCKED_BY_SELF, true);
          }
        } else {
          success = false;
        }
      }
    }
    CVSFile stateFile = new CVSFile(cachedir, "_state");
    if (success || Boolean.TRUE.equals(isUpgrade)) {
      stateFile.setContents("complete");
    } else {
      stateFile.setContents("incomplete");
    }
  }

  public void clearLocks() throws IOException, InterruptedException {
    lockfile.delete();
    debugMsg("Lockfile deleted, about to commit cachedir");
    cachedir.commit();
    debugMsg("Committed cachedir");
  }

  private List failures = new ArrayList();

  public boolean executeSQL(Changes tc, String sql) {
    return executeSQL(tc, sql, true);
  }

  private int statementsExecuted = 0;

  public boolean executeSQL(Changes tc, String sql, boolean fatal) {
    if (tc != null && !tc.success) return false;
    statementsExecuted++;
    Output.println(sql);
    boolean success = false;
    while (!success) {
      try {
        Statement s = conn.createStatement();
        s.executeUpdate(sql);
        s.close();
        Output.println("SUCCESSFUL.");
        if (logSql != null) {
          logSql.println(sql);
          String cmdSep = cfg.dbadapter.getCommandSeparator();
          if (cmdSep != null) logSql.println(cmdSep);
        }
        success = true;
        if (cfg.dbadapter.isSleepRequired()) Thread.sleep(200);
      } catch (SQLException e) {
        if (!failures.isEmpty()
            || cfg.forcedrops
            || !prompt(null, "SQL statement " + trunc(sql, 100) + " failed: "
                + e.getMessage(), trunc(sql, 100), "Try again?")) {
          Output.println("FAILED: " + e);
          if (fatal) {
            if (tc != null) tc.success = false;
            failures.add(sql);
          }
          return false;
        }
      } catch (InterruptedException e) {
        // do nothing
      }
    }
    return true;
  }

  private String trunc(String val, int length) {
    if (val.length() > length) {
      val = val.substring(0, length - 3) + "...";
    }
    return val;
  }

  public boolean prompt(Changes tc, String prompt, String summary,
      String question) {
    if (cfg.forcedrops) return true;
    boolean ok = Output.prompt(prompt, question);
    if (!ok && tc != null) {
      tc.success = false;
      failures.add("Manual abort: " + summary);
    }
    return ok;
  }

  // Determine whether this before statement should
  // be run, based on whether this NRDOTool run is an
  // upgrade or not.
  private boolean skipBefore(BeforeStmt before) {
    return isUpgrade.booleanValue() ? !before.upgrade : !before.initially;
  }

  public void before(String step) throws FileLocatedException, IOException {
    if (isAborted()) return;
    boolean didAny = false;
    for (Iterator i = allChanges.iterator(); i.hasNext();) {
      Changes tc = (Changes) i.next();
      if (tc.beforeStmts.containsKey(step)) {
        List befores = (List) tc.beforeStmts.get(step);
        for (Iterator j = befores.iterator(); j.hasNext();) {
          BeforeStmt before = (BeforeStmt) j.next();
          // If the table/query hasn't been actually created yet then we don't
          // allow any before statements to actually *happen*, but we do track
          // which
          // ones are skipped.
          if (tc.getCurrent() == null) {
            if (skipBefore(before)) {
              tc.befores.add(before.name);
            } else {
              throw new RuntimeException(
                  "Illegal before statement - statements prior to table creation are allowed on upgrade only");
            }
          } else if (!tc.getCurrent().befores.contains(before.name)) {
            if (skipBefore(before)) {
              tc.getCurrent().befores.add(before.name);
            } else {
              if (!didAny) {
                Output.println("before " + step + "...");
              }
              if (executeSQL(tc, Util.translateSql(before.sql, cfg))) {
                tc.getCurrent().befores.add(before.name);
              }
            }
          }
        }
      }
    }
    Output.println(step + "...");
  }

  public void renameTables() throws IOException, FileLocatedException,
      InterruptedException {
    before("renaming-tables");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null && !tbl.desired.name.equals(tbl.current.name)) {
        if (executeSQL(tbl, tbl.current.toRenameString(tbl.desired.name))) {
          new CVSFile(cachedir, tbl.current.name).renameTo(tbl.desired.name);
          tbl.current.name = tbl.desired.name;
        }
      }
    }
  }

  public void runPreUpgradeHooks() throws IOException, FileLocatedException {
    before("pre-upgrade-hooks");
    if (isAborted()) return;
    
    for (Iterator i = procChanges.iterator(); i.hasNext();) {
      ProcChanges proc = (ProcChanges) i.next();
      if (proc.current != null
          && proc.current.body != null
          && proc.current.preUpgradeHook) {
        executeSQL(proc, proc.current.toExecuteString());
      }
    }
  }
  public void dropProcs() throws IOException, FileLocatedException {
    before("dropping-storedprocs");
    if (isAborted()) return;

    for (Iterator i = procChanges.iterator(); i.hasNext();) {
      ProcChanges proc = (ProcChanges) i.next();
      if (proc.current != null
          && proc.current.body != null
          && (proc.desired == null || proc.desired.body == null || (!cfg.dbadapter
              .isCreateOrReplaceProcSupported() && !proc.desired
              .equals(proc.current)))) {
        if (executeSQL(proc, proc.current.toDropString())) {
          proc.current.body = null;
        }
      }
    }
  }

  public void addProcs() throws IOException, FileLocatedException {
    before("adding-storedprocs");
    if (isAborted()) return;

    for (Iterator i = procChanges.iterator(); i.hasNext();) {
      ProcChanges proc = (ProcChanges) i.next();
      if (proc.desired != null && !proc.desired.equals(proc.current)) {
        if (proc.desired.body != null) {
          if (executeSQL(proc, proc.desired.toCreateString())) {
            proc.current = proc.desired;
            proc.current.befores.addAll(proc.befores);
          }
        } else {
          proc.current = proc.desired;
          proc.current.befores.addAll(proc.befores);
        }
      }
    }
  }

  public void dropTables() throws IOException, FileLocatedException {
    before("dropping-tables");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null && tbl.desired.fields.isEmpty()) {
        if (prompt(tbl, "About to drop table " + tbl.current.name +
            ":\nAll data in this table will be lost!",
            "Drop table " + tbl.current.name,
            "Are you sure?")) {
          if (executeSQL(tbl, tbl.current.toDropString())) {
            tbl.current.fields.clear();
            tbl.current.indexes.clear();
            tbl.current.fkeys.clear();
            tbl.current.fieldMap.clear();
            tbl.current.indexMap.clear();
            tbl.current.fkeyMap.clear();
            tbl.current.seqpkfld = null;
          }
        }
      }
    }
  }

  public void addTables() throws IOException, FileLocatedException {
    before("adding-tables");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current == null && tbl.desired != null) {
        if (executeSQL(tbl, tbl.desired.toAddString())) {
          tbl.current = new Tbl(tbl.desired);
          tbl.current.befores.addAll(tbl.befores);
        }
      }
    }
  }

  public void alterFields() throws IOException, FileLocatedException {
    before("altering-fields");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator fli = tbl.desired.fields.iterator(); fli.hasNext();) {
          Field df = (Field) fli.next();
          Field cf = (Field) tbl.current.fieldMap.get(df.name);
          if (cf != null && cf.identity == df.identity
              && !cf.dbType.equals(df.dbType)) {
            String oldtype = cf.dbType;
            cf.dbType = df.dbType;
            if (executeSQL(tbl, cf.toAlterString(), false)) {
              // Nothing to do on success
            } else {
              cf.dbType = oldtype;
            }
          }
        }
      }
    }
  }

  public void dropChangedFields() throws IOException, FileLocatedException {
    before("dropping-changed-fields");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        if (tbl.desired.fields.isEmpty()) continue;
        for (Iterator fli = tbl.current.fields.iterator(); fli.hasNext();) {
          Field cf = (Field) fli.next();
          Field df = (Field) tbl.desired.fieldMap.get(cf.name);
          if ((df != null && cf.identity == df.identity && !cf.equals(df))
              || (df == null && cf.identity)) {
            if (prompt(tbl, "About to drop field " + tbl.current.name + "."
                + cf.name + ":\nAll data in this column will be lost!",
                "Drop field " + tbl.current.name + "." + cf.name,
                "Are you sure?")) {
              if (executeSQL(tbl, cf.toDropString())) {
                fli.remove();
                tbl.current.fieldMap.remove(cf.name);
              }
            }
          }
        }
      }
    }
  }

  public void setFieldsNull() throws IOException, FileLocatedException {
    before("setting-null");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null && !tbl.desired.fields.isEmpty()) {
        for (Iterator fli = tbl.current.fields.iterator(); fli.hasNext();) {
          Field cf = (Field) fli.next();
          Field df = (Field) tbl.desired.fieldMap.get(cf.name);
          if (!cf.nullable && (df == null || df.nullable || !cf.equals(df))) {
            if (cfg.dbadapter instanceof MSSQLAdapter
                && (cf.dbType.equals("text") || cf.dbType.equals("ntext"))) {
              // skip trying to make it nullable because it'd fail anyway...
            } else if (executeSQL(tbl, cf.toNullString())) {
              cf.nullable = true;
            }
          }
        }
      }
    }
  }

  public void addFields() throws IOException, FileLocatedException {
    before("adding-fields");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator fli = tbl.desired.fields.iterator(); fli.hasNext();) {
          Field df = (Field) fli.next();
          if (tbl.current.fieldMap.get(df.name) == null) {
            if (executeSQL(tbl, df.toAddString())) {
              Field cf = new Field(tbl.current, df);
              if (df.identity && !cfg.dbadapter.isNullIdentitySupported()) {
                cf.nullable = false;
              } else {
                cf.nullable = true;
              }
              tbl.current.fields.add(cf);
              tbl.current.fieldMap.put(cf.name, cf);
            }
          }
        }
      }
    }
  }

  public void setFieldsNotNull() throws IOException, FileLocatedException {
    before("setting-notnull");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator fli = tbl.desired.fields.iterator(); fli.hasNext();) {
          Field df = (Field) fli.next();
          Field cf = (Field) tbl.current.fieldMap.get(df.name);
          if (cf != null && cf.nullable != df.nullable) {
            if (executeSQL(tbl, df.toNotNullString())) {
              cf.nullable = df.nullable;
            }
          }
        }
      }
    }
  }

  public void dropFields() throws IOException, FileLocatedException {
    before("dropping-fields");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null && !tbl.desired.fields.isEmpty()) {
        for (Iterator fli = tbl.current.fields.iterator(); fli.hasNext();) {
          Field cf = (Field) fli.next();
          if (!cf.equals(tbl.desired.fieldMap.get(cf.name))) {
            if (prompt(tbl, "About to drop field " + tbl.current.name + "."
                + cf.name + ":\nAll data in this column will be lost!",
                "Drop field " + tbl.current.name + "." + cf.name,
                "Are you sure?")) {
              if (executeSQL(tbl, cf.toDropString())) {
                fli.remove();
                tbl.current.fieldMap.remove(cf.name);
              }
            }
          }
        }
      }
    }
  }

  public void dropKeys() throws IOException, FileLocatedException {
    before("dropping-indexes");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator kli = tbl.current.indexes.iterator(); kli.hasNext();) {
          Key ck = (Key) kli.next();
          if (!ck.equals(tbl.desired.indexMap.get(ck.name))) {
            if (executeSQL(tbl, ck.toDropString())) {
              kli.remove();
              tbl.current.indexMap.remove(ck.name);
            }
          }
        }
      }
    }
  }

  public void addKeys() throws IOException, FileLocatedException {
    before("adding-indexes");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator kli = tbl.desired.indexes.iterator(); kli.hasNext();) {
          Key dk = (Key) kli.next();
          if (tbl.current.indexMap.get(dk.name) == null) {
            if (executeSQL(tbl, dk.toAddString())) {
              Key ck = new Key(tbl.current, dk);
              tbl.current.indexes.add(ck);
              tbl.current.indexMap.put(ck.name, ck);
            }
          }
        }
      }
    }
  }

  // FIXME: This code has a bug that causes FKeys not to be dropped if an index
  // that they rely on is to be changed. The ideal solution is to somehow find
  // the index that each FKey relies on and drop the FKey if that index is to
  // be changed. An alternative workaround (which works, but is really ugly) is
  // to drop *all* FKeys *all* the time, and re-add them afterwards.
  // The difficulty with the first solution is that it can mean dropping fkeys
  // on tables that aren't even under consideration, if only a certain subset
  // of tables were mentioned on the command line.
  // As a hackaround, if this bites you, run TableCreator once with the 2 lines
  // marked COMMENTME commented out.
  public void dropFKeys() throws IOException, FileLocatedException {
    before("dropping-fkeys");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator fli = tbl.current.fkeys.iterator(); fli.hasNext();) {
          FKey cf = (FKey) fli.next();
          if (!cf.equals(tbl.desired.fkeyMap.get(cf.name))) { // COMMENTME
            if (executeSQL(tbl, cf.toDropString())) {
              fli.remove();
              tbl.current.fkeyMap.remove(cf.name);
            }
          } // COMMENTME
        }
      }
    }
  }

  public void addFKeys() throws IOException, FileLocatedException {
    before("adding-fkeys");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        for (Iterator fli = tbl.desired.fkeys.iterator(); fli.hasNext();) {
          FKey df = (FKey) fli.next();
          if (tbl.current.fkeyMap.get(df.name) == null) {
            if (executeSQL(tbl, df.toAddString())) {
              FKey cf = new FKey(tbl.current, df);
              tbl.current.fkeys.add(cf);
              tbl.current.fkeyMap.put(cf.name, cf);
            }
          }
        }
      }
    }
  }

  public void dropSeqs() throws IOException, FileLocatedException {
    if (!cfg.dbadapter.isSeqUsed()) return;
    before("dropping-seqs");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        if (tbl.current.seqpkseq != null
            && (!tbl.current.seqpkseq.equals(tbl.desired.seqpkseq)
                || !tbl.current.seqpkfld.equals(tbl.desired.seqpkfld) || !(tbl.current.seqpktrg == null ? tbl.desired.seqpktrg == null
                : tbl.current.seqpktrg.equals(tbl.desired.seqpktrg)))) {
          if (tbl.current.seqpktrg == null
              && !tbl.current.seqpkseq.equals(tbl.desired.seqpkseq)) {
            if (executeSQL(tbl, tbl.current.toDropSeqString())) {
              if (tbl.current.seqpktrg == null) tbl.current.seqpkfld = null;
              tbl.current.seqpkseq = null;
            }
          }
        }
      }
    }
  }

  public void addSeqs() throws IOException, FileLocatedException {
    if (!cfg.dbadapter.isSeqUsed()) return;
    before("adding-seqs");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        if (tbl.desired.seqpkseq != null && tbl.current.seqpkseq == null) {
          if (executeSQL(tbl, tbl.desired.toAddSeqString())) {
            tbl.current.seqpkfld = tbl.desired.seqpkfld;
            tbl.current.seqpkseq = tbl.desired.seqpkseq;
          }
        }
      }
    }
  }

  public void dropTrigs() throws IOException, FileLocatedException {
    if (!cfg.dbadapter.isTrigUsed()) return;
    before("dropping-triggers");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        if (tbl.current.seqpkseq != null
            && (!tbl.current.seqpkseq.equals(tbl.desired.seqpkseq)
                || !tbl.current.seqpkfld.equals(tbl.desired.seqpkfld) || !(tbl.current.seqpktrg == null ? tbl.desired.seqpktrg == null
                : tbl.current.seqpktrg.equals(tbl.desired.seqpktrg)))) {
          if (tbl.current.seqpktrg != null) {
            if (executeSQL(tbl, tbl.current.toDropTriggerString())) {
              tbl.current.seqpktrg = null;
            }
          }
        }
      }
    }
  }

  public void addTrigs() throws IOException, FileLocatedException {
    if (!cfg.dbadapter.isTrigUsed()) return;
    before("adding-triggers");
    if (isAborted()) return;

    for (Iterator li = tblChanges.iterator(); li.hasNext();) {
      TblChanges tbl = (TblChanges) li.next();
      if (tbl.current != null) {
        if (tbl.desired.seqpktrg != null && tbl.current.seqpktrg == null
            && tbl.current.seqpkseq != null) {
          if (executeSQL(tbl, tbl.desired.toAddTriggerString())) {
            tbl.current.seqpktrg = tbl.desired.seqpktrg;
          }
        }
      }
    }
  }
}
