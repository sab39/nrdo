///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.OracleAdapter
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2003/04/28
// Description: Defines the syntax of the DDL of Oracle.
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

import net.netreach.util.FileLocatedException;

/**
 * Defines the syntax of the DDL of Oracle.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class OracleAdapter extends DBAdapter {
  public OracleAdapter(Config cfg) {
    super(cfg);
  }

  // Field add string
  public String getAddFieldSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " ADD (" + q(fld.name)
        + " " + getFieldString(fld, true) + ")";
  }

  // Field drop
  public String getDropFieldSql(TableCreator.Field fld) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " DROP (" + q(fld.name)
        + ")";
  }

  // Field type alteration
  public String getAlterFieldTypeSql(TableCreator.Field fld, boolean forceNull) {
    return "ALTER TABLE " + qschema(fld.tbl.name) + " MODIFY (" + q(fld.name)
        + " " + getFieldString(fld, forceNull) + ")";
  }

  // Full name for index
  public String getIndexName(TableCreator.Key key) {
    String base = "";
    if (cfg.schema != null) base = q(cfg.schema) + ".";
    return base + q(key.name);
  }

  // Drop index
  public String getDropIndexSql(TableCreator.Key key) {
    return "DROP INDEX " + getIndexName(key);
  }

  // Add trigger (only currently needed for oracle)
  public String getCreateTriggerSql(TableCreator.Tbl tbl) {
    return "CREATE OR REPLACE TRIGGER " + qschema(tbl.seqpktrg) + "\n"
        + "  BEFORE INSERT ON " + qschema(tbl.name) + " FOR EACH ROW\n"
        + "  BEGIN\n" + "    IF :new." + q(tbl.seqpkfld) + " IS NULL THEN\n"
        + "      SELECT " + qschema(tbl.seqpkseq) + ".NEXTVAL\n"
        + "        INTO :new." + q(tbl.seqpkfld) + "\n"
        + "        FROM dual;\n" + "    END IF;\n" + "  END;";
  }

  // Drop trigger (only currently needed for oracle)
  public String getDropTriggerSql(TableCreator.Tbl tbl) {
    return "DROP TRIGGER " + qschema(tbl.seqpktrg);
  }

  // Whether dropping and adding sequences and triggers is needed at all
  public boolean isSeqUsed() {
    return true;
  }

  public boolean isTrigUsed() {
    return true;
  }

  // What are the correct substitutions for ::true and ::false?
  public String getTrueSql() {
    return "'Y'";
  }

  public String getFalseSql() {
    return "'N'";
  }

  public String getNowSql() {
    return "SYSDATE";
  }

  public String getTodaySql() {
    return "TRUNC(SYSDATE, 'J')";
  }

  public String getSeqSql(TableDef td) throws FileLocatedException, IOException {
    td.resolveFields();
    if (!td.isPkeySequenced)
      throw new RuntimeException(td.fullName
          + " does not have a sequenced pkey");
    return "sq_" + td.hash + "_" + ((TableDef.Field) td.pkey.get(0)).name
        + ".currval";
  }

  public String getDeclareSql(String varname, String vartype) {
    return "declare " + varname + " " + vartype + "; begin null";
  }

  public String getVarSql(String varname) {
    return varname;
  }

  public String getAssignBeginSql(String varname) {
    return varname + " := ";
  }

  public String getAssignEndBlockSql(String varname) {
    return " end;";
  }

  // What quote characters are used? Currently none, because quoting things
  // in Oracle makes them case-sensitive where they wouldn't normally be.
  public String qs() {
    return "";
  }

  public String qe() {
    return "";
  }

  public boolean usesIdentity() {
    return false;
  }

  public String getBooleanType() {
    return "char(1)";
  }

  public String getLongTextType() {
    return "long";
  }

  public boolean getResultSetBoolean(ResultSet rs, String name)
      throws SQLException {
    return rs.getString(name).charAt(0) == 'Y';
  }
}
