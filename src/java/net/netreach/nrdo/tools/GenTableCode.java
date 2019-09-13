///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.GenTableCode
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2001/08/01
// Description: Construct a source code file from a table definition.
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

// CGL classes for output generation.
import java.io.IOException;
import java.util.Calendar;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

import net.netreach.cgl.CGLParser;
import net.netreach.cgl.Context;
import net.netreach.cgl.Expr;
import net.netreach.cgl.Scope;
import net.netreach.util.FileLocatedException;
import net.netreach.util.MappedList;
import net.netreach.util.Output;

/**
 * Construct a source code file from a table definition. The code is written
 * according to a cgl template file defined in the config file passed on the
 * command line.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class GenTableCode {

  /**
   * Load in a table and a template, and produce an output file according to the
   * template.
   * 
   * @param args
   *          The arguments passed on the command line.
   */
  public static void main(String[] args) throws FileLocatedException, IOException {

    // Test for bad arguments and throw out a usage message.
    if (args.length < 1) {
      Output.reportError(null, "Usage: GenTableCode <config> <table> ...");
      System.exit(1);
    }

    // Parse and load the configuration file.
    Config cfg = Config.get(args[0]);

    // Do the main processing.
    doMain(cfg, args);
  }

  public static void doMain(Config cfg, String[] args) throws FileLocatedException,
      IOException {

    Map cfgvars = new HashMap();
    cfgvars.put("nrdover", Version.NRDO_VERSION);
    Calendar cal = Calendar.getInstance();
    int mth = cal.get(Calendar.MONTH) + 1;
    int day = cal.get(Calendar.DATE);
    cfgvars.put("date", "" + cal.get(Calendar.YEAR) + "/"
        + (mth < 10 ? "0" : "") + mth + "/" + (day < 10 ? "0" : "") + day);
    cfgvars.put("dbadapter", cfg.dbadaptername);
    cfgvars.put("srcbase", cfg.srcbase);
    cfgvars.put("basemodule", cfg.module);
    cfgvars.put("schema", cfg.schema);

    // Create a blank CGL context.
    Context basectx = new Context();
    basectx.srcbase = cfg.srcbase;
    basectx.age = cfg.modified;
    basectx.vars = new Scope(cfgvars);

    DfnBase dfnbase = cfg.dfnbase;

    // Load and parse the CGL template file.
    CGLParser.setVerbose(true);
    Expr template = CGLParser.loadTemplate(cfg.cgltemplate);

    // Determine the tables to process.
    List tables = dfnbase.getFromArgs(args);
    cfgvars.put("alltables", new MappedList(tables));
    cfgvars.put("allqueries", new MappedList(dfnbase.getQueriesFromArgs(args)));
    cfgvars.put("dependtables", new MappedList(dfnbase.getDependTables()));

    for (Iterator i = tables.iterator(); i.hasNext();) {

      // Load the table definition.
      TableDef td = (TableDef) i.next();

      // Create a blank CGL context.
      Context ctx = new Context(basectx);
      if (td.age > ctx.age) ctx.age = td.age;

      // Construct a HashMap of the values from basectx.vars.
      // This is theoretically equivalent to new HashMap(basectx.vars) but
      // Classpath's HashMap constructor doesn't like Scopes much.
      HashMap baseVars = new HashMap();
      for (Iterator j = basectx.vars.entrySet().iterator(); j.hasNext();) {
        Map.Entry e = (Map.Entry) j.next();
        baseVars.put(e.getKey(), e.getValue());
      }
      ctx.vars = new Scope(td.toMap(), baseVars);

      // Use the CGL expression created earlier to process the table.
      template.evaluate(ctx);
    }
  }
}
