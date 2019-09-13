///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.DBAdapter
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2003/04/28
// Description: Defines the syntax of the DDL of a particular database.
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
import java.io.IOException;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.Iterator;

import net.netreach.util.FileLocatedException;

/**
 * Defines the syntax of the DDL of a particular database.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public abstract class DBAdapter {
  public static DBAdapter getInstance(Config cfg) {
    if ("postgres".equals(cfg.dbadaptername)) {
      return new PgAdapter(cfg);
    } else if ("oracle".equals(cfg.dbadaptername)) {
      return new OracleAdapter(cfg);
    } else if ("mssqlserver".equals(cfg.dbadaptername)) {
      return new MSSQLAdapter(cfg);
    } else if ("msaccess".equals(cfg.dbadaptername)) {
      return new AccessAdapter(cfg);
    } else {
      throw new IllegalArgumentException("Unknown DB adapter " + cfg.dbadapter);
    }
  }

  protected Config cfg;

  public DBAdapter(Config cfg) {
    this.cfg = cfg;
  }

  // Field add string
  public abstract String getAddFieldSql(TableCreator.Field fld);

  // Field drop
  public String getDropFieldSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " DROP COLUMN "
        + q(fld.name);
  }

  // Field set not null
  public String getSetFieldNotNullSql(TableCreator.Field fld) {
    return getAlterFieldTypeSql(fld);
  }

  public String getSetFieldNullSql(TableCreator.Field fld) {
    return getAlterFieldTypeSql(fld, true);
  }

  public String getAlterFieldTypeSql(TableCreator.Field fld) {
    return getAlterFieldTypeSql(fld, false);
  }

  public String getAlterFieldTypeSql(TableCreator.Field fld, boolean forceNull) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " ALTER COLUMN "
        + q(fld.name) + " " + getFieldString(fld, forceNull);
  }

  // How identity fields differ from non-identity fields
  public String getIdentityFieldString(TableCreator.Field fld, boolean forceNull) {
    return getNormalFieldString(fld, forceNull);
  }

  // How an individual field is created (not currently different based on DBs)
  public String getNormalFieldString(TableCreator.Field fld, boolean forceNull) {
    return fld.dbType + (forceNull || fld.nullable ? "" : " NOT") + " NULL";
  }

  // How a field is created (not currently different based on DBs)
  public String getFieldString(TableCreator.Field fld, boolean forceNull) {
    return fld.identity ? getIdentityFieldString(fld, forceNull)
        : getNormalFieldString(fld, forceNull);
  }

  public String getFieldString(TableCreator.Field fld) {
    return getFieldString(fld, false);
  }

  // Whether an identity field can be created as nullable and altered, or
  // whether it must be created non-null off the bat.
  public boolean isNullIdentitySupported() {
    return true;
  }

  // Create unique (pkey/unique) index (not currently different based on DBs)
  public String getCreateUniqueSql(TableCreator.Key key) {
    StringBuffer sb = new StringBuffer("ALTER TABLE " + qschema(key.tbl.name)
        + " ADD CONSTRAINT " + q(key.name)
        + (key.type == TableCreator.Key.TYPE_PK ? " PRIMARY KEY" : " UNIQUE"));
    String sep = " (";
    for (Iterator li = key.fields.iterator(); li.hasNext();) {
      sb.append(sep + q(((TableCreator.Field) li.next()).name));
      sep = ", ";
    }
    sb.append(")");
    if (cfg.indextablespace != null) {
      sb.append(" USING INDEX TABLESPACE " + cfg.indextablespace);
    }
    return sb.toString();
  }

  // Drop unique (pkey/unique) index (not currently different based on DBs)
  public String getDropUniqueSql(TableCreator.Key key) {
    return "ALTER TABLE " + qschema(key.tbl.name) + " DROP CONSTRAINT "
        + q(key.name);
  }

  // Full name for an index
  public String getIndexName(TableCreator.Key key) {
    return key.name;
  }

  // Create index (not currently different based on DBs)
  public String getCreateIndexSql(TableCreator.Key key) {
    StringBuffer sb = new StringBuffer("CREATE INDEX " + getIndexName(key)
        + " ON " + qschema(key.tbl.name));
    String sep = " (";
    for (Iterator li = key.fields.iterator(); li.hasNext();) {
      sb.append(sep + q(((TableCreator.Field) li.next()).name));
      sep = ", ";
    }
    sb.append(")");
    if (cfg.indextablespace != null) {
      sb.append(" TABLESPACE " + cfg.indextablespace);
    }
    return sb.toString();
  }

  // Drop index
  public abstract String getDropIndexSql(TableCreator.Key key);

  // Add fkey (not currently different based on DBs)
  public String getAddFkeySql(TableCreator.FKey fkey) {
    StringBuffer sb = new StringBuffer("ALTER TABLE " + qschema(fkey.tbl.name)
        + " ADD CONSTRAINT " + q(fkey.name) + " FOREIGN KEY (");
    StringBuffer sb2 = new StringBuffer();
    String sep = "";
    for (Iterator li = fkey.flds.iterator(); li.hasNext();) {
      TableCreator.FFld ff = (TableCreator.FFld) li.next();
      sb.append(sep + q(ff.thisFld.name));
      sb2.append(sep + q(ff.otherFld));
      sep = ", ";
    }
    sb.append(") REFERENCES " + qschema(fkey.otherTable) + " (" + sb2 + ")");
    if (fkey.cascading) sb.append(" ON DELETE CASCADE");
    return sb.toString();
  }

  // Drop fkey (not currently different based on DBs)
  public String getDropFkeySql(TableCreator.FKey fkey) {
    return "ALTER TABLE " + qschema(fkey.tbl.name) + " DROP CONSTRAINT "
        + q(fkey.name);
  }

  // Create table (not currently different based on DBs)
  public String getCreateTableSql(TableCreator.Tbl tbl) {
    StringBuffer sb = new StringBuffer("CREATE TABLE " + qschema(tbl.name)
        + " (");
    String sep = "";
    for (Iterator li = tbl.fields.iterator(); li.hasNext();) {
      TableCreator.Field f = (TableCreator.Field) li.next();
      sb.append(sep + "\n  " + q(f.name) + " " + getFieldString(f));
      sep = ",";
    }
    sb.append(")");
    return sb.toString();
  }

  // Drop table (not currently different based on DBs)
  public String getDropTableSql(TableCreator.Tbl tbl) {
    return "DROP TABLE " + qschema(tbl.name);
  }

  // Rename table
  public String getRenameTableSql(TableCreator.Tbl tbl, String newName) {
    return "ALTER TABLE " + qschema(tbl.name) + " RENAME TO "
        + qschema(newName);
  }

  // Add trigger (only currently needed for oracle)
  public String getCreateTriggerSql(TableCreator.Tbl tbl) {
    return null;
  }

  // Drop trigger (only currently needed for oracle)
  public String getDropTriggerSql(TableCreator.Tbl tbl) {
    return null;
  }

  // Add sequence (not currently different based on DBs)
  public String getCreateSequenceSql(TableCreator.Tbl tbl) {
    return "CREATE SEQUENCE " + qschema(tbl.seqpkseq);
  }

  // Drop sequence (not currently different based on DBs)
  public String getDropSequenceSql(TableCreator.Tbl tbl) {
    return "DROP SEQUENCE " + qschema(tbl.seqpkseq);
  }

  // Param char on storedprocs
  public String getProcParamChar() {
    return "";
  }

  // Construction of params/body for storedprocs
  public String getProcBodyString(QueryDef qd) {
    if (!qd.storedproc && !qd.storedfunction) return null;
    String paramChar = getProcParamChar();
    StringBuffer sb = new StringBuffer("(");
    boolean firstTime = true;
    for (Iterator i = qd.params.iterator(); i.hasNext();) {
      QueryDef.Field param = (QueryDef.Field) i.next();
      if (!firstTime) sb.append(",\n");
      sb.append(paramChar + param.name + " " + param.sqlType);
      firstTime = false;
    }
    sb.append(")\n");
    if (firstTime) {
      // No parameters at all, parentheses unnecessary
      sb.setLength(0); // Wipe out the contents
    }
    if (qd.storedfunction) {
      sb.append("RETURNS " + ((QueryDef.Field)qd.results.get(0)).sqlType + "\n");
    }
    sb.append("AS\n");
    if (qd.storedfunction) sb.append("BEGIN\n");
    for (Iterator i = qd.sql.iterator(); i.hasNext();) {
      Util.SQLParam param = (Util.SQLParam) i.next();
      sb.append(param.sqlbefore);
      if (param instanceof QueryDef.SQLParam)
        sb.append(paramChar + ((QueryDef.SQLParam) param).field.name);
    }
    if (qd.storedfunction) sb.append("END;\n");
    return sb.toString();
  }

  // Whether CREATE OR REPLACE PROCEDURE is supported (used twice)
  public boolean isCreateOrReplaceProcSupported() {
    return true;
  }

  // Create stored procedure (not currently different based on DBs)
  public String getCreateProcSql(TableCreator.Qry qry) {
    return "CREATE " + (isCreateOrReplaceProcSupported() ? "OR REPLACE " : "")
        + (qry.isfunction ? "FUNCTION " : "PROCEDURE ") + qschema(qry.name) + " " + qry.body;
  }

  // Drop procedure (not currently different based on DBs)
  public String getDropProcSql(TableCreator.Qry qry) {
    return "DROP " + (qry.isfunction ? "FUNCTION " : "PROCEDURE ") + qschema(qry.name);
  }
  
  public String getExecuteProcSql(TableCreator.Qry qry) {
    return "CALL " + qschema(qry.name);
  }

  // Whether to execute a 200ms sleep after executing an SQL statement
  public boolean isSleepRequired() {
    return false;
  }

  // Whether dropping and adding sequences and triggers is needed at all
  public boolean isSeqUsed() {
    return false;
  }

  public boolean isTrigUsed() {
    return false;
  }

  // What are the correct substitutions for ::true and ::false?
  public String getTrueSql() {
    return "true"; // or "1" or "-1" or "'Y'"
  }

  public String getFalseSql() {
    return "false"; // or "0" or "'N'"
  }

  public String getConcatSql() {
    return "||"; // or "+"
  }

  public abstract String getNowSql(); // No portable answer

  public abstract String getTodaySql(); // No portable answer

  public abstract String getSeqSql(TableDef td) throws FileLocatedException,
      IOException;

  public abstract String getDeclareSql(String varname, String vartype);

  public abstract String getVarSql(String varname);

  public abstract String getAssignBeginSql(String varname);

  public String getAssignEndSql(String varname) {
    return "";
  }

  public String getAssignEndBlockSql(String varname) {
    return "";
  }

  // What quote characters should be used?
  public String qs() {
    return "\""; // or "["
  }

  public String qe() {
    return "\""; // or "]"
  }

  public String q(String str) {
    return qs() + str + qe();
  }

  public String qschema(String str) {
    int pos = str.indexOf('.');
    if (pos >= 0) {
      return q(str.substring(0, pos)) + "." + q(str.substring(pos + 1));
    } else {
      return q(str);
    }
  }

  public boolean usesIdentity() {
    return true;
  }

  public String getCommandSeparator() {
    return null;
  }

  public String getCloseDbToUsersSql() {
    return null;
  }

  public String getOpenDbToUsersSql() {
    return null;
  }

  public abstract String getBooleanType();

  public abstract String getLongTextType();

  public abstract boolean getResultSetBoolean(ResultSet rs, String name)
      throws SQLException;
}
