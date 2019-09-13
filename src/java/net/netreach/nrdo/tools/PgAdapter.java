///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.PgAdapter
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2003/04/28
// Description: Defines the syntax of the DDL of PostgreSQL.
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
 * Defines the syntax of the DDL of PostgreSQL.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class PgAdapter extends DBAdapter {
  public PgAdapter(Config cfg) {
    super(cfg);
  }

  // Field add string
  public String getAddFieldSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " ADD " + q(fld.name) + " "
        + getFieldString(fld, true);
  }

  // Field drop
  public String getDropFieldSql(TableCreator.Field fld) {
    return super.getDropFieldSql(fld);
  }

  // Field set not null
  public String getSetFieldNotNullSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " ALTER COLUMN "
        + q(fld.name) + (fld.nullable ? " DROP" : " SET") + " NOT NULL";
  }

  public String getSetFieldNullSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " ALTER COLUMN "
        + q(fld.name) + " DROP NOT NULL";
  }

  // How identity fields differ from non-identity fields
  public String getIdentityFieldString(TableCreator.Field fld, boolean forceNull) {
    return fld.dbType + " DEFAULT NEXTVAL('" + fld.tbl.seqpkseq + "')"
        + (forceNull || fld.nullable ? "" : " NOT") + " NULL";
  }

  // Drop index
  public String getDropIndexSql(TableCreator.Key key) {
    return "DROP INDEX " + getIndexName(key);
  }

  // Param char on storedprocs
  public String getProcParamChar() {
    return "";
  }

  // Construction of params/body for storedprocs
  public String getProcBodyString(QueryDef qd) {
    if (!qd.storedproc) return null;
    String paramChar = getProcParamChar();
    StringBuffer sb = new StringBuffer("(");
    boolean firstTime = true;
    for (Iterator i = qd.params.iterator(); i.hasNext();) {
      QueryDef.Field param = (QueryDef.Field) i.next();
      if (!firstTime) sb.append(",\n");
      sb.append(paramChar + param.name + " " + param.sqlType);
      firstTime = false;
    }
    sb.append(")\nAS\n");
    for (Iterator i = qd.sql.iterator(); i.hasNext();) {
      Util.SQLParam param = (Util.SQLParam) i.next();
      sb.append(param.sqlbefore);
      if (param instanceof QueryDef.SQLParam) {
        sb.append(paramChar + ((QueryDef.SQLParam) param).field.name);
      }
    }
    return sb.toString();
  }

  // Whether CREATE OR REPLACE PROCEDURE is supported (used twice)
  public boolean isCreateOrReplaceProcSupported() {
    return true;
  }

  // Whether dropping and adding sequences and triggers is needed at all
  public boolean isSeqUsed() {
    return true;
  }

  // What quote characters should be used?
  public String qs() {
    return "\"";
  }

  public String qe() {
    return "\"";
  }

  // Substitutions for portable SQL
  public String getNowSql() {
    return "timenow()";
  }

  public String getTodaySql() {
    throw new RuntimeException("Need to find out how to do Today() on Postgres");
  }

  public String getSeqSql(TableDef td) throws FileLocatedException, IOException {
    td.resolveFields();
    if (!td.isPkeySequenced)
      throw new RuntimeException(td.fullName
          + " does not have a sequenced pkey");
    return "currval('sq_" + td.hash + "_"
        + ((TableDef.Field) td.pkey.get(0)).name + "')";
  }

  public String getDeclareSql(String varname, String vartype) {
    return "create temp table nrdovar_" + varname + " (" + varname + " "
        + vartype + " null); insert into nrdovar_" + varname + " (" + varname
        + ") values (null)";
  }

  public String getVarSql(String varname) {
    return "(select " + varname + " from nrdovar_" + varname + ")";
  }

  public String getAssignBeginSql(String varname) {
    return "update nrdovar_" + varname + " set " + varname + " = ";
  }

  public String getAssignEndBlockSql(String varname) {
    return "drop table nrdovar_" + varname + ";";
  }

  public boolean usesIdentity() {
    return false;
  }

  public String getBooleanType() {
    return "boolean";
  }

  public String getLongTextType() {
    return "text";
  }

  public boolean getResultSetBoolean(ResultSet rs, String name)
      throws SQLException {
    return rs.getBoolean(name);
  }
}
