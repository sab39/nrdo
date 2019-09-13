///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.MSSQLAdapter
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2003/04/28
// Description: Defines the syntax of the DDL of MS SQL Server.
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
import java.sql.ResultSet;
import java.sql.SQLException;

/**
 * Defines the syntax of the DDL of MS SQL Server.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class MSSQLAdapter extends DBAdapter {
  public MSSQLAdapter(Config cfg) {
    super(cfg);
  }

  // Field add string
  public String getAddFieldSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " ADD " + q(fld.name) + " "
        + getFieldString(fld, true);
  }

  // How identity fields differ from non-identity fields
  public String getIdentityFieldString(TableCreator.Field fld, boolean forceNull) {
    return fld.dbType + " IDENTITY NOT NULL";
  }

  // Whether an identity field can be created as nullable and altered, or
  // whether it must be created non-null off the bat.
  public boolean isNullIdentitySupported() {
    return false;
  }

  // Drop index
  public String getDropIndexSql(TableCreator.Key key) {
    return "DROP INDEX " + qschema(key.tbl.name) + "." + q(key.name);
  }

  // Rename table
  public String getRenameTableSql(TableCreator.Tbl tbl, String newName) {
    return "EXEC sp_rename '" + qschema(tbl.name) + "', '"
        + newName.substring(newName.lastIndexOf('.') + 1) + "'";
  }

  // Param char on storedprocs
  public String getProcParamChar() {
    return "@";
  }
  
  public String getExecuteProcSql(TableCreator.Qry qry) {
    return "EXEC " + qschema(qry.name);
  }
  
  // Whether CREATE OR REPLACE PROCEDURE is supported (used twice)
  public boolean isCreateOrReplaceProcSupported() {
    return false;
  }

  // What are the correct substitutions for ::true and ::false?
  public String getTrueSql() {
    return "1";
  }

  public String getFalseSql() {
    return "0";
  }

  public String getConcatSql() {
    return "+";
  }

  public String getNowSql() {
    return "{fn Now()}";
  }

  public String getTodaySql() {
    return "{fn Today()}";
  }

  public String getSeqSql(TableDef td) {
    return "@@identity";
  }

  public String getDeclareSql(String varname, String vartype) {
    return "declare @" + varname + " " + vartype;
  }

  public String getVarSql(String varname) {
    return "@" + varname;
  }

  public String getAssignBeginSql(String varname) {
    return "select @" + varname + " = ";
  }

  // What quote characters should be used?
  public String qs() {
    return "[";
  }

  public String qe() {
    return "]";
  }

  public String getCommandSeparator() {
    return "\r\nGO\r\n";
  }

  public String getCloseDbToUsersSql() {
    return "ALTER DATABASE SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
  }

  public String getOpenDbToUsersSql() {
    return "ALTER DATABASE SET MULTI_USER";
  }

  public String getBooleanType() {
    return "bit";
  }

  public String getLongTextType() {
    return "ntext";
  }

  public boolean getResultSetBoolean(ResultSet rs, String name)
      throws SQLException {
    return rs.getInt(name) != 0;
  }
}
