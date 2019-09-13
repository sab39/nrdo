///////////////////////////////////////////////////////////////////////////////
// Class: net.netreach.nrdo.tools.NRDOTool
//
//      Author: Stuart Ballard <sballard@netreach.net>
//  Created On: 2000/11/17
// Description: Run TableCreator and TableToCode, sharing information and VM.
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

// Various miscellaneous classes used...
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.TreeSet;

import net.netreach.cgl.CGLParser;
import net.netreach.util.CVSDir;
import net.netreach.util.Output;
import net.netreach.util.PathUtil;

/**
 * Run TableCreator and TableToCode/GenTableCode, sharing information and VM.
 * This improves performance as the table definitions only need to be loaded
 * once, and the VM initialization overhead only needs to happen once, too.
 * 
 * @author Stuart Ballard &lt;sballard@netreach.net&gt;
 */
public class NRDOTool {

  /**
   * Main processing.
   */
  public static void main(String[] args) throws Throwable {

    // Test for bad arguments and throw out a usage message.
    if (args.length < 1) {
      Output.reportError(null, "Usage: NRDOTool <config> [<table> ...]");
      System.exit(1);
    }

    // Trap the special args "-gen" and "-create" but pass all other args
    // through
    // to the underlying programs.
    boolean gencode = true;
    boolean createtables = true;
    ArrayList processedArgs = new ArrayList();
    for (int i = 0; i < args.length; i++) {
      if ("-gen".equals(args[i])) {
        createtables = false;
      } else if ("-create".equals(args[i])) {
        gencode = false;
      } else {
        processedArgs.add(args[i]);
      }
    }
    args = (String[]) processedArgs.subList(1, processedArgs.size()).toArray(new String[processedArgs.size() - 1]);
    
    if (!doMain((String) processedArgs.get(0), gencode, createtables, false, args)) {
      System.exit(1);
    }
  }

  public static boolean doMain(String configPath, boolean gencode,
      boolean createtables, boolean dropTables, String[] tables) throws Throwable {

    Config cfg = null;
    try {
      // Parse and load the configuration file.
      cfg = Config.get(configPath);
      
      if (cfg.isNothingToDo()) {
        Output.println("nrdo: No changes to process for " + configPath + ".");
        return true;
      } else {
        Output.println("nrdo: Processing" + configPath + "...");
      }

      // Do the main processing of both tools.
      if (gencode) {
        if (cfg.nocode) {
          Output.println("Skipping codegen step due to 'nocode' in .nrdo file.");
        } else {
          // Queries are done first due to a bug which seems to cause referenced
          // tables to end up in the "alltables"
          // list even when they aren't in the current module and so should be
          // "dependtables" instead.
          // This doesn't FIX the bug (can't find it!), but avoids the scenario
          // where it currently causes a problem.
          // Since queries can never cause tables or even other queries to be
          // loaded, and the alltables list is used
          // before any loading happens on the first table that HAS changed,
          // there's no opportunity for anything that
          // shouldn't be there to have been added first.
          Output.println("Generating query code using CGL...");
          GenQueryCode.doMain(cfg, tables);
          Output.println("Generating table code using CGL...");
          GenTableCode.doMain(cfg, tables);
        }
      }
      if (createtables) {
        if (cfg.nodatabase) {
          Output.println("Skipping database step due to 'nodatabase' in .nrdo file.");
        } else {
          Output.println("Updating DB tables...");
          new TableCreator().doMain(cfg, dropTables, tables);
        }
      }

      // Generate the list of files touched and create a stamp file, if
      // requested.
      if (gencode && createtables && cfg.stampfile != null) {
        TreeSet touchedFiles = new TreeSet();
        touchedFiles.addAll(CGLParser.getTouchedFiles());
        addTouchedFiles(touchedFiles, cfg);
        PrintWriter pw = new PrintWriter(new FileWriter(cfg.stampfile));
        for (Iterator i = touchedFiles.iterator(); i.hasNext();) {
          pw.println("" + i.next());
        }
        pw.close();
        Output.println("Stamp file written.");
      }
    } catch (Exception e) {
      Output.reportException(e);
      return false;
    }
    return true;
  }


  public static void reset() {
    Output.reset();
    CGLParser.reset();
    Config.reset();
    CVSDir.reset();
  }

  private static void addTouchedFiles(TreeSet touchedFiles, Config cfg)
      throws IOException {
    addTouchedCfgFile(touchedFiles, cfg);
    touchedFiles.addAll(cfg.dfnbase.touchedFiles);
    for (Iterator i = cfg.depends.iterator(); i.hasNext();) {
      addTouchedFiles(touchedFiles, (Config) i.next());
    }
  }

  private static void addTouchedCfgFile(TreeSet touchedFiles, Config cfg)
      throws IOException {
    if (!touchedFiles.add(PathUtil.canonicalFile(cfg.cfgfile))) return;
    if (cfg.baseConfig != null)
      addTouchedCfgFile(touchedFiles, cfg.baseConfig);
  }
}
